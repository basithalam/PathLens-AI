using Microsoft.EntityFrameworkCore;
using PathLens.Application.Comparisons;
using PathLens.Domain.Evidence;
using PathLens.Domain.Reports;

namespace PathLens.Infrastructure.Persistence;

public sealed class ComparisonRepository(PathLensDbContext dbContext) : IComparisonRepository
{
    public Task<EvidenceItem?> GetEvidenceAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.EvidenceItems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<AccessibilityReport?> GetReportAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Reports.FirstOrDefaultAsync(report => report.Id == id, cancellationToken);

    public async Task AddComparisonAsync(EvidenceComparison comparison, CancellationToken cancellationToken) =>
        await dbContext.EvidenceComparisons.AddAsync(comparison, cancellationToken);

    public Task<EvidenceComparison?> GetComparisonAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.EvidenceComparisons.FirstOrDefaultAsync(comparison => comparison.Id == id, cancellationToken);

    public async Task<IReadOnlyList<EvidenceComparison>> ListForReportAsync(
        Guid reportId, CancellationToken cancellationToken) =>
        await dbContext.EvidenceComparisons.AsNoTracking()
            .Where(comparison => comparison.ReportId == reportId)
            .OrderByDescending(comparison => comparison.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
