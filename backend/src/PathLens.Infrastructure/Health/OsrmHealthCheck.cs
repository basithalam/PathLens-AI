using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PathLens.Infrastructure.Routing;

namespace PathLens.Infrastructure.Health;

/// <summary>H5.3 extended /health/ready — pings OSRM (best-effort, degraded not unhealthy).</summary>
public sealed class OsrmHealthCheck(IOptions<RoutingOptions> options, IHttpClientFactory httpFactory) : IHealthCheck
{
    // A real, routable pair in the pilot city. Probing 0,0 only proves the host answers;
    // this proves the same code path the route planner uses.
    private const string ProbeCoordinates = "37.7749,-122.4194;37.7785,-122.4135";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = options.Value.OsrmBaseUrl.TrimEnd('/');
            var profile = string.IsNullOrWhiteSpace(options.Value.Profile) ? "foot" : options.Value.Profile.Trim();
            var url = string.Create(CultureInfo.InvariantCulture,
                $"{baseUrl}/route/v1/{profile}/{ProbeCoordinates}?overview=false");

            var client = httpFactory.CreateClient(nameof(OsrmHealthCheck));
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            // Same identity as OsrmRoutingService: the public demo instance answers anonymous
            // requests with 403, which used to report a working router as degraded.
            request.Headers.UserAgent.ParseAdd(OsrmRoutingService.ClientUserAgent);
            request.Headers.Referrer = new Uri(OsrmRoutingService.ClientReferer);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            var resp = await client.SendAsync(request, linked.Token);

            // 400 still proves the service is reachable and just refuses that pair.
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
            return Task.FromResult(HealthCheckResult.Healthy("AI provider: Mock (pipeline only, not real AI). Set Ai:Provider=Meta + MetaAi:Endpoint/Model/ApiKey for live (G1)."));
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(model) || !hasKey)
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(endpoint)) missing.Add("MetaAi:Endpoint");
            if (string.IsNullOrWhiteSpace(model)) missing.Add("MetaAi:Model");
            if (!hasKey) missing.Add("MetaAi:ApiKey");
            return Task.FromResult(HealthCheckResult.Degraded($"AI provider: not fully configured — missing {string.Join(", ", missing)} — live analysis will fail with 502 (see docs/AI_GATE_A_REPORT.md G1, set .local/set-meta-env.ps1 or GitHub Secrets). Mock still works."));
        }
        return Task.FromResult(HealthCheckResult.Healthy($"AI provider: {provider} {model} @ {endpoint} (key present, G1 ready)."));
    }
}
