using PathLens.Application.Ai;
using PathLens.Application.Evidence;

namespace PathLens.UnitTests;

public class EvidenceValidationTests
{
    private static readonly byte[] TinyPng =
        Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8Xw8AAwMCAO+ip1sAAAAASUVORK5CYII=");

    [Fact]
    public void Valid_png_passes_validation()
    {
        var errors = ImageFileValidator.Validate(TinyPng, "image/png", TinyPng.Length);
        Assert.Empty(errors);
    }

    [Fact]
    public void Spoofed_content_type_is_rejected()
    {
        var textBytes = "this is not an image"u8.ToArray();
        var errors = ImageFileValidator.Validate(textBytes, "image/png", textBytes.Length);
        Assert.Contains("file", errors.Keys);
    }

    [Fact]
    public void Empty_file_is_rejected()
    {
        var errors = ImageFileValidator.Validate([], "image/png", 0);
        Assert.Contains("file", errors.Keys);
    }

    [Fact]
    public void Parser_accepts_valid_schema_and_rejects_bad_confidence()
    {
        const string valid = """{"schemaVersion":"1.0","obstacleType":"BlockedSidewalk","visibleEvidence":["A barrier blocks the path."],"limitations":["Full width not visible."],"confidence":0.7,"humanReviewRequired":true}""";
        Assert.True(ImageAnalysisResultParser.TryParse(valid, "Meta", "model-x", DateTimeOffset.UtcNow, out var result, out _));
        Assert.Equal("BlockedSidewalk", result!.ObstacleType);
        Assert.Equal(0.7, result.Confidence);

        const string invalid = """{"schemaVersion":"1.0","obstacleType":"BlockedSidewalk","visibleEvidence":[],"limitations":[],"confidence":1.5,"humanReviewRequired":true}""";
        Assert.False(ImageAnalysisResultParser.TryParse(invalid, "Meta", "model-x", DateTimeOffset.UtcNow, out _, out var error));
        Assert.Contains("Confidence", error);
    }
}
