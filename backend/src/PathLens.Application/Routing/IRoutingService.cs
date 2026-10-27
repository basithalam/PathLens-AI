namespace PathLens.Application.Routing;

public sealed record GeoPoint(double Latitude, double Longitude);

public sealed record CandidateRoute(
    IReadOnlyList<GeoPoint> Geometry,
    double DistanceMeters,
    double DurationSeconds);

public sealed record RouteProviderResult(
    string Provider,
    string Profile,
    IReadOnlyList<CandidateRoute> Routes);

/// <summary>Raised when the routing provider cannot produce a route. Never faked away.</summary>
public sealed class RouteProviderUnavailableException(string message, Exception? inner = null)
    : Exception(message, inner);

public interface IRoutingService
{
    Task<RouteProviderResult> GetRoutesAsync(
        GeoPoint origin,
        GeoPoint destination,
        CancellationToken cancellationToken);
}
