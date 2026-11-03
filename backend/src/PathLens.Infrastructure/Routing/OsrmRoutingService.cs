using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PathLens.Application.Routing;

namespace PathLens.Infrastructure.Routing;

/// <summary>
/// Real routing through OSRM. Geometry, distance, and duration come from the
/// provider only; any failure is surfaced as unavailable, never invented.
/// </summary>
public sealed class OsrmRoutingService(
    IHttpClientFactory httpClientFactory,
    IOptions<RoutingOptions> options) : IRoutingService
{
    private readonly RoutingOptions _options = options.Value;

    /// <summary>
    /// The public OSRM demo instance answers anonymous requests with 403 and asks clients to identify
    /// themselves. Anything that calls it - routing or the readiness probe - must send the same identity,
    /// otherwise /health/ready reports the provider as degraded while real routing works fine.
    /// </summary>
    public const string ClientUserAgent = "PathLensAI/0.1 (Meta Global AI Developer Hackathon 2026 prototype)";
    public const string ClientReferer = "https://github.com/basithalam/PathLens-AI";

    public async Task<RouteProviderResult> GetRoutesAsync(
        GeoPoint origin,
        GeoPoint destination,
        CancellationToken cancellationToken)
    {
        var baseUrl = _options.OsrmBaseUrl.TrimEnd('/');
        var profile = string.IsNullOrWhiteSpace(_options.Profile) ? "foot" : _options.Profile.Trim();

        var url = string.Create(CultureInfo.InvariantCulture,
            $"{baseUrl}/route/v1/{profile}/{origin.Longitude},{origin.Latitude};{destination.Longitude},{destination.Latitude}?overview=full&geometries=geojson&alternatives=2");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds)));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            var client = httpClientFactory.CreateClient(nameof(OsrmRoutingService));
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            // OSRM's public service asks clients to identify themselves.
            httpRequest.Headers.UserAgent.ParseAdd(ClientUserAgent);
            httpRequest.Headers.Referrer = new Uri(ClientReferer);
            using var response = await client.SendAsync(httpRequest, linked.Token);
            var body = await response.Content.ReadAsStringAsync(linked.Token);

            if (!response.IsSuccessStatusCode)
            {
                throw new RouteProviderUnavailableException(
                    $"Routing provider returned status {(int)response.StatusCode}.");
            }

            return ParseResponse(body, profile);
        }
        catch (RouteProviderUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            throw new RouteProviderUnavailableException(
                "The routing provider is unavailable right now. No route can be shown instead of inventing one.", ex);
        }
    }

    public static RouteProviderResult ParseResponse(string json, string profile)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
            {
                throw new RouteProviderUnavailableException("The routing provider returned no route for this trip.");
            }

            var candidates = new List<CandidateRoute>();
            foreach (var route in routes.EnumerateArray())
            {
                var coordinates = route.GetProperty("geometry").GetProperty("coordinates");
                var geometry = new List<GeoPoint>();
                foreach (var coordinate in coordinates.EnumerateArray())
                {
                    // GeoJSON order is [longitude, latitude].
                    geometry.Add(new GeoPoint(coordinate[1].GetDouble(), coordinate[0].GetDouble()));
                }

                if (geometry.Count < 2)
                {
                    continue;
                }

                candidates.Add(new CandidateRoute(
                    geometry,
                    route.GetProperty("distance").GetDouble(),
                    route.GetProperty("duration").GetDouble()));
            }

            if (candidates.Count == 0)
            {
                throw new RouteProviderUnavailableException("The routing provider returned no usable route geometry.");
            }

            return new RouteProviderResult("OSRM", profile, candidates);
        }
        catch (RouteProviderUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new RouteProviderUnavailableException("The routing provider returned an unusable response.", ex);
        }
    }
}
