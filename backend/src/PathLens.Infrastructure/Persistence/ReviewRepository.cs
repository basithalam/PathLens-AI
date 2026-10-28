using Microsoft.EntityFrameworkCore;
using PathLens.Application.Reviews;
using PathLens.Domain.Users;

namespace PathLens.Infrastructure.Persistence;

public sealed class ReviewRepository(PathLensDbContext dbContext) : IReviewRepository
{
    public async Task AddAuditAsync(ReviewAuditEvent auditEvent, CancellationToken cancellationToken) =>
        await dbContext.ReviewAuditEvents.AddAsync(auditEvent, cancellationToken);

    public async Task<IReadOnlyList<ReviewAuditEvent>> ListAuditForEntityAsync(
        string entityType, Guid entityId, CancellationToken cancellationToken) =>
        await dbContext.ReviewAuditEvents.AsNoTracking()
            .Where(auditEvent => auditEvent.EntityType == entityType && auditEvent.EntityId == entityId)
            .OrderByDescending(auditEvent => auditEvent.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReviewAuditEvent>> ListRecentAuditAsync(
        int limit, CancellationToken cancellationToken) =>
        await dbContext.ReviewAuditEvents.AsNoTracking()
            .OrderByDescending(auditEvent => auditEvent.CreatedAt)
            .Take(Math.Clamp(limit, 1, 200))
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
