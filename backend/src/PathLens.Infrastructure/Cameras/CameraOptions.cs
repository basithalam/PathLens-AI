namespace PathLens.Infrastructure.Cameras;

public sealed class CameraOptions
{
    public const string SectionName = "Cameras";

    /// <summary>Polling runs only where this is explicitly enabled (Worker host).</summary>
    public bool PollingEnabled { get; set; }

    /// <summary>How often the worker checks which sources are due.</summary>
    public int PollCheckSeconds { get; set; } = 15;

    /// <summary>
    /// Maximum analysed snapshots per camera per UTC day. When reached, a
    /// changed snapshot is acknowledged but not stored or analysed, and the
    /// skip is reported on the source instead of spending silently.
    /// Zero or negative means unlimited.
    /// </summary>
    public int DailyAnalysisBudgetPerCamera { get; set; } = 240;

    /// <summary>Pre-AI frame quality gate for camera snapshots (Phase F3).</summary>
    public bool QualityGateEnabled { get; set; } = true;

    /// <summary>Mean brightness (0-255) below this is too dark to judge.</summary>
    public double MinBrightness { get; set; } = 10;

    /// <summary>Laplacian-variance sharpness proxy below this is too blurred/flat to judge.</summary>
    public double MinSharpness { get; set; } = 2;

    /// <summary>Perceptual-hash Hamming distance above this means the camera view changed.</summary>
    public int ViewChangeHashDistance { get; set; } = 30;

    /// <summary>Optional bearer token for providers that require it (Datumfeed, 511NY/SF). Leave empty if the feed is public.</summary>
    public string? DatumfeedApiKey { get; set; }
}
