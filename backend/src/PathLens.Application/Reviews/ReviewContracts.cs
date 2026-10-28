namespace PathLens.Application.Reviews;

public sealed record ReviewReportRequest(string? VerificationStatus, string? Note);

public sealed record ReviewResponse(
    Guid Id,
    string EntityType,
    Guid EntityId,
    Guid ActorUserId,
    string Action,
    string? PreviousValue,
    string? NewValue,
    string? Note,
    DateTimeOffset CreatedAt);

public sealed record ReviewQueueResponse(
    IReadOnlyList<QueueReportItem> Reports,
    IReadOnlyList<QueueComparisonItem> Comparisons,
    IReadOnlyList<StaleCameraItem> StaleCameras);

public sealed record StaleCameraItem(
    Guid Id,
    string Label,
    string City,
    string Provider,
    DateTimeOffset? LastSuccessAt,
    string? LastError);

public sealed record QueueReportItem(
    Guid Id,
    string Title,
    string ObstacleType,
    string VerificationStatus,
    DateTimeOffset CreatedAt,
    bool IsCameraCandidate,
    Guid? PossibleDuplicateOfReportId);

public sealed record DuplicateDecisionRequest(string? Note);

public sealed record QueueComparisonItem(
    Guid Id,
    Guid ReportId,
    string EffectiveOutcome,
    DateTimeOffset CreatedAt);

public interface IReviewRepository
{
    Task AddAuditAsync(Domain.Users.ReviewAuditEvent auditEvent, CancellationToken cancellationToken);
    Task<IReadOnlyList<Domain.Users.ReviewAuditEvent>> ListAuditForEntityAsync(
        string entityType, Guid entityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Domain.Users.ReviewAuditEvent>> ListRecentAuditAsync(
        int limit, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
