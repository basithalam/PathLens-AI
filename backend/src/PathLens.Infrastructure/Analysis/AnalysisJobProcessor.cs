using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PathLens.Application.Ai;
using PathLens.Application.Storage;
using PathLens.Domain.Evidence;
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

        // H3.2 Distributed lock: SKIP LOCKED so two API/worker instances against the same DB
        // never process the same Pending job. Transaction holds the row lock only while claimed.
        AnalysisJob? job = null;
        EvidenceItem? evidence = null;
        await using (var tx = await dbContext.Database.BeginTransactionAsync(stoppingToken))
        {
            try
            {
                job = await dbContext.AnalysisJobs
                    .FromSqlRaw("""
                        SELECT * FROM "analysis_jobs"
                        WHERE "Status" = 'Pending'
                        ORDER BY "CreatedAt"
                        LIMIT 1
                        FOR UPDATE SKIP LOCKED
                        """)
                    .FirstOrDefaultAsync(stoppingToken);
            }
            catch
            {
                // Fallback for DBs without SKIP LOCKED support (older PG or test provider)
                job = await dbContext.AnalysisJobs
                    .Where(candidate => candidate.Status == AnalysisStatus.Pending)
                    .OrderBy(candidate => candidate.CreatedAt)
                    .FirstOrDefaultAsync(stoppingToken);
            }

            if (job is null)
            {
                await tx.RollbackAsync(stoppingToken);
                return;
            }

            evidence = await dbContext.EvidenceItems
                .FirstOrDefaultAsync(item => item.Id == job.EvidenceItemId, stoppingToken);
            if (evidence is null)
            {
                job.MarkFailed("Evidence item no longer exists.", DateTimeOffset.UtcNow);
                await dbContext.SaveChangesAsync(stoppingToken);
                await tx.CommitAsync(stoppingToken);
                return;
            }

            job.MarkProcessing(DateTimeOffset.UtcNow);
            evidence.MarkAnalysisProcessing();
            await dbContext.SaveChangesAsync(stoppingToken);
            await tx.CommitAsync(stoppingToken);
        }

        if (job is null || evidence is null) return;
        AnalysisJob currentJob = job!;
        EvidenceItem currentEvidence = evidence!;

        // Quality gate (F3): an unsuitable camera frame is stored evidence,
        // but no AI call is spent on it. The job records why, and the pair
        // comparison (if any) stays Inconclusive rather than guessed.
        if (currentEvidence.SourceType == PathLens.Domain.Evidence.EvidenceSourceType.CameraFeed &&
            currentEvidence.IsUnsuitableForAnalysis)
        {
            currentJob.MarkFailed($"Camera frame not analysed: {currentEvidence.QualityNote ?? "frame quality too low."}",
                DateTimeOffset.UtcNow);
            currentEvidence.MarkAnalysisFailed();
            await dbContext.SaveChangesAsync(stoppingToken);

            try
            {
                var postProcessor = scope.ServiceProvider
                    .GetRequiredService<Cameras.CameraEvidencePostProcessor>();
                await postProcessor.ProcessUnsuitableAsync(currentEvidence, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Camera unsuitable-frame comparison failed for evidence {EvidenceId}.", currentEvidence.Id);
            }

            return;
        }

        try
        {
            var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
            await using var stream = await storage.OpenReadAsync(currentEvidence.StorageKey, stoppingToken)
                ?? throw new InvalidOperationException("The evidence file is missing from storage.");

            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, stoppingToken);
            var bytes = memory.ToArray();

            var analysisService = scope.ServiceProvider.GetRequiredService<IImageAnalysisService>();
            var result = await analysisService.AnalyzeAsync(
                new ImageAnalysisRequest(bytes, currentEvidence.ContentType), stoppingToken);

            currentEvidence.MarkAnalysisCompleted(
                ImageAnalysisResultParser.Serialize(result),
                result.Provider,
                result.Model,
                DateTimeOffset.UtcNow);

            // Keep the parent report's evidence freshness up to date.
            var report = await dbContext.Reports.FirstOrDefaultAsync(
                candidate => candidate.Id == currentEvidence.ReportId, stoppingToken);
            report?.RecordEvidence(currentEvidence.UploadedAt);

            currentJob.MarkCompleted(DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync(stoppingToken);

            // Camera snapshots: auto-compare with the prior snapshot from the
            // same camera. A failure here must not fail the analysis itself.
            if (currentEvidence.SourceType == PathLens.Domain.Evidence.EvidenceSourceType.CameraFeed)
            {
                try
                {
                    var postProcessor = scope.ServiceProvider
                        .GetRequiredService<Cameras.CameraEvidencePostProcessor>();
                    await postProcessor.ProcessAsync(currentEvidence, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Camera auto-comparison failed for evidence {EvidenceId}.", currentEvidence.Id);
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException or TaskCanceledException)
        {
            var isRateLimited = IsRateLimited(ex);
            if (isRateLimited && currentJob.Attempts < MaxAttempts && !stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Rate-limited (429) for job {JobId}; delaying 5s before requeue (free tier 10/min).", currentJob.Id);
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); } catch (OperationCanceledException) { }
                currentJob.Requeue($"Rate-limited (429), retrying after 5s delay: {ex.Message}");
                currentEvidence.ResetForAnalysis();
            }
            else if (currentJob.Attempts < MaxAttempts && !stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Transient analysis failure for job {JobId}; requeueing.", currentJob.Id);
                currentJob.Requeue($"Transient failure, retrying: {ex.Message}");
                currentEvidence.ResetForAnalysis();
            }
            else
            {
                logger.LogWarning(ex, "Analysis job {JobId} failed.", currentJob.Id);
                currentJob.MarkFailed(ex.Message, DateTimeOffset.UtcNow);
                currentEvidence.MarkAnalysisFailed();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Analysis job {JobId} failed.", currentJob.Id);
            currentJob.MarkFailed(ex.Message, DateTimeOffset.UtcNow);
            currentEvidence.MarkAnalysisFailed();
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }

    private static bool IsRateLimited(Exception ex)
    {
        // Groq/Meta OpenAI-compatible returns 429 Too Many Requests with body; HttpRequestException message includes status.
        if (ex is HttpRequestException httpEx)
        {
            var msg = httpEx.Message ?? string.Empty;
            if (msg.Contains("429") || msg.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase) || msg.Contains("rate", StringComparison.OrdinalIgnoreCase) && msg.Contains("limit", StringComparison.OrdinalIgnoreCase))
                return true;
            if (httpEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                return true;
        }
        var text = ex.Message ?? string.Empty;
        return text.Contains("429") || text.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase);
    }
}
