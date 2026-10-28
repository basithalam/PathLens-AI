using System.Text.Json;
using PathLens.Application.Ai;
using PathLens.Domain.Evidence;
using PathLens.Domain.Reports;

namespace PathLens.Application.Comparisons;

public enum ComparisonError
{
    None,
    SelfComparison,
    EvidenceNotFound,
    DifferentReports,
}

public sealed record ComparisonCreation(ComparisonResponse? Response, ComparisonError Error);

public sealed class ComparisonService(IComparisonRepository repository)
{
    public async Task<ComparisonCreation> CreateAsync(
        Guid earlierEvidenceId, Guid laterEvidenceId, CancellationToken cancellationToken)
    {
        if (earlierEvidenceId == laterEvidenceId)
        {
            return new ComparisonCreation(null, ComparisonError.SelfComparison);
        }

        var earlier = await repository.GetEvidenceAsync(earlierEvidenceId, cancellationToken);
        var later = await repository.GetEvidenceAsync(laterEvidenceId, cancellationToken);
        if (earlier is null || later is null)
        {
            return new ComparisonCreation(null, ComparisonError.EvidenceNotFound);
        }

        if (earlier.ReportId != later.ReportId)
        {
            return new ComparisonCreation(null, ComparisonError.DifferentReports);
        }

        var report = await repository.GetReportAsync(earlier.ReportId, cancellationToken);
        if (report is null)
        {
            return new ComparisonCreation(null, ComparisonError.EvidenceNotFound);
        }

        var earlierResult = ParseResult(earlier);
        var laterResult = ParseResult(later);

        var (locationQuality, distance) = EvaluateLocation(earlier, later, report);
        var (timestampTrusted, timestampNote) = EvaluateTimestamps(earlier, later);

        var decision = ComparisonEngine.Evaluate(new ComparisonInput(
            earlier.AnalysisStatus == AnalysisStatus.Completed && earlierResult is not null,
            later.AnalysisStatus == AnalysisStatus.Completed && laterResult is not null,
            earlierResult?.ObstacleType,
            laterResult?.ObstacleType,
            earlierResult?.Confidence ?? 0,
            laterResult?.Confidence ?? 0,
            locationQuality,
            distance,
            timestampTrusted,
            timestampNote,
            report.VerificationStatus == VerificationStatus.HumanVerified));

        var supporting = JsonSerializer.Serialize(new
        {
            earlierObstacleType = earlierResult?.ObstacleType,
            laterObstacleType = laterResult?.ObstacleType,
            earlierConfidence = earlierResult?.Confidence,
            laterConfidence = laterResult?.Confidence,
            locationQuality = locationQuality.ToString(),
            locationDistanceMeters = distance,
            timestampTrusted,
            earlierEffectiveTime = EffectiveTime(earlier),
            laterEffectiveTime = EffectiveTime(later),
            limitations = decision.Limitations,
            note = "NotObservedInNewImage never means an obstacle was removed.",
        });

        var comparison = EvidenceComparison.Create(
            report.Id,
            earlier.Id,
            later.Id,
            decision.Outcome,
            decision.Explanation,
            supporting,
            laterResult?.Model ?? earlierResult?.Model,
            DateTimeOffset.UtcNow);

        await repository.AddComparisonAsync(comparison, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return new ComparisonCreation(ToResponse(comparison), ComparisonError.None);
    }

    public async Task<ComparisonResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var comparison = await repository.GetComparisonAsync(id, cancellationToken);
        return comparison is null ? null : ToResponse(comparison);
    }

    public async Task<IReadOnlyList<ComparisonResponse>?> ListForReportAsync(
        Guid reportId, CancellationToken cancellationToken)
    {
        var report = await repository.GetReportAsync(reportId, cancellationToken);
        if (report is null)
        {
            return null;
        }

        var comparisons = await repository.ListForReportAsync(reportId, cancellationToken);
        return comparisons.Select(ToResponse).ToList();
    }

    public async Task<ComparisonResponse?> ReviewAsync(
        Guid id, EvidenceComparisonOutcome outcome, string reason, CancellationToken cancellationToken,
        Guid? reviewedByUserId = null)
    {
        var comparison = await repository.GetComparisonAsync(id, cancellationToken);
        if (comparison is null)
        {
            return null;
        }

        comparison.Review(outcome, reason, DateTimeOffset.UtcNow, reviewedByUserId);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(comparison);
    }

    private static ImageAnalysisResult? ParseResult(EvidenceItem item)
    {
        if (item.AnalysisResultJson is null)
        {
            return null;
        }

        return ImageAnalysisResultParser.TryParse(
            item.AnalysisResultJson,
            item.AnalysisProvider ?? "unknown",
            item.AnalysisModel,
            item.AnalyzedAt ?? item.UploadedAt,
            out var result,
            out _)
            ? result
            : null;
    }

    private static DateTimeOffset EffectiveTime(EvidenceItem item) =>
        item.CaptureTimeUtc ?? item.UploadedAt;

    private static (bool Trusted, string? Note) EvaluateTimestamps(EvidenceItem earlier, EvidenceItem later)
    {
        if (earlier.CaptureTimeUtc is null || later.CaptureTimeUtc is null)
        {
            return (false, "Capture time is missing for one or both photos; upload order alone cannot support a time-based claim.");
        }

        if (later.CaptureTimeUtc < earlier.CaptureTimeUtc)
        {
            return (false, "Capture times are out of order: the 'later' photo claims an earlier capture time.");
        }

        var now = DateTimeOffset.UtcNow;
        if (earlier.CaptureTimeUtc > now.AddDays(1) || later.CaptureTimeUtc > now.AddDays(1))
        {
            return (false, "A capture time is in the future, so the time order is not trusted.");
        }

        return (true, null);
    }

    private static (LocationMatchQuality Quality, double? Distance) EvaluateLocation(
        EvidenceItem earlier, EvidenceItem later, AccessibilityReport report)
    {
        var earlierHasGps = earlier.Latitude is not null && earlier.Longitude is not null;
        var laterHasGps = later.Latitude is not null && later.Longitude is not null;

        if (!earlierHasGps || !laterHasGps)
        {
            return (LocationMatchQuality.InheritedFromReport, null);
        }

        var distance = AccessibilityReport.DistanceMetersBetween(
            earlier.Latitude!.Value, earlier.Longitude!.Value,
            later.Latitude!.Value, later.Longitude!.Value);

        var accuracy = (earlier.LocationAccuracyMeters ?? 0) + (later.LocationAccuracyMeters ?? 0);

        // GPS accuracy widens the strong-match radius; thresholds follow Section 18.
        if (distance <= Math.Max(25, accuracy))
        {
            return (LocationMatchQuality.Strong, distance);
        }

        return distance <= 75
            ? (LocationMatchQuality.Possible, distance)
            : (LocationMatchQuality.Mismatch, distance);
    }

    public static ComparisonResponse ToResponse(EvidenceComparison comparison) => new(
        comparison.Id,
        comparison.ReportId,
        comparison.EarlierEvidenceId,
        comparison.LaterEvidenceId,
        comparison.Outcome.ToString(),
        comparison.EffectiveOutcome.ToString(),
        comparison.Explanation,
        comparison.SupportingEvidenceJson,
        comparison.ModelVersion,
        comparison.CreatedAt,
        comparison.ReviewedOutcome?.ToString(),
        comparison.ReviewReason,
        comparison.ReviewedAt,
        comparison.ReviewedByUserId);
}
