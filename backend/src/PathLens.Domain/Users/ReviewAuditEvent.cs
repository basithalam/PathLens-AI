namespace PathLens.Domain.Users;

/// <summary>
/// One trust-changing action (verify, dispute, reject, comparison override).
/// The original value is always kept so history can be audited.
/// </summary>
public sealed class ReviewAuditEvent
{
    private ReviewAuditEvent()
    {
    }

    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? PreviousValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static ReviewAuditEvent Create(
        string entityType,
        Guid entityId,
        Guid actorUserId,
        string action,
        string? previousValue,
        string? newValue,
        string? note,
        DateTimeOffset now) => new()
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            ActorUserId = actorUserId,
            Action = action,
            PreviousValue = previousValue,
            NewValue = newValue,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt = now,
        };
}
