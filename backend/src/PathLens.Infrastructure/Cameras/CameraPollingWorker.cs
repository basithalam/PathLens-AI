using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PathLens.Infrastructure.Cameras;

/// <summary>
/// Checks every few seconds which camera sources are due (per-source
/// cadence, 60s for the NYC profile) and polls them. Does nothing unless
/// Cameras:PollingEnabled is set, so the API host never double-polls.
/// </summary>
public sealed class CameraPollingWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<CameraOptions> options,
    ILogger<CameraPollingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.PollingEnabled)
        {
            logger.LogInformation("Camera polling is disabled (Cameras:PollingEnabled is false).");
            return;
        }

        var checkInterval = TimeSpan.FromSeconds(Math.Max(5, options.Value.PollCheckSeconds));
        logger.LogInformation("Camera polling enabled; checking for due sources every {Seconds}s.", checkInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var ingestion = scope.ServiceProvider.GetRequiredService<CameraIngestionService>();
                var results = await ingestion.PollDueSourcesAsync(stoppingToken);
                foreach (var (sourceId, result) in results)
                {
                    logger.LogInformation(
                        "Camera {SourceId}: {Outcome} — {Message}", sourceId, result.Outcome, result.Message);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Camera polling loop failed.");
            }

            try
            {
                await Task.Delay(checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
