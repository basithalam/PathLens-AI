using Microsoft.EntityFrameworkCore;
using PathLens.Application.Comparisons;
using PathLens.Domain.Evidence;
using PathLens.Domain.Reports;
using PathLens.Infrastructure.Persistence;

namespace PathLens.Infrastructure.Cameras;

/// <summary>
/// Runs after a camera snapshot finishes analysis: compares it with the
/// latest prior analysed snapshot from the same camera using the existing
/// comparison engine (Section 18 rules). Camera repositioning therefore
/// yields Inconclusive, never a hidden guess. Runs at most once per pair.
/// </summary>
public sealed class CameraEvidencePostProcessor(
    PathLensDbContext dbContext,
    ComparisonService comparisonService)
{
    public async Task<Guid?> ProcessAsync(EvidenceItem evidence, CancellationToken cancellationToken)
    {
        if (evidence.SourceType != EvidenceSourceType.CameraFeed ||
            evidence.AnalysisStatus != AnalysisStatus.Completed)
        {
            return null;
        }

        var priorQuery = dbContext.EvidenceItems
            .Where(item => item.SourceType == EvidenceSourceType.CameraFeed &&
                           item.Id != evidence.Id &&
                           item.AnalysisStatus == AnalysisStatus.Completed &&
                           item.UploadedAt <= evidence.UploadedAt);

        priorQuery = evidence.CameraSourceId is not null
            ? priorQuery.Where(item => item.CameraSourceId == evidence.CameraSourceId)
            : priorQuery.Where(item => item.ReportId == evidence.ReportId);

        var prior = await priorQuery
            .OrderByDescending(item => item.UploadedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (prior is null)
        {
            return null;
        }

        var alreadyCompared = await dbContext.EvidenceComparisons.AnyAsync(
            comparison => comparison.EarlierEvidenceId == prior.Id &&
                            comparison.LaterEvidenceId == evidence.Id,
            cancellationToken);
        if (alreadyCompared)
        {
            return null;
        }

        var creation = await comparisonService.CreateAsync(prior.Id, evidence.Id, cancellationToken);
        return creation.Response?.Id;
    }
}
