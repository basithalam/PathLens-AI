using PathLens.Domain.Reports;

namespace PathLens.Domain.Evidence;

public enum EvidenceSourceType
{
    Upload,
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
    public AnalysisStatus AnalysisStatus { get; private set; }
    public string? AnalysisResultJson { get; private set; }
    public string? AnalysisProvider { get; private set; }
    public string? AnalysisModel { get; private set; }
    public DateTimeOffset? AnalyzedAt { get; private set; }

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
        DateTimeOffset now)
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
            SourceType = EvidenceSourceType.Upload,
            AnalysisStatus = AnalysisStatus.Pending,
        };
    }

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

    public void ResetForAnalysis()
    {
        AnalysisStatus = AnalysisStatus.Pending;
        AnalysisResultJson = null;
        AnalyzedAt = null;
    }
}
