using PathLens.Application.Routing;

namespace PathLens.Infrastructure.Routing;

/// <summary>
/// Deterministic routing for automated tests only. Produces straight-line
/// geometry; never used in production (provider name makes that visible).
/// </summary>
public sealed class MockRoutingService : IRoutingService
{
    public Task<RouteProviderResult> GetRoutesAsync(
        GeoPoint origin,
        GeoPoint destination,
        CancellationToken cancellationToken)
    {
        var direct = Interpolate(origin, destination, bend: 0);
        var detour = Interpolate(origin, destination, bend: 0.004);

        var result = new RouteProviderResult("MockRouting", "foot", new[]
        {
            new CandidateRoute(direct, RouteGeometry.RouteLengthMeters(direct), RouteGeometry.RouteLengthMeters(direct) / 1.2),
            new CandidateRoute(detour, RouteGeometry.RouteLengthMeters(detour), RouteGeometry.RouteLengthMeters(detour) / 1.2),
        });

        return Task.FromResult(result);
    }

    private static IReadOnlyList<GeoPoint> Interpolate(GeoPoint origin, GeoPoint destination, double bend)
    {
        var points = new List<GeoPoint>();
        const int steps = 10;
        for (var i = 0; i <= steps; i++)
        {
            var fraction = i / (double)steps;
            var latitude = origin.Latitude + (destination.Latitude - origin.Latitude) * fraction;
            var longitude = origin.Longitude + (destination.Longitude - origin.Longitude) * fraction;

            if (i is > 0 and < steps)
            {
                // Bend sideways (longitude) so the detour is a genuinely
                // different path, not the same line walked at another pace.
                longitude += bend * Math.Sin(fraction * Math.PI);
            }

            points.Add(new GeoPoint(latitude, longitude));
        }

        return points;
    }
}
