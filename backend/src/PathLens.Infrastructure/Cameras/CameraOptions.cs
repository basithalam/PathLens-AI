namespace PathLens.Infrastructure.Cameras;

public sealed class CameraOptions
{
    public const string SectionName = "Cameras";

    /// <summary>Polling runs only where this is explicitly enabled (Worker host).</summary>
    public bool PollingEnabled { get; set; }

    /// <summary>How often the worker checks which sources are due.</summary>
    public int PollCheckSeconds { get; set; } = 15;
}
