using PathLens.Domain.Reports;

namespace PathLens.Application.Comparisons;

public enum LocationMatchQuality
{
    Strong,
    Possible,
    Mismatch,
    InheritedFromReport,
}

public sealed record ComparisonInput(
    bool EarlierAnalysisCompleted,
    bool LaterAnalysisCompleted,
    string? EarlierObstacleType,
    string? LaterObstacleType,
    double EarlierConfidence,
    double LaterConfidence,
    LocationMatchQuality LocationQuality,
    double? LocationDistanceMeters,
    bool TimestampTrusted,
    string? TimestampNote,
    bool ReportHumanVerified);

public sealed record ComparisonDecision(
    EvidenceComparisonOutcome Outcome,
    string Explanation,
    IReadOnlyList<string> Limitations);

/// <summary>
/// Deterministic evidence comparison rules (Blueprint Section 18).
/// The default is Inconclusive: a confident outcome must be earned by
/// location, timestamp, and visibility checks. In particular,
/// NotObservedInNewImage never means an obstacle was removed.
/// Thresholds are documented constants validated against the demo dataset,
/// not hidden magic numbers.
/// </summary>
public static class ComparisonEngine
{
    /// <summary>Below this later-image confidence, the newer photo is too weak to support a conclusion.</summary>
    public const double MinimumLaterConfidence = 0.35;

    public static ComparisonDecision Evaluate(ComparisonInput input)
    {
        var limitations = new List<string>();
        if (input.TimestampNote is not null)
        {
            limitations.Add(input.TimestampNote);
        }

        if (input.LocationQuality == LocationMatchQuality.Possible)
        {
            limitations.Add(
                $"Location match is only possible (distance {(input.LocationDistanceMeters is null ? "unknown" : $"{input.LocationDistanceMeters:0} m")}); treat this comparison with caution.");
        }

        if (input.LocationQuality == LocationMatchQuality.InheritedFromReport)
        {
            limitations.Add("Evidence items have no separate GPS, so the report location was used for both. Same-place matching is only as good as the report location.");
        }

        if (!input.EarlierAnalysisCompleted || !input.LaterAnalysisCompleted)
        {
            return new ComparisonDecision(
                EvidenceComparisonOutcome.Inconclusive,
                "Comparison is inconclusive because one or both photos have no completed AI analysis yet. Run analysis on both photos first.",
                limitations);
        }

        if (input.LocationQuality == LocationMatchQuality.Mismatch)
        {
            return new ComparisonDecision(
                EvidenceComparisonOutcome.Inconclusive,
                $"Comparison is inconclusive because the two photos appear to be from different places (distance {(input.LocationDistanceMeters is null ? "unknown" : $"{input.LocationDistanceMeters:0} m")}). Photos from different places must not be compared confidently.",
                limitations);
        }

        var earlierObstacle = ParseObstacle(input.EarlierObstacleType);
        var laterObstacle = ParseObstacle(input.LaterObstacleType);

        if (earlierObstacle is null || earlierObstacle == ObstacleType.Unknown)
        {
            return new ComparisonDecision(
                EvidenceComparisonOutcome.Inconclusive,
                "Comparison is inconclusive because the earlier photo did not establish a specific obstacle. There is nothing definite to compare the newer photo against.",
                limitations);
        }

        EvidenceComparisonOutcome outcome;
        string explanation;

        if (laterObstacle is not null && laterObstacle == earlierObstacle)
        {
            if (input.LaterConfidence < MinimumLaterConfidence)
            {
                return new ComparisonDecision(
                    EvidenceComparisonOutcome.Inconclusive,
                    $"The newer photo also suggests {earlierObstacle}, but with low confidence ({input.LaterConfidence:0.00}), so the comparison stays inconclusive.",
                    limitations);
            }

            outcome = EvidenceComparisonOutcome.LikelyStillPresent;
            explanation =
                $"Both photos show {earlierObstacle}. The obstacle is likely still present. This is an AI-based comparison, not a verified fact; a human reviewer can override it.";
        }
        else if (laterObstacle is null || laterObstacle == ObstacleType.Unknown)
        {
            if (input.LaterConfidence < MinimumLaterConfidence)
            {
                return new ComparisonDecision(
                    EvidenceComparisonOutcome.Inconclusive,
                    "Comparison is inconclusive because the newer photo is too weak to judge: the previously reported obstacle area may not be clearly visible (angle, lighting, or occlusion can hide it).",
                    limitations);
            }

            outcome = EvidenceComparisonOutcome.NotObservedInNewImage;
            explanation =
                $"The previously reported {earlierObstacle} was not observed in the newer photo. This does not prove the obstacle was removed: it may be outside the frame or hidden. Treat the place as unconfirmed, not cleared.";
        }
        else
        {
            outcome = EvidenceComparisonOutcome.ConflictingEvidence;
            explanation =
                $"The earlier photo shows {earlierObstacle}, but the newer photo shows {laterObstacle}. The evidence conflicts or the situation changed in a different way, so a human reviewer should decide.";
        }

        if (!input.TimestampTrusted && outcome is EvidenceComparisonOutcome.LikelyStillPresent or EvidenceComparisonOutcome.NotObservedInNewImage)
        {
            limitations.Add("Because the time order is not trusted, a time-based conclusion (still present / not observed later) is not allowed.");
            return new ComparisonDecision(
                EvidenceComparisonOutcome.Inconclusive,
                "Comparison is inconclusive because the capture times cannot be trusted to order these photos. The photos may be comparable visually, but no time-based claim can be made.",
                limitations);
        }

        if (input.ReportHumanVerified)
        {
            explanation += " The report itself is human-verified, which strengthens the earlier evidence.";
        }

        return new ComparisonDecision(outcome, explanation, limitations);
    }

    private static ObstacleType? ParseObstacle(string? value) =>
        Enum.TryParse<ObstacleType>(value, ignoreCase: true, out var parsed) ? parsed : null;
}
