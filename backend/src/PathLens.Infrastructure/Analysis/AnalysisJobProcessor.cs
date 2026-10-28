using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PathLens.Application.Ai;
using PathLens.Application.Storage;
using PathLens.Domain.Reports;
using PathLens.Infrastructure.Persistence;

namespace PathLens.Infrastructure.Analysis;

/// <summary>
/// Polls the database for pending analysis jobs and runs them one at a time.
/// Transient provider failures are retried once (bounded); everything else
/// fails honestly with the reason stored on the job.
/// </summary>
public sealed class AnalysisJobProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<AnalysisJobProcessor> logger) : BackgroundService
{
    private const int MaxAttempts = 2;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextJobAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Analysis job processing loop failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessNextJobAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PathLensDbContext>();

        var job = await dbContext.AnalysisJobs
            .Where(candidate => candidate.Status == AnalysisStatus.Pending)
            .OrderBy(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(stoppingToken);

        if (job is null)
        {
            return;
        }

        var evidence = await dbContext.EvidenceItems
            .FirstOrDefaultAsync(item => item.Id == job.EvidenceItemId, stoppingToken);
        if (evidence is null)
        {
            job.MarkFailed("Evidence item no longer exists.", DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync(stoppingToken);
            return;
        }

        job.MarkProcessing(DateTimeOffset.UtcNow);
        evidence.MarkAnalysisProcessing();
        await dbContext.SaveChangesAsync(stoppingToken);

        // Quality gate (F3): an unsuitable camera frame is stored evidence,
        // but no AI call is spent on it. The job records why, and the pair
        // comparison (if any) stays Inconclusive rather than guessed.
        if (evidence.SourceType == PathLens.Domain.Evidence.EvidenceSourceType.CameraFeed &&
            evidence.IsUnsuitableForAnalysis)
        {
            job.MarkFailed(
                $"Camera frame not analysed: {evidence.QualityNote ?? "frame quality too low."}",
                DateTimeOffset.UtcNow);
            evidence.MarkAnalysisFailed();
            await dbContext.SaveChangesAsync(stoppingToken);

            try
            {
                var postProcessor = scope.ServiceProvider
                    .GetRequiredService<Cameras.CameraEvidencePostProcessor>();
                await postProcessor.ProcessUnsuitableAsync(evidence, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Camera unsuitable-frame comparison failed for evidence {EvidenceId}.", evidence.Id);
            }

            return;
        }

        try
        {
            var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
            await using var stream = await storage.OpenReadAsync(evidence.StorageKey, stoppingToken)
                ?? throw new InvalidOperationException("The evidence file is missing from storage.");

            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, stoppingToken);
            var bytes = memory.ToArray();

            var analysisService = scope.ServiceProvider.GetRequiredService<IImageAnalysisService>();
            var result = await analysisService.AnalyzeAsync(
                new ImageAnalysisRequest(bytes, evidence.ContentType), stoppingToken);

            evidence.MarkAnalysisCompleted(
                ImageAnalysisResultParser.Serialize(result),
                result.Provider,
                result.Model,
                DateTimeOffset.UtcNow);

            // Keep the parent report's evidence freshness up to date.
            var report = await dbContext.Reports.FirstOrDefaultAsync(
                candidate => candidate.Id == evidence.ReportId, stoppingToken);
            report?.RecordEvidence(evidence.UploadedAt);

            job.MarkCompleted(DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync(stoppingToken);

            // Camera snapshots: auto-compare with the prior snapshot from the
            // same camera. A failure here must not fail the analysis itself.
            if (evidence.SourceType == PathLens.Domain.Evidence.EvidenceSourceType.CameraFeed)
            {
                try
                {
                    var postProcessor = scope.ServiceProvider
                        .GetRequiredService<Cameras.CameraEvidencePostProcessor>();
                    await postProcessor.ProcessAsync(evidence, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Camera auto-comparison failed for evidence {EvidenceId}.", evidence.Id);
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException or TaskCanceledException)
        {
            if (job.Attempts < MaxAttempts && !stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Transient analysis failure for job {JobId}; requeueing.", job.Id);
                job.Requeue($"Transient failure, retrying: {ex.Message}");
                evidence.ResetForAnalysis();
            }
            else
            {
                logger.LogWarning(ex, "Analysis job {JobId} failed.", job.Id);
                job.MarkFailed(ex.Message, DateTimeOffset.UtcNow);
                evidence.MarkAnalysisFailed();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Analysis job {JobId} failed.", job.Id);
            job.MarkFailed(ex.Message, DateTimeOffset.UtcNow);
            evidence.MarkAnalysisFailed();
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }
}
