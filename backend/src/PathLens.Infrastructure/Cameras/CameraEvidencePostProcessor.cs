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

        var prior = await FindPriorAsync(evidence, cancellationToken);
        if (prior is null || await AlreadyComparedAsync(prior, evidence, cancellationToken))
        {
            return null;
        }

        // View-change gate (F3): the two frames may show different places,
        // so no change conclusion is drawn whatever the AI saw.
        if (evidence.QualityFlag == FrameQuality.ViewChanged)
        {
            var comparison = EvidenceComparison.Create(
                evidence.ReportId,
                prior.Id,
                evidence.Id,
                EvidenceComparisonOutcome.Inconclusive,
                "Comparison is inconclusive because the camera view appears to have changed between the two snapshots " +
                $"(perceptual distance gate). {evidence.QualityNote} The camera may have been repositioned, so the frames must not be compared as the same place.",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    gate = "ViewChanged",
                    qualityNote = evidence.QualityNote,
                    earlierPerceptualHash = prior.PerceptualHash,
                    laterPerceptualHash = evidence.PerceptualHash,
                    note = "NotObservedInNewImage never means an obstacle was removed.",
                }),
                null,
                DateTimeOffset.UtcNow);

            await dbContext.EvidenceComparisons.AddAsync(comparison, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return comparison.Id;
        }

        var creation = await comparisonService.CreateAsync(prior.Id, evidence.Id, cancellationToken);
        return creation.Response?.Id;
    }

    /// <summary>
    /// The frame was too dark/blurred for AI. Still record the pair outcome:
    /// the engine returns Inconclusive for an unanalysed later photo, and the
    /// job/evidence already records the quality reason.
    /// </summary>
    public async Task<Guid?> ProcessUnsuitableAsync(EvidenceItem evidence, CancellationToken cancellationToken)
    {
        if (evidence.SourceType != EvidenceSourceType.CameraFeed)
        {
            return null;
        }

        var prior = await FindPriorAsync(evidence, cancellationToken);
        if (prior is null || await AlreadyComparedAsync(prior, evidence, cancellationToken))
        {
            return null;
        }

        var creation = await comparisonService.CreateAsync(prior.Id, evidence.Id, cancellationToken);
        return creation.Response?.Id;
    }

    private async Task<EvidenceItem?> FindPriorAsync(EvidenceItem evidence, CancellationToken cancellationToken)
    {
        var priorQuery = dbContext.EvidenceItems
            .Where(item => item.SourceType == EvidenceSourceType.CameraFeed &&
                           item.Id != evidence.Id &&
                           item.AnalysisStatus == AnalysisStatus.Completed &&
                           item.UploadedAt <= evidence.UploadedAt);

        priorQuery = evidence.CameraSourceId is not null
            ? priorQuery.Where(item => item.CameraSourceId == evidence.CameraSourceId)
            : priorQuery.Where(item => item.ReportId == evidence.ReportId);

        return await priorQuery
            .OrderByDescending(item => item.UploadedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> AlreadyComparedAsync(
        EvidenceItem prior, EvidenceItem evidence, CancellationToken cancellationToken) =>
        await dbContext.EvidenceComparisons.AnyAsync(
            comparison => comparison.EarlierEvidenceId == prior.Id &&
                            comparison.LaterEvidenceId == evidence.Id,
            cancellationToken);
}
