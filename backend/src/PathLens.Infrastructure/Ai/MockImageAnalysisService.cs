using PathLens.Application.Ai;

namespace PathLens.Infrastructure.Ai;

/// <summary>
/// Deterministic provider for automated tests and explicitly labelled demo
/// mode only. It performs no real image understanding: it reports the
/// obstacle as Unknown with zero confidence. Never present its output as
/// real AI analysis.
/// </summary>
public sealed class MockImageAnalysisService : IImageAnalysisService
{
    public string ProviderName => "Mock";

    public Task<ImageAnalysisResult> AnalyzeAsync(
        ImageAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var result = new ImageAnalysisResult(
            ImageAnalysisResultParser.CurrentSchemaVersion,
            "Unknown",
            [$"Mock analysis received an image ({request.ImageBytes.Length} bytes, {request.ContentType}). No real image understanding was performed."],
            ["Mock provider: this result is for tests or a labelled demo only and is not real AI inference."],
            0,
            true,
            ProviderName,
            "mock-deterministic-v1",
            DateTimeOffset.UtcNow);
        return Task.FromResult(result);
    }
}
