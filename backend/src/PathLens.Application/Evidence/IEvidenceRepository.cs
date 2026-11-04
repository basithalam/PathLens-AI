using PathLens.Domain.Evidence;

namespace PathLens.Application.Evidence;

public interface IEvidenceRepository
{
    Task<bool> ReportExistsAsync(Guid reportId, CancellationToken cancellationToken);
    Task AddEvidenceAsync(EvidenceItem item, CancellationToken cancellationToken);
    Task<EvidenceItem?> GetEvidenceAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<EvidenceItem>> ListForReportAsync(Guid reportId, CancellationToken cancellationToken);

    /// <summary>
    /// Evidence rows that carry an obstruction measurement for the given reports. Used by routing to say
    /// how much of the walkway a known obstacle occupies. Reports with no measurement are simply absent -
    /// the route explanation then says so instead of inventing a share.
    /// </summary>
    Task<IReadOnlyList<EvidenceItem>> ListMeasuredObstructionsAsync(
        IReadOnlyCollection<Guid> reportIds, CancellationToken cancellationToken);
    Task AddJobAsync(AnalysisJob job, CancellationToken cancellationToken);
    Task<AnalysisJob?> GetJobAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
