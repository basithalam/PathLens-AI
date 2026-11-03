namespace PathLens.Application.Ai;

/// <summary>
/// A measured obstruction share for one evidence image. Every number here comes from deterministic
/// geometry over boxes SAM 3.1 returned - the model itself supplies no score and no distance.
/// </summary>
/// <param name="WalkwayOccupancyPercent">
/// Share of the visible walkway region covered by at least one obstacle box, 1..100.
/// </param>
/// <param name="ObstacleBoxCount">How many obstacle boxes the concept prompt returned.</param>
/// <param name="WalkwayConcept">The exact concept text used for the walkway prompt.</param>
/// <param name="ObstacleConcept">The exact concept text used for the obstacle prompt.</param>
/// <param name="FrameWidth">Pixel width of the image as SAM saw it, so the boxes can be re-drawn.</param>
/// <param name="FrameHeight">Pixel height of the image as SAM saw it.</param>
/// <param name="ObstacleBoxes">The obstacle boxes, in the same pixel space, for overlay rendering.</param>
/// <param name="WalkwayBoxes">The walkway boxes, largest first.</param>
/// <param name="Provider">Always "SAM 3.1" - a different provider would need its own contract.</param>
/// <param name="Model">Model id the response came from.</param>
/// <param name="MeasuredAt">UTC instant of the measurement.</param>
public sealed record ObstructionMeasurement(
    int WalkwayOccupancyPercent,
    int ObstacleBoxCount,
    string WalkwayConcept,
    string ObstacleConcept,
    double FrameWidth,
    double FrameHeight,
    IReadOnlyList<PixelBox> ObstacleBoxes,
    IReadOnlyList<PixelBox> WalkwayBoxes,
    string Provider,
    string Model,
    DateTimeOffset MeasuredAt);

/// <summary>
/// Measures how much of the walkable region in a photo is occupied. Deliberately separate from
/// <see cref="IImageAnalysisService"/>: classification and measurement fail for different reasons,
/// and a failed measurement must leave the classification standing on its own.
/// </summary>
public interface ISceneMeasurementService
{
    string ProviderName { get; }

    /// <summary>
    /// Returns null when nothing can be honestly measured - not configured, no walkway found, or the
    /// obstacle concept returned no box. Callers must treat null as "unknown", never as "clear".
    /// </summary>
    Task<ObstructionMeasurement?> MeasureAsync(
        ImageAnalysisRequest request,
        string obstacleConcept,
        CancellationToken cancellationToken);
}
