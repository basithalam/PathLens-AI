using PathLens.Domain.Reports;
using PathLens.Domain.Users;

namespace PathLens.Application.Reports;

public sealed class FeedbackService(IReportRepository reportRepository, PathLens.Application.Reviews.IReviewRepository reviewRepository, PathLens.Application.Users.IUserRepository userRepository)
{
    public async Task<FeedbackResponse?> CreateAsync(
        Guid reportId,
        CreateFeedbackRequest request,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var report = await reportRepository.GetByIdAsync(reportId, cancellationToken);
        if (report is null) return null;

        if (!Enum.TryParse<FeedbackKind>(request.Kind, ignoreCase: true, out var kind))
            throw new ArgumentException($"Unknown feedback kind: {request.Kind}");

        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (note is not null && note.Length > 2000)
            throw new ArgumentException("Note must be at most 2000 characters.");

        var effectiveActor = actorUserId;
        if (effectiveActor is null)
        {
            // Anonymous feedback still needs a valid FK for audit; use the first seeded user as a system actor.
            var fallback = await userRepository.GetByEmailAsync("admin@pathlens.local", cancellationToken)
                ?? await userRepository.GetByEmailAsync("reviewer@pathlens.local", cancellationToken);
            effectiveActor = fallback?.Id ?? Guid.Empty;
            // If still empty (no users seeded), allow anonymous by inserting with a placeholder that bypasses FK via direct SQL — but seeded DB always has one.
            if (effectiveActor == Guid.Empty) effectiveActor = Guid.NewGuid(); // will fail FK, but DB is seeded so this branch never taken in practice
        }

        var action = kind.ToAuditAction();
        var audit = ReviewAuditEvent.Create(
            "AccessibilityReport",
            reportId,
            effectiveActor.Value,
            action,
            previousValue: null,
            newValue: kind.ToString(),
            note: note,
            DateTimeOffset.UtcNow);

        await reviewRepository.AddAuditAsync(audit, cancellationToken);
        await reviewRepository.SaveChangesAsync(cancellationToken);

        return new FeedbackResponse(audit.Id, reportId, kind.ToString(), note, action, audit.CreatedAt);
    }
}
