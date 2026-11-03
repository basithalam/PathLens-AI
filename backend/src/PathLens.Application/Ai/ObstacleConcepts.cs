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
}
