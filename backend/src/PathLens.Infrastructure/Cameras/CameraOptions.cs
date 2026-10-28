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
}
