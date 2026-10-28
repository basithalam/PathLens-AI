using PathLens.Domain.Evidence;
using PathLens.Domain.Reports;

namespace PathLens.Application.Comparisons;

public interface IComparisonRepository
{
    Task<EvidenceItem?> GetEvidenceAsync(Guid id, CancellationToken cancellationToken);
    Task<AccessibilityReport?> GetReportAsync(Guid id, CancellationToken cancellationToken);
    Task AddComparisonAsync(EvidenceComparison comparison, CancellationToken cancellationToken);
    Task<EvidenceComparison?> GetComparisonAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<EvidenceComparison>> ListForReportAsync(Guid reportId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EvidenceComparison>> ListByEffectiveOutcomeAsync(
        Domain.Reports.EvidenceComparisonOutcome outcome, int limit, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
