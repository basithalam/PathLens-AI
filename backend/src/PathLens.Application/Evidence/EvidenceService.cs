using PathLens.Application.Ai;
using PathLens.Application.Storage;
using PathLens.Domain.Evidence;

namespace PathLens.Application.Evidence;

public sealed class EvidenceService(IEvidenceRepository repository, IObjectStorage storage)
{
    public async Task<EvidenceResponse?> AddEvidenceAsync(
        Guid reportId,
        byte[] content,
        string fileName,
        string contentType,
        DateTimeOffset? captureTimeUtc,
        double? latitude,
        double? longitude,
        double? locationAccuracyMeters,
        CancellationToken cancellationToken)
    {
        if (!await repository.ReportExistsAsync(reportId, cancellationToken))
        {
            return null;
        }

        string storageKey;
        await using (var stream = new MemoryStream(content))
        {
            storageKey = await storage.SaveAsync(stream, contentType, cancellationToken);
        }

        try
        {
            var item = EvidenceItem.Create(
                reportId,
                storageKey,
                Path.GetFileName(fileName),
                contentType,
                content.LongLength,
                captureTimeUtc,
                latitude,
                longitude,
                locationAccuracyMeters,
                DateTimeOffset.UtcNow);

            await repository.AddEvidenceAsync(item, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return ToResponse(item);
        }
        catch
        {
            // Do not leave an orphaned object when the database write fails.
            await storage.DeleteAsync(storageKey, CancellationToken.None);
            throw;
        }
    }

    public async Task<EvidenceResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await repository.GetEvidenceAsync(id, cancellationToken);
        return item is null ? null : ToResponse(item);
    }

    public async Task<IReadOnlyList<EvidenceResponse>?> ListForReportAsync(
        Guid reportId, CancellationToken cancellationToken)
    {
        if (!await repository.ReportExistsAsync(reportId, cancellationToken))
        {
            return null;
        }

        var items = await repository.ListForReportAsync(reportId, cancellationToken);
        return items.Select(ToResponse).ToList();
    }

    public async Task<(EvidenceItem Item, Stream Stream)?> OpenFileAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var item = await repository.GetEvidenceAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var stream = await storage.OpenReadAsync(item.StorageKey, cancellationToken);
        return stream is null ? null : (item, stream);
    }

    public async Task<AnalysisJobResponse?> StartAnalysisAsync(Guid evidenceId, CancellationToken cancellationToken)
    {
        var item = await repository.GetEvidenceAsync(evidenceId, cancellationToken);
        if (item is null)
        {
            return null;
        }

        item.ResetForAnalysis();
        var job = AnalysisJob.Create(evidenceId, DateTimeOffset.UtcNow);
        await repository.AddJobAsync(job, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(job);
    }

    public async Task<AnalysisJobResponse?> GetJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await repository.GetJobAsync(jobId, cancellationToken);
        return job is null ? null : ToResponse(job);
    }

    public static EvidenceResponse ToResponse(EvidenceItem item) => new(
        item.Id,
        item.ReportId,
        item.FileName,
        item.ContentType,
        item.FileSize,
        $"/api/evidence/{item.Id}/file",
        item.CaptureTimeUtc,
        item.UploadedAt,
        item.Latitude,
        item.Longitude,
        item.LocationAccuracyMeters,
        item.SourceType.ToString(),
        item.AnalysisStatus.ToString(),
        item.AnalysisResultJson,
        item.AnalysisProvider,
        item.AnalysisModel,
        item.AnalyzedAt,
        item.QualityFlag.ToString(),
        item.QualityNote,
        item.PerceptualHash,
        item.ImageBrightness,
        item.ImageSharpness);

    public static AnalysisJobResponse ToResponse(AnalysisJob job) => new(
        job.Id,
        job.EvidenceItemId,
        job.Status.ToString(),
        job.Attempts,
        job.ErrorMessage,
        job.CreatedAt,
        job.CompletedAt);
}
