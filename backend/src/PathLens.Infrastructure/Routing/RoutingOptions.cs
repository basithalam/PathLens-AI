namespace PathLens.Infrastructure.Routing;

public sealed class RoutingOptions
{
    public const string SectionName = "Routing";

    /// <summary>"OSRM" for the real provider, "Mock" only for tests or a labelled demo.</summary>
    public string Provider { get; set; } = "OSRM";

    public string OsrmBaseUrl { get; set; } = "https://router.project-osrm.org";

    /// <summary>OSRM profile. "foot" is a walking profile; it is not a validated wheelchair profile.</summary>
    public string Profile { get; set; } = "foot";

    public int TimeoutSeconds { get; set; } = 20;
}
