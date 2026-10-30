using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PathLens.Infrastructure.Routing;

namespace PathLens.Infrastructure.Health;

/// <summary>H5.3 extended /health/ready — pings OSRM (best-effort, degraded not unhealthy).</summary>
public sealed class OsrmHealthCheck(IOptions<RoutingOptions> options, IHttpClientFactory httpFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = options.Value.OsrmBaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/route/v1/foot/0,0;0,0?overview=false";
            var client = httpFactory.CreateClient(nameof(OsrmHealthCheck));
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            var resp = await client.GetAsync(url, linked.Token);
            // 400 on 0,0 is expected (no route) — still proves reachability
            return resp.IsSuccessStatusCode || (int)resp.StatusCode == 400
                ? HealthCheckResult.Healthy($"OSRM reachable at {baseUrl} (status {(int)resp.StatusCode}).")
                : HealthCheckResult.Degraded($"OSRM returned {(int)resp.StatusCode} at {baseUrl}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded($"OSRM not reachable: {ex.Message}", ex);
        }
    }
}

public sealed class AiProviderHealthCheck(IConfiguration config) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var provider = config["Ai:Provider"] ?? "Meta";
        var endpoint = config["MetaAi:Endpoint"];
        var model = config["MetaAi:Model"];
        var hasKey = !string.IsNullOrWhiteSpace(config["MetaAi:ApiKey"]);
        if (string.Equals(provider, "Mock", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(HealthCheckResult.Healthy("AI provider: Mock (pipeline only)."));
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(model) || !hasKey)
            return Task.FromResult(HealthCheckResult.Degraded("AI provider: not fully configured (endpoint/model/key missing) — live analysis will fail, Mock still works."));
        return Task.FromResult(HealthCheckResult.Healthy($"AI provider: {provider} {model} @ {endpoint} (key present)."));
    }
}
