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
}

public sealed record CameraPollResult(CameraPollOutcome Outcome, Guid? EvidenceId, string? Message);

/// <summary>
/// Polls one camera source. Unchanged images (same SHA-256) create no
/// evidence and no AI job. A changed image becomes CameraFeed evidence on
/// the nearest report within range and enters the existing analysis queue.
/// With no nearby report, the change is recorded on the source only —
/// unattached evidence is not invented.
/// </summary>
public sealed class CameraIngestionService(
    PathLensDbContext dbContext,
    IObjectStorage storage,
    IHttpClientFactory httpClientFactory)
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

            var report = await FindMatchingReportAsync(source, cancellationToken);
            if (report is null)
            {
                source.RecordSuccess(now, hash, changed: true);
                await dbContext.SaveChangesAsync(cancellationToken);
                return new CameraPollResult(
                    CameraPollOutcome.NoMatchingReport, null,
                    "Image changed, but no report exists within range of this camera. Recorded as a change only.");
            }

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
                    EvidenceSourceType.CameraFeed);

                await dbContext.EvidenceItems.AddAsync(evidence, cancellationToken);
                await dbContext.AnalysisJobs.AddAsync(AnalysisJob.Create(evidence.Id, now), cancellationToken);
                report.RecordEvidence(now);

                source.RecordSuccess(now, hash, changed: true);
                await dbContext.SaveChangesAsync(cancellationToken);

                return new CameraPollResult(CameraPollOutcome.Changed, evidence.Id,
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
            results.Add((source.Id, await PollSourceAsync(source, cancellationToken)));
        }

        return results;
    }

    private async Task<AccessibilityReport?> FindMatchingReportAsync(
        CameraSource source, CancellationToken cancellationToken)
    {
        const double delta = 0.002;
        var candidates = await dbContext.Reports.AsNoTracking()
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
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => candidate.Report)
            .FirstOrDefault();
    }

    private async Task<CameraPollResult> FailAsync(
        CameraSource source, string error, CancellationToken cancellationToken)
    {
        source.RecordFailure(DateTimeOffset.UtcNow, error);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CameraPollResult(CameraPollOutcome.Failed, null, error);
    }
}
