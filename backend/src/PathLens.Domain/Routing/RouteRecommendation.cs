namespace PathLens.Domain.Routing;

/// <summary>
/// A stored route recommendation: the request and the full evaluated
/// response (as JSON), so a recommendation can be reopened and audited.
/// </summary>
public sealed class RouteRecommendationRecord
{
    private RouteRecommendationRecord()
    {
    }

    public Guid Id { get; private set; }
    public double OriginLatitude { get; private set; }
    public double OriginLongitude { get; private set; }
    public double DestinationLatitude { get; private set; }
    public double DestinationLongitude { get; private set; }
    public string Profile { get; private set; } = string.Empty;
    public string Provider { get; private set; } = string.Empty;
    public string ResponseJson { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static RouteRecommendationRecord Create(
        Guid id,
        double originLatitude,
        double originLongitude,
        double destinationLatitude,
        double destinationLongitude,
        string profile,
        string provider,
        string responseJson,
        DateTimeOffset now) => new()
        {
            Id = id,
            OriginLatitude = originLatitude,
            OriginLongitude = originLongitude,
            DestinationLatitude = destinationLatitude,
            DestinationLongitude = destinationLongitude,
            Profile = profile,
            Provider = provider,
            ResponseJson = responseJson,
            CreatedAt = now,
        };
}
