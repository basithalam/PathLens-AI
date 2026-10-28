using PathLens.Application.Cameras;
using PathLens.Application.Comparisons;
using PathLens.Application.Reports;
using PathLens.Domain.Reports;
using PathLens.Domain.Users;

namespace PathLens.Application.Reviews;

public sealed class ReviewService(
    IReportRepository reportRepository,
    IComparisonRepository comparisonRepository,
    IReviewRepository reviewRepository,
    ICameraRepository cameraRepository)
{
    public async Task<ReportResponse?> ReviewReportAsync(
        Guid actorUserId,
        Guid reportId,
        VerificationStatus newStatus,
        string? note,
        CancellationToken cancellationToken)
    {
        var report = await reportRepository.GetByIdAsync(reportId, cancellationToken);
        if (report is null)
        {
            return null;
        }

        var previous = report.VerificationStatus;
        var now = DateTimeOffset.UtcNow;
        report.SetVerificationStatus(newStatus, now);

        await reviewRepository.AddAuditAsync(ReviewAuditEvent.Create(
            "Report", reportId, actorUserId, "VerificationChanged",
            previous.ToString(), newStatus.ToString(), note, now), cancellationToken);

        await reportRepository.SaveChangesAsync(cancellationToken);
        await reviewRepository.SaveChangesAsync(cancellationToken);
        return ReportService.ToResponse(report);
    }

    /// <summary>Reviewer confirms a duplicate suggestion: this report becomes a pointer, nothing is deleted.</summary>
    public async Task<ReportResponse?> ConfirmDuplicateAsync(
        Guid actorUserId, Guid reportId, string? note, CancellationToken cancellationToken)
    {
        var report = await reportRepository.GetByIdAsync(reportId, cancellationToken);
        if (report is null)
        {
            return null;
        }

        var suggested = report.PossibleDuplicateOfReportId;
        var canonicalId = report.ConfirmDuplicate(DateTimeOffset.UtcNow);

        await reviewRepository.AddAuditAsync(ReviewAuditEvent.Create(
            "Report", reportId, actorUserId, "DuplicateConfirmed",
            suggested is null ? null : $"PossibleDuplicateOf:{suggested}",
            $"DuplicateOf:{canonicalId}", note, DateTimeOffset.UtcNow), cancellationToken);

        await reportRepository.SaveChangesAsync(cancellationToken);
        await reviewRepository.SaveChangesAsync(cancellationToken);
        return ReportService.ToResponse(report);
    }

    /// <summary>Reviewer rejects a duplicate suggestion: both reports stay independent.</summary>
    public async Task<ReportResponse?> RejectDuplicateAsync(
        Guid actorUserId, Guid reportId, string? note, CancellationToken cancellationToken)
    {
        var report = await reportRepository.GetByIdAsync(reportId, cancellationToken);
        if (report is null)
        {
            return null;
        }

        var suggested = report.PossibleDuplicateOfReportId;
        report.RejectDuplicateSuggestion(DateTimeOffset.UtcNow);

        await reviewRepository.AddAuditAsync(ReviewAuditEvent.Create(
            "Report", reportId, actorUserId, "DuplicateSuggestionRejected",
            suggested is null ? null : $"PossibleDuplicateOf:{suggested}",
            null, note, DateTimeOffset.UtcNow), cancellationToken);

        await reportRepository.SaveChangesAsync(cancellationToken);
        await reviewRepository.SaveChangesAsync(cancellationToken);
        return ReportService.ToResponse(report);
    }

    public async Task RecordComparisonReviewAsync(
        Guid actorUserId,
        Guid comparisonId,
        string previousOutcome,
        string newOutcome,
        string reason,
        CancellationToken cancellationToken)
    {
        await reviewRepository.AddAuditAsync(ReviewAuditEvent.Create(
            "Comparison", comparisonId, actorUserId, "ComparisonReviewed",
            previousOutcome, newOutcome, reason, DateTimeOffset.UtcNow), cancellationToken);
        await reviewRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<ReviewQueueResponse> GetQueueAsync(CancellationToken cancellationToken)
    {
        var unverified = await reportRepository.ListAsync(
            new ReportQuery(null, null, VerificationStatus.Unverified, 1, 50, "oldest"),
            cancellationToken);

        // Conflicting comparisons need human eyes first.
        var conflicting = await comparisonRepository.ListByEffectiveOutcomeAsync(
            EvidenceComparisonOutcome.ConflictingEvidence, 50, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var cameras = await cameraRepository.ListAsync(cancellationToken);
        var staleCameras = cameras
            .Where(camera => camera.Enabled && camera.IsStale(now, CameraService.DefaultStaleAfter))
            .Select(camera => new StaleCameraItem(
                camera.Id, camera.Label, camera.City, camera.Provider.ToString(),
                camera.LastSuccessAt, camera.LastError))
            .ToList();

        return new ReviewQueueResponse(
            unverified.Items.Select(report => new QueueReportItem(
                report.Id, report.Title, report.ObstacleType.ToString(),
                report.VerificationStatus.ToString(), report.CreatedAt,
                report.CameraSourceId is not null,
                report.PossibleDuplicateOfReportId)).ToList(),
            conflicting.Select(comparison => new QueueComparisonItem(
                comparison.Id, comparison.ReportId, comparison.EffectiveOutcome.ToString(),
                comparison.CreatedAt)).ToList(),
            staleCameras);
    }

    public async Task<IReadOnlyList<ReviewResponse>> ListAuditForEntityAsync(
        string entityType, Guid entityId, CancellationToken cancellationToken)
    {
        var events = await reviewRepository.ListAuditForEntityAsync(entityType, entityId, cancellationToken);
        return events.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<ReviewResponse>> ListRecentAuditAsync(
        int limit, CancellationToken cancellationToken)
    {
        var events = await reviewRepository.ListRecentAuditAsync(limit, cancellationToken);
        return events.Select(ToResponse).ToList();
    }

    public static ReviewResponse ToResponse(ReviewAuditEvent auditEvent) => new(
        auditEvent.Id,
        auditEvent.EntityType,
        auditEvent.EntityId,
        auditEvent.ActorUserId,
        auditEvent.Action,
        auditEvent.PreviousValue,
        auditEvent.NewValue,
        auditEvent.Note,
        auditEvent.CreatedAt);
}
