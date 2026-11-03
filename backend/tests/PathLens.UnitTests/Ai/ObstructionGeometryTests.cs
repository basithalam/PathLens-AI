using PathLens.Application.Ai;
using PathLens.Domain.Reports;

namespace PathLens.UnitTests.Ai;

/// <summary>
/// The obstruction share is deterministic geometry over boxes. These tests hold the two rules that
/// matter for a wheelchair user reading the screen: the share is measured against the walkway, and a
/// missing measurement is reported as unknown - never as zero.
/// </summary>
public class ObstructionGeometryTests
{
    private const string SamTextWithTwoBoxes =
        "<0f>0<|box;x1=457;y1=376;x2=761;y2=506;w=960;h=720|><|mask;x=0;y=0;data=131,305,abc|>" +
        "<|box;x1=131;y1=459;x2=499;y2=587;w=960;h=720|><|mask;x=0;y=0;data=129,369,def|>";

    [Fact]
    public void Parses_every_box_token_and_keeps_the_frame_size()
    {
        var boxes = ObstructionGeometry.ParseBoxes(SamTextWithTwoBoxes);

        Assert.Equal(2, boxes.Count);
        Assert.Equal(457, boxes[0].X1);
        Assert.Equal(304, boxes[0].Width);
        Assert.Equal(130, boxes[0].Height);
        Assert.Equal(960, boxes[0].FrameWidth);
        Assert.Equal(720, boxes[0].FrameHeight);
    }

    [Fact]
    public void Ignores_mask_tokens_and_malformed_boxes_instead_of_guessing()
    {
        var boxes = ObstructionGeometry.ParseBoxes(
            "<|box;x1=1;y1=2;w=10;h=10|><|box;x1=zero;y1=1;x2=2;y2=3|><|box;x1=0;y1=0;x2=4;y2=4;w=8;h=8|>");

        var box = Assert.Single(boxes);
        Assert.Equal(4, box.Width);
        Assert.Equal(4, box.Height);
        Assert.Equal(16, box.Area);
    }

    [Fact]
    public void Empty_or_null_text_yields_no_boxes()
    {
        Assert.Empty(ObstructionGeometry.ParseBoxes(""));
        Assert.Empty(ObstructionGeometry.ParseBoxes("no tokens here"));
    }

    [Fact]
    public void Occupancy_is_measured_against_the_walkway_not_the_whole_frame()
    {
        // Walkway band: 0,331 -> 960,720 = 960 x 389. Obstacle: 240 x 389 -> exactly half of it.
        var walkway = new[] { new PixelBox(0, 331, 960, 720, 960, 720) };
        var obstacle = new[] { new PixelBox(0, 331, 240, 720, 960, 720) };

        var percent = ObstructionGeometry.WalkwayOccupancyPercent(walkway, obstacle);

        Assert.Equal(25, percent);
    }

    [Fact]
    public void Zero_obstacle_boxes_is_unknown_never_zero_percent()
    {
        var walkway = new[] { new PixelBox(0, 331, 960, 720, 960, 720) };

        // This is the measured failure mode: the bare concept "scooter" returned no box at all on a
        // photo of three fallen scooters. "0% occupied" would tell the user the path is clear.
        Assert.Null(ObstructionGeometry.WalkwayOccupancyPercent(walkway, Array.Empty<PixelBox>()));
    }

    [Fact]
    public void Missing_walkway_box_is_unknown()
    {
        var obstacles = new[] { new PixelBox(10, 10, 50, 50, 960, 720) };

        Assert.Null(ObstructionGeometry.WalkwayOccupancyPercent(Array.Empty<PixelBox>(), obstacles));
    }

    [Fact]
    public void Obstacle_outside_the_walkway_does_not_count()
    {
        var walkway = new[] { new PixelBox(0, 331, 960, 720, 960, 720) };
        var obstacleOnTheRoad = new[] { new PixelBox(0, 0, 960, 300, 960, 720) };

        Assert.Null(ObstructionGeometry.WalkwayOccupancyPercent(walkway, obstacleOnTheRoad));
    }

    [Fact]
    public void Overlapping_obstacle_boxes_are_counted_once()
    {
        var walkway = new[] { new PixelBox(0, 0, 100, 100, 100, 100) };
        // The same 50x50 region reported twice must not read as 50% occupied.
        var obstacles = new[]
        {
            new PixelBox(0, 0, 50, 50, 100, 100),
            new PixelBox(0, 0, 50, 50, 100, 100),
        };

        Assert.Equal(25, ObstructionGeometry.WalkwayOccupancyPercent(walkway, obstacles));
    }

    [Fact]
    public void Union_of_disjoint_boxes_adds_up()
    {
        var area = ObstructionGeometry.UnionArea(new[]
        {
            new PixelBox(0, 0, 10, 10, 100, 100),
            new PixelBox(20, 0, 30, 10, 100, 100),
        });

        Assert.Equal(200, area);
    }

    [Fact]
    public void A_share_that_would_exceed_the_walkway_is_capped_at_one_hundred()
    {
        var walkway = new[] { new PixelBox(0, 0, 10, 10, 100, 100) };
        var obstacles = new[]
        {
            new PixelBox(0, 0, 10, 10, 100, 100),
            new PixelBox(0, 0, 10, 10, 100, 100),
            new PixelBox(0, 0, 10, 10, 100, 100),
        };

        Assert.Equal(100, ObstructionGeometry.WalkwayOccupancyPercent(walkway, obstacles));
    }

    [Fact]
    public void The_largest_walkway_box_is_the_one_used()
    {
        // SAM returned two sidewalk boxes on the live probe: 959x388 and 788x41. The band the person
        // walks along is the larger one, so the small strip must not become the denominator.
        var walkway = new[]
        {
            new PixelBox(171, 259, 959, 300, 960, 720),
            new PixelBox(0, 331, 959, 719, 960, 720),
        };
        var obstacle = new[] { new PixelBox(0, 331, 240, 719, 960, 720) };

        var percent = ObstructionGeometry.WalkwayOccupancyPercent(walkway, obstacle);

        Assert.True(percent is > 20 and < 30);
    }

    [Fact]
    public void Each_obstacle_type_has_a_descriptive_concept_phrase()
    {
        // A bare noun was measured to return zero boxes on an image full of that thing, so every
        // concept must be a phrase, not a single word.
        Assert.Equal(
            "scooters, bikes, goods or vehicles blocking the sidewalk",
            ObstacleConcepts.For(nameof(ObstacleType.BlockedSidewalk)));

        foreach (var type in new[]
        {
            nameof(ObstacleType.BlockedSidewalk), nameof(ObstacleType.BrokenSurface),
            nameof(ObstacleType.MissingOrBlockedRamp), nameof(ObstacleType.StairsOrStep),
            nameof(ObstacleType.ConstructionBarrier), nameof(ObstacleType.Other),
        })
        {
            var concept = ObstacleConcepts.For(type);
            Assert.NotNull(concept);
            Assert.True(concept!.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 3,
                $"{type} concept \"{concept}\" is too terse to segment reliably");
        }
    }

    [Fact]
    public void Unknown_obstacle_type_has_no_concept_so_nothing_is_measured()
    {
        Assert.Null(ObstacleConcepts.For(nameof(ObstacleType.Unknown)));
        Assert.Null(ObstacleConcepts.For("NotARealType"));
    }
}
