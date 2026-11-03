using PathLens.Domain.Reports;

namespace PathLens.Domain.Evidence;

public enum EvidenceSourceType
{
    Upload,
    CameraFeed,
}

/// <summary>
/// Local quality assessment of a camera frame, made before any AI call.
/// Unknown means the frame could not be decoded for checks (it is still
/// sent to analysis as usual); it never means the frame was fine.
/// </summary>
public enum FrameQuality
{
    Unknown,
    Suitable,
    TooDark,
    TooBlurred,
    ViewChanged,
}

/// <summary>
/// One piece of evidence (a photo) attached to a report. Capture time and
/// GPS are user-supplied claims unless independently verified; they are
/// stored separately from the upload time for exactly that reason.
/// </summary>
public sealed class EvidenceItem
{
    private EvidenceItem()
    {
    }

    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public DateTimeOffset? CaptureTimeUtc { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public double? LocationAccuracyMeters { get; private set; }
    public EvidenceSourceType SourceType { get; private set; }

    /// <summary>Set only for camera evidence: which source produced this snapshot.</summary>
    public Guid? CameraSourceId { get; private set; }
    // Camera frame quality gate (Phase F3). Null metrics mean not measured.
    public string? PerceptualHash { get; private set; }
    public double? ImageBrightness { get; private set; }
    public double? ImageSharpness { get; private set; }
    public FrameQuality QualityFlag { get; private set; } = FrameQuality.Unknown;
    public string? QualityNote { get; private set; }

    public AnalysisStatus AnalysisStatus { get; private set; }
    public string? AnalysisResultJson { get; private set; }
    public string? AnalysisProvider { get; private set; }
    public string? AnalysisModel { get; private set; }
    public DateTimeOffset? AnalyzedAt { get; private set; }

    /// <summary>
    /// Deterministic obstruction measurement over SAM 3.1 boxes, stored apart from the AI analysis
    /// result so the two provenances never blur: one is what a model claimed, the other is what we
    /// computed from boxes. Null means not measured - which is unknown, not "clear".
    /// </summary>
    public string? ObstructionJson { get; private set; }
    public DateTimeOffset? ObstructionMeasuredAt { get; private set; }

    public static EvidenceItem Create(
        Guid reportId,
        string storageKey,
        string fileName,
        string contentType,
        long fileSize,
        DateTimeOffset? captureTimeUtc,
        double? latitude,
        double? longitude,
        double? locationAccuracyMeters,
        DateTimeOffset now,
        EvidenceSourceType sourceType = EvidenceSourceType.Upload,
        Guid? cameraSourceId = null)
    {
        if (latitude is not null && !AccessibilityReport.IsValidLatitude(latitude.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (longitude is not null && !AccessibilityReport.IsValidLongitude(longitude.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }

        return new EvidenceItem
        {
            Id = Guid.NewGuid(),
            ReportId = reportId,
            StorageKey = storageKey,
            FileName = fileName,
            ContentType = contentType,
            FileSize = fileSize,
            CaptureTimeUtc = captureTimeUtc,
            UploadedAt = now,
            Latitude = latitude,
            Longitude = longitude,
            LocationAccuracyMeters = locationAccuracyMeters,
            SourceType = sourceType,
            CameraSourceId = sourceType == EvidenceSourceType.CameraFeed ? cameraSourceId : null,
            AnalysisStatus = AnalysisStatus.Pending,
        };
    }

    /// <summary>Records the pre-AI quality decision so it is auditable, not magic.</summary>
    public void RecordFrameQuality(
        string? perceptualHash, double? brightness, double? sharpness,
        FrameQuality flag, string? note)
    {
        PerceptualHash = perceptualHash;
        ImageBrightness = brightness;
        ImageSharpness = sharpness;
        QualityFlag = flag;
        QualityNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public bool IsUnsuitableForAnalysis =>
        QualityFlag is FrameQuality.TooDark or FrameQuality.TooBlurred;

    public void MarkAnalysisProcessing() => AnalysisStatus = AnalysisStatus.Processing;

    public void MarkAnalysisCompleted(string resultJson, string provider, string? model, DateTimeOffset now)
    {
        AnalysisStatus = AnalysisStatus.Completed;
        AnalysisResultJson = resultJson;
        AnalysisProvider = provider;
        AnalysisModel = model;
        AnalyzedAt = now;
    }

    public void MarkAnalysisFailed()
    {
        AnalysisStatus = AnalysisStatus.Failed;
        AnalyzedAt = null;
    }

    /// <summary>
    /// Stores the deterministic obstruction measurement beside the evidence. A null percent is recorded
    /// as no measurement at all, so the absence stays visible instead of becoming a number.
    /// </summary>
    public void RecordObstruction(string resultJson, DateTimeOffset now)
    {
        ObstructionJson = resultJson;
        ObstructionMeasuredAt = now;
    }

    public void ResetForAnalysis()
    {
        AnalysisStatus = AnalysisStatus.Pending;
        AnalysisResultJson = null;
        AnalyzedAt = null;
        ObstructionJson = null;
        ObstructionMeasuredAt = null;
    }
}
