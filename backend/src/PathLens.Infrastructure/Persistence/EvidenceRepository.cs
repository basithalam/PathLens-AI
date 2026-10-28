using Microsoft.EntityFrameworkCore;
using PathLens.Application.Evidence;
using PathLens.Domain.Evidence;

namespace PathLens.Infrastructure.Persistence;

public sealed class EvidenceRepository(PathLensDbContext dbContext) : IEvidenceRepository
{
    public Task<bool> ReportExistsAsync(Guid reportId, CancellationToken cancellationToken) =>
        dbContext.Reports.AnyAsync(report => report.Id == reportId, cancellationToken);

    public async Task AddEvidenceAsync(EvidenceItem item, CancellationToken cancellationToken)
    {
        await dbContext.EvidenceItems.AddAsync(item, cancellationToken);

        // Evidence freshness starts at upload, not only after a successful analysis.
        var report = await dbContext.Reports.FirstOrDefaultAsync(
            candidate => candidate.Id == item.ReportId, cancellationToken);
        report?.RecordEvidence(item.UploadedAt);
    }

    public Task<EvidenceItem?> GetEvidenceAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.EvidenceItems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<IReadOnlyList<EvidenceItem>> ListForReportAsync(
        Guid reportId, CancellationToken cancellationToken) =>
        await dbContext.EvidenceItems.AsNoTracking()
            .Where(item => item.ReportId == reportId)
            .OrderByDescending(item => item.UploadedAt)
            .ToListAsync(cancellationToken);

    public async Task AddJobAsync(AnalysisJob job, CancellationToken cancellationToken) =>
        await dbContext.AnalysisJobs.AddAsync(job, cancellationToken);

    public Task<AnalysisJob?> GetJobAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.AnalysisJobs.FirstOrDefaultAsync(job => job.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
