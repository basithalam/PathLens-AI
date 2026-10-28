namespace PathLens.Application.Routing;

public sealed record RecommendRouteRequest(
    double OriginLatitude,
    double OriginLongitude,
    double DestinationLatitude,
    double DestinationLongitude,
    RoutePreferences? Preferences = null);

/// <summary>
/// Traveller preferences. They reorder ranking only; they never hide a
/// known obstacle or turn unknown into clear. Defaults: stairs are a
/// hard constraint, the other preferences are off.
/// </summary>
public sealed record RoutePreferences(
    bool AvoidStairs = true,
    bool PreferVerifiedEvidence = false,
    bool AvoidStaleEvidence = false,
    bool PreferShortest = false)
{
    public static RoutePreferences Default => new();

    public IReadOnlyList<string> Describe() => new[]
    {
        AvoidStairs
            ? "Avoid stairs: hard constraint — a route with a human-verified stair report cannot outrank one without."
            : "Avoid stairs: off — stair reports count as ordinary obstacles.",
        PreferVerifiedEvidence
            ? "Prefer verified evidence: routes with fewer uncertain obstacles rank higher."
            : "Prefer verified evidence: off.",
        AvoidStaleEvidence
            ? $"Avoid stale evidence: routes with fewer stale obstacles (over {RouteRecommendationService.StaleEvidenceDays} days) rank higher."
            : "Avoid stale evidence: off.",
        PreferShortest
            ? "Prefer shortest: distance counts earlier in ranking."
            : "Prefer shortest: off — distance is the last tiebreak.",
    };
}
