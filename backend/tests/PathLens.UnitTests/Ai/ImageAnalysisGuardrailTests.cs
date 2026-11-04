using PathLens.Application.Ai;
using PathLens.Domain.Reports;

namespace PathLens.UnitTests.Ai;

public class ImageAnalysisGuardrailTests
{
    private static bool TryParseType(string type, double confidence, out ImageAnalysisResult? result)
    {
        var json = $"{{\"schemaVersion\":\"1.0\",\"obstacleType\":\"{type}\",\"visibleEvidence\":[\"x\"],\"limitations\":[\"y\"],\"confidence\":{confidence},\"humanReviewRequired\":true}}";
        return ImageAnalysisResultParser.TryParse(json, "Meta", "muse-spark-1.3", DateTimeOffset.UtcNow, out result, out _);
    }

    [Fact]
    public void Low_confidence_named_type_becomes_unknown_abstention()
    {
        Assert.True(TryParseType(nameof(ObstacleType.Other), 0.30, out var result));
        Assert.Equal(nameof(ObstacleType.Unknown), result!.ObstacleType);
    }

    [Fact]
    public void High_confidence_named_type_is_kept()
    {
        Assert.True(TryParseType(nameof(ObstacleType.BlockedSidewalk), 0.90, out var result));
        Assert.Equal(nameof(ObstacleType.BlockedSidewalk), result!.ObstacleType);
    }

    [Fact]
    public void Threshold_itself_is_kept_as_named()
    {
        Assert.True(TryParseType(nameof(ObstacleType.BlockedSidewalk), ImageAnalysisResultParser.AbstainBelowConfidence, out var result));
        Assert.Equal(nameof(ObstacleType.BlockedSidewalk), result!.ObstacleType);
    }
}
