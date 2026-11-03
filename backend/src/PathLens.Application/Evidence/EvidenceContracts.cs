namespace PathLens.Application.Evidence;

public sealed record EvidenceResponse(
    Guid Id,
    Guid ReportId,
    string FileName,
    string ContentType,
    long FileSize,
    string FileUrl,
    DateTimeOffset? CaptureTimeUtc,
    DateTimeOffset UploadedAt,
    double? Latitude,
    double? Longitude,
    double? LocationAccuracyMeters,
    string SourceType,
    string AnalysisStatus,
    string? AnalysisResultJson,
    string? AnalysisProvider,
    string? AnalysisModel,
    DateTimeOffset? AnalyzedAt,
    string? QualityFlag,
    string? QualityNote,
    string? PerceptualHash,
    double? ImageBrightness,
    double? ImageSharpness,
    string? ObstructionJson,
    DateTimeOffset? ObstructionMeasuredAt);

public sealed record AnalysisJobResponse(
    Guid Id,
    Guid EvidenceItemId,
    string Status,
    int Attempts,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
