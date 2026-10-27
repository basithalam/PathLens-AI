using PathLens.Domain.Evidence;

namespace PathLens.Application.Evidence;

public interface IEvidenceRepository
{
    Task<bool> ReportExistsAsync(Guid reportId, CancellationToken cancellationToken);
    Task AddEvidenceAsync(EvidenceItem item, CancellationToken cancellationToken);
    Task<EvidenceItem?> GetEvidenceAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<EvidenceItem>> ListForReportAsync(Guid reportId, CancellationToken cancellationToken);
    Task AddJobAsync(AnalysisJob job, CancellationToken cancellationToken);
    Task<AnalysisJob?> GetJobAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
