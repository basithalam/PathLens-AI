namespace PathLens.Application.Ai;

public sealed record ImageAnalysisRequest(byte[] ImageBytes, string ContentType);

/// <summary>
/// Versioned AI output contract (schema 1.0). Provider output is untrusted
/// data and must pass validation before it is stored or shown.
/// </summary>
public sealed record ImageAnalysisResult(
    string SchemaVersion,
    string ObstacleType,
    IReadOnlyList<string> VisibleEvidence,
    IReadOnlyList<string> Limitations,
    double Confidence,
    bool HumanReviewRequired,
    string Provider,
    string? Model,
    DateTimeOffset AnalyzedAt);

public interface IImageAnalysisService
{
    string ProviderName { get; }

    Task<ImageAnalysisResult> AnalyzeAsync(
        ImageAnalysisRequest request,
        CancellationToken cancellationToken);
}
