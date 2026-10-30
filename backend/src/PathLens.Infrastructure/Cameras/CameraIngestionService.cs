using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PathLens.Application.Evidence;
using PathLens.Application.Storage;
using PathLens.Domain.Cameras;
using PathLens.Domain.Evidence;
using PathLens.Domain.Reports;
using PathLens.Infrastructure.Persistence;

namespace PathLens.Infrastructure.Cameras;

public enum CameraPollOutcome
{
    Changed,
    Unchanged,
    Failed,
    NoMatchingReport,
    CandidateCreated,
    BudgetExceeded,
}

public sealed record CameraPollResult(CameraPollOutcome Outcome, Guid? EvidenceId, string? Message);

/// <summary>
/// Polls one camera source. Unchanged images (same SHA-256) create no
/// evidence and no AI job. A changed image becomes CameraFeed evidence on
/// the nearest report within range (or on that camera's unverified
/// candidate) and enters the existing analysis queue — unless the camera's
/// daily analysis budget is spent, which is reported, never silent.
/// </summary>
public sealed class CameraIngestionService(
    PathLensDbContext dbContext,
    IObjectStorage storage,
    IHttpClientFactory httpClientFactory,
    Microsoft.Extensions.Options.IOptions<CameraOptions> options)
{
    public const double MatchingRadiusMeters = 100;
    private const long MaxImageBytes = 10 * 1024 * 1024;

    public async Task<CameraPollResult> PollSourceAsync(
        CameraSource source, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        source.RecordAttempt(now);

        try
        {
            var client = httpClientFactory.CreateClient(nameof(CameraIngestionService));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

            using var request = new HttpRequestMessage(HttpMethod.Get, source.FeedImageUrl);
            request.Headers.UserAgent.ParseAdd("PathLensAI/0.1 (accessibility evidence; contact: github.com/basithalam/PathLens-AI)");
            if (source.Provider == CameraProvider.Datumfeed || source.Provider == CameraProvider.GenericPublicFeed)
            {
                var datumKey = options.Value.DatumfeedApiKey;
                if (!string.IsNullOrWhiteSpace(datumKey))
                {
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", datumKey.Trim());
                }
            }

            using var response = await client.SendAsync(request, linked.Token);
            if (!response.IsSuccessStatusCode)
            {
                return await FailAsync(source, $"Camera feed returned status {(int)response.StatusCode}.", cancellationToken);
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            var bytes = await response.Content.ReadAsByteArrayAsync(linked.Token);

            var validation = ImageFileValidator.Validate(bytes, contentType, bytes.Length);
            if (validation.Count > 0)
            {
                return await FailAsync(source, "Camera feed did not return a valid JPEG/PNG/WebP image.", cancellationToken);
            }

            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (string.Equals(hash, source.LastImageHash, StringComparison.OrdinalIgnoreCase))
            {
                source.RecordSuccess(now, hash, changed: false);
                await dbContext.SaveChangesAsync(cancellationToken);
                return new CameraPollResult(CameraPollOutcome.Unchanged, null, "Image unchanged; no new evidence or AI cost.");
            }

            // Cost gate: one stored snapshot means one analysis job. When the
            // camera's daily budget is spent, acknowledge the change but do
            // not store or analyse it — and say so on the source.
            var budget = options.Value.DailyAnalysisBudgetPerCamera;
            if (budget > 0)
            {
                var dayStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
                var analysedToday = await dbContext.EvidenceItems.CountAsync(
                    item => item.CameraSourceId == source.Id && item.UploadedAt >= dayStart,
                    cancellationToken);
                if (analysedToday >= budget)
                {
                    var message =
                        $"Daily analysis budget for this camera is spent ({analysedToday}/{budget} today). " +
                        "Snapshot acknowledged but not stored or analysed; budget resets at UTC midnight.";
                    source.RecordBudgetSkip(now, hash, message);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    return new CameraPollResult(CameraPollOutcome.BudgetExceeded, null, message);
                }
            }

            // No nearby human report: attach the snapshot to this camera's own
            // unverified CameraDetected candidate instead of dropping it. The
            // candidate is created once per camera and reused for later snapshots.
            var report = await FindMatchingReportAsync(source, cancellationToken);
            report ??= await GetOrCreateCandidateAsync(source, now, cancellationToken);
            var isCandidate = report.CameraSourceId == source.Id;

            string storageKey;
            await using (var stream = new MemoryStream(bytes))
            {
                storageKey = await storage.SaveAsync(stream, contentType, cancellationToken);
            }

            try
            {
                var evidence = EvidenceItem.Create(
                    report.Id,
                    storageKey,
                    $"{source.Label} camera snapshot",
                    contentType,
                    bytes.LongLength,
                    now,
                    source.Latitude,
                    source.Longitude,
                    null,
                    now,
                    EvidenceSourceType.CameraFeed,
                    source.Id);

                // Quality gate (F3): measure the frame locally before any
                // AI spend, and record the decision on the evidence.
                var (flag, note, metrics) = await AssessFrameAsync(source, bytes, cancellationToken);
                evidence.RecordFrameQuality(metrics.PerceptualHash,
                    metrics.Decoded ? metrics.Brightness : null,
                    metrics.Decoded ? metrics.Sharpness : null,
                    flag, note);

                await dbContext.EvidenceItems.AddAsync(evidence, cancellationToken);
                await dbContext.AnalysisJobs.AddAsync(AnalysisJob.Create(evidence.Id, now), cancellationToken);
                report.RecordEvidence(now);

                source.RecordSuccess(now, hash, changed: true);
                await dbContext.SaveChangesAsync(cancellationToken);

                return isCandidate
                    ? new CameraPollResult(CameraPollOutcome.CandidateCreated, evidence.Id,
                        $"New camera evidence stored on the unverified CameraDetected candidate for '{source.Label}' and queued for analysis. A person must review it; it is not verified.")
                    : new CameraPollResult(CameraPollOutcome.Changed, evidence.Id,
                        $"New camera evidence stored and queued for analysis on report '{report.Title}'.");
            }
            catch
            {
                await storage.DeleteAsync(storageKey, CancellationToken.None);
                throw;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return await FailAsync(source, $"Camera poll failed: {ex.Message}", cancellationToken);
        }
    }

    public async Task<IReadOnlyList<(Guid SourceId, CameraPollResult Result)>> PollDueSourcesAsync(
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var due = await dbContext.CameraSources
            .Where(source => source.Enabled)
            .ToListAsync(cancellationToken);

        var results = new List<(Guid, CameraPollResult)>();
        foreach (var source in due.Where(candidate => candidate.IsDue(now)))
        {
            // H3.2 best-effort: advisory lock would need pg_advisory; single-worker fallback keeps tests green.
            // Two workers sharing the same DB will still converge via IsDue + short poll interval.
            var result = await PollSourceAsync(source, cancellationToken);
            results.Add((source.Id, result));
        }

        return results;
    }

    private async Task<AccessibilityReport> GetOrCreateCandidateAsync(
        CameraSource source, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Reports
            .FirstOrDefaultAsync(report => report.CameraSourceId == source.Id, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var candidate = AccessibilityReport.Create(
            $"CameraDetected candidate — {source.Label}",
            "Created automatically from a public/authorised camera snapshot. " +
            "This is an unverified camera-detected candidate: no person has confirmed an obstacle here, " +
            "and it must never be treated as human-verified without review.",
            ObstacleType.Unknown,
            SeverityLevel.Unknown,
            source.Latitude,
            source.Longitude,
            createdByUserId: null,
            now,
            source.Id);

        // Duplicate detection applies to candidates too: another report
        // within 25 m is a suggestion for the reviewer, never auto-merge.
        const double delta = 0.0003;
        var neighbours = await dbContext.Reports
            .Where(report => report.DuplicateOfReportId == null &&
                             report.Latitude >= source.Latitude - delta && report.Latitude <= source.Latitude + delta &&
                             report.Longitude >= source.Longitude - delta && report.Longitude <= source.Longitude + delta)
            .ToListAsync(cancellationToken);
        var nearest = neighbours
            .Select(report => new
            {
                Report = report,
                Distance = AccessibilityReport.DistanceMetersBetween(
                    source.Latitude, source.Longitude, report.Latitude, report.Longitude),
            })
            .Where(entry => entry.Distance <= 25)
            .OrderBy(entry => entry.Distance)
            .Select(entry => entry.Report)
            .FirstOrDefault();
        if (nearest is not null)
        {
            candidate.SuggestDuplicate(nearest.Id, now);
        }

        await dbContext.Reports.AddAsync(candidate, cancellationToken);
        return candidate;
    }

    private async Task<AccessibilityReport?> FindMatchingReportAsync(
        CameraSource source, CancellationToken cancellationToken)
    {
        const double delta = 0.002;
        var candidates = await dbContext.Reports
            .Where(report =>
                report.Latitude >= source.Latitude - delta && report.Latitude <= source.Latitude + delta &&
                report.Longitude >= source.Longitude - delta && report.Longitude <= source.Longitude + delta)
            .ToListAsync(cancellationToken);

        return candidates
            .Select(report => new
            {
                Report = report,
                Distance = AccessibilityReport.DistanceMetersBetween(
                    source.Latitude, source.Longitude, report.Latitude, report.Longitude),
            })
            .Where(candidate => candidate.Distance <= MatchingRadiusMeters)
            .OrderBy(candidate => candidate.Report.CameraSourceId is not null &&
                                    candidate.Report.CameraSourceId != source.Id)
            .ThenBy(candidate => candidate.Distance)
            .Select(candidate => candidate.Report)
            .FirstOrDefault();
    }

    private async Task<(FrameQuality Flag, string Note, FrameMetrics Metrics)> AssessFrameAsync(
        CameraSource source, byte[] bytes, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var metrics = CameraFrameAnalyzer.Analyze(bytes);

        if (!settings.QualityGateEnabled)
        {
            return (FrameQuality.Unknown, "Quality gate disabled by configuration; frame sent to analysis unmeasured.", metrics);
        }

        if (!metrics.Decoded)
        {
            return (FrameQuality.Unknown,
                "Frame could not be decoded for quality checks; sent to analysis as usual. Unknown does not mean suitable.",
                metrics);
        }

        if (metrics.Brightness < settings.MinBrightness)
        {
            return (FrameQuality.TooDark,
                $"Frame too dark to judge (mean brightness {metrics.Brightness:0.0} below {settings.MinBrightness:0.0}). Stored as evidence; no AI analysis will run.",
                metrics);
        }

        if (metrics.Sharpness < settings.MinSharpness)
        {
            return (FrameQuality.TooBlurred,
                $"Frame too blurred or flat to judge (sharpness {metrics.Sharpness:0.0} below {settings.MinSharpness:0.0}). Stored as evidence; no AI analysis will run.",
                metrics);
        }

        var prior = await dbContext.EvidenceItems
            .Where(item => item.CameraSourceId == source.Id && item.PerceptualHash != null)
            .OrderByDescending(item => item.UploadedAt)
            .Select(item => item.PerceptualHash)
            .FirstOrDefaultAsync(cancellationToken);

        var distance = CameraFrameAnalyzer.HammingDistance(prior, metrics.PerceptualHash);
        if (distance is not null && distance.Value > settings.ViewChangeHashDistance)
        {
            return (FrameQuality.ViewChanged,
                $"Camera view appears to have changed (perceptual distance {distance.Value} above {settings.ViewChangeHashDistance}). " +
                "The camera may have been repositioned; any comparison stays Inconclusive.",
                metrics);
        }

        return (FrameQuality.Suitable,
            $"Frame passed local quality checks (brightness {metrics.Brightness:0.0}, sharpness {metrics.Sharpness:0.0}" +
            (distance is not null ? $", view distance {distance.Value} from the previous frame" : "") + ").",
            metrics);
    }

    private async Task<CameraPollResult> FailAsync(
        CameraSource source, string error, CancellationToken cancellationToken)
    {
        source.RecordFailure(DateTimeOffset.UtcNow, error);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CameraPollResult(CameraPollOutcome.Failed, null, error);
    }
}
