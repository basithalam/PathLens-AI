namespace PathLens.Infrastructure.Ai;

/// <summary>
/// SAM 3.1 configuration. The key is the same server-side Meta credential used for analysis and is
/// never read by the frontend. Measurement is opt-in: with <see cref="Enabled"/> false the pipeline
/// behaves exactly as it did before SAM was added.
/// </summary>
public sealed class Sam31Options
{
    public const string SectionName = "Sam31";

    /// <summary>Off by default so an unprobed or unbilled model can never silently change demo output.</summary>
    public bool Enabled { get; set; }

    /// <summary>Responses endpoint, e.g. https://api.meta.ai/v1/responses (measured 2026-11-03).</summary>
    public string Endpoint { get; set; } = "https://api.meta.ai/v1/responses";

    public string Model { get; set; } = "sam-3.1";

    /// <summary>Reuses the MetaAi key unless a separate one is configured.</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Populated from <c>MetaAi:ApiKey</c> at registration time so one credential serves both calls;
    /// never read from the frontend and never committed.
    /// </summary>
    public string? FallbackApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Concept prompt for the walkable region. Measured on 2026-11-03: "sidewalk" and
    /// "the paved pedestrian walkway" both returned the same dominant box (53.8% of the frame),
    /// so either phrasing is stable for the walkway itself.
    /// </summary>
    public string WalkwayConcept { get; set; } = "the paved pedestrian walkway";
}
