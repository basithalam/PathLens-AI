using PathLens.Domain.Reports;

namespace PathLens.Application.Ai;

/// <summary>
/// The concept phrase SAM 3.1 is asked for, per obstacle type.
///
/// These are deliberately full phrases, not the bare noun. Measured on 2026-11-03 against a photo of
/// three fallen scooters: the concept <c>scooter</c> returned <b>zero</b> boxes, while
/// <c>scooters blocking the sidewalk</c> returned two. A bare noun therefore cannot be used - it would
/// read as "nothing there" on exactly the images that need measuring.
///
/// Unknown returns null: there is no honest concept to ask for when nobody has said what the obstacle is.
/// </summary>
public static class ObstacleConcepts
{
    public static string? For(string obstacleType) => obstacleType switch
    {
        nameof(ObstacleType.BlockedSidewalk) => "scooters, bikes, goods or vehicles blocking the sidewalk",
        nameof(ObstacleType.BrokenSurface) => "cracked, broken, raised or missing paving in the walking surface",
        nameof(ObstacleType.MissingOrBlockedRamp) => "the dropped kerb ramp at the curb",
        nameof(ObstacleType.StairsOrStep) => "stairs or a step on the footpath",
        nameof(ObstacleType.ConstructionBarrier) => "construction barriers, scaffolding or hoarding on the sidewalk",
        // Widened on 2026-11-03 after a Sylhet photo (footpath clear, open drain along its edge) classified
        // as Other and returned zero obstacle boxes: the old phrase asked only for things that *block*, so
        // the most common hazard in the pilot city was unmeasurable. Still returns null, never 0%.
        nameof(ObstacleType.Other) => "an obstacle, open drain, trench, excavation or pile of material blocking or narrowing the sidewalk",
        _ => null,
    };

    /// <summary>
    /// Whether an occupancy share is a meaningful number for this obstacle type at all.
    ///
    /// Measured on 2026-11-04 against a Sylhet photo whose walking line is clear and whose hazard is the
    /// excavated drain along its edge: the model classified it <c>BrokenSurface</c>, the surface concept
    /// segmented the whole damaged pavement, and the share came back <b>100% occupied</b>. The arithmetic
    /// was right; the metric was not. Broken paving does not occupy a walkway, it degrades it, so
    /// obstacle-area over walkway-area answers a question nobody asked - and the answer it gives ("the path
    /// is entirely blocked") is the most dangerous false claim this feature can make.
    ///
    /// Only obstacles that physically stand on the walking line are measured. Everything else gets no
    /// measurement, which the UI reports as unknown - never as 0% and never as a clear path.
    /// </summary>
    public static bool MeasuresOccupancy(string obstacleType) => obstacleType switch
    {
        nameof(ObstacleType.BlockedSidewalk) => true,
        nameof(ObstacleType.ConstructionBarrier) => true,
        nameof(ObstacleType.Other) => true,
        _ => false,
    };
}
