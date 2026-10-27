using PathLens.Application.Comparisons;
using PathLens.Domain.Reports;

namespace PathLens.UnitTests;

public class ComparisonEngineTests
{
    private static ComparisonInput Base(
        string earlier = "BlockedSidewalk",
        string later = "BlockedSidewalk",
        double laterConfidence = 0.8,
        LocationMatchQuality location = LocationMatchQuality.Strong,
        bool timestampTrusted = true,
        bool earlierCompleted = true,
        bool laterCompleted = true) => new(
        earlierCompleted,
        laterCompleted,
        earlier,
        later,
        0.8,
        laterConfidence,
        location,
        5,
        timestampTrusted,
        timestampTrusted ? null : "Capture time is missing for one photo.",
        false);

    [Fact]
    public void Same_obstacle_in_both_photos_is_likely_still_present()
    {
        var decision = ComparisonEngine.Evaluate(Base());
        Assert.Equal(EvidenceComparisonOutcome.LikelyStillPresent, decision.Outcome);
    }

    [Fact]
    public void Obstacle_missing_in_new_photo_is_not_observed_never_removed()
    {
        var decision = ComparisonEngine.Evaluate(Base(later: "Unknown"));
        Assert.Equal(EvidenceComparisonOutcome.NotObservedInNewImage, decision.Outcome);
        Assert.Contains("does not prove", decision.Explanation);
    }

    [Fact]
    public void Different_obstacle_is_conflicting_evidence()
    {
        var decision = ComparisonEngine.Evaluate(Base(later: "BrokenSurface"));
        Assert.Equal(EvidenceComparisonOutcome.ConflictingEvidence, decision.Outcome);
    }

    [Fact]
    public void Location_mismatch_is_inconclusive()
    {
        var decision = ComparisonEngine.Evaluate(Base(location: LocationMatchQuality.Mismatch));
        Assert.Equal(EvidenceComparisonOutcome.Inconclusive, decision.Outcome);
        Assert.Contains("different places", decision.Explanation);
    }

    [Fact]
    public void Untrusted_timestamp_downgrades_time_based_outcome()
    {
        var decision = ComparisonEngine.Evaluate(Base(timestampTrusted: false));
        Assert.Equal(EvidenceComparisonOutcome.Inconclusive, decision.Outcome);
    }

    [Fact]
    public void Incomplete_analysis_is_inconclusive()
    {
        var decision = ComparisonEngine.Evaluate(Base(laterCompleted: false));
        Assert.Equal(EvidenceComparisonOutcome.Inconclusive, decision.Outcome);
    }

    [Fact]
    public void Weak_new_photo_is_inconclusive_not_cleared()
    {
        var decision = ComparisonEngine.Evaluate(Base(later: "Unknown", laterConfidence: 0.1));
        Assert.Equal(EvidenceComparisonOutcome.Inconclusive, decision.Outcome);
    }

    [Fact]
    public void Earlier_unknown_obstacle_is_inconclusive()
    {
        var decision = ComparisonEngine.Evaluate(Base(earlier: "Unknown"));
        Assert.Equal(EvidenceComparisonOutcome.Inconclusive, decision.Outcome);
    }
}
