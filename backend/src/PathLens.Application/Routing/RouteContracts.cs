namespace PathLens.Application.Routing;

public sealed record RecommendRouteRequest(
    double OriginLatitude,
    double OriginLongitude,
    double DestinationLatitude,
    double DestinationLongitude);
