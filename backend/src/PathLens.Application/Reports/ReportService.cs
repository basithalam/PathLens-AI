using PathLens.Domain.Reports;

namespace PathLens.Application.Reports;

public sealed class ReportService(IReportRepository repository)
{
    public async Task<ReportResponse> CreateAsync(CreateReportRequest request, CancellationToken cancellationToken)
    {
        var report = AccessibilityReport.Create(
            request.Title!,
            request.Description,
            ReportRequestValidator.ParseObstacleType(request.ObstacleType),
            ReportRequestValidator.ParseSeverity(request.Severity),
            request.Latitude,
            request.Longitude,
            createdByUserId: null,
            DateTimeOffset.UtcNow);

        await repository.AddAsync(report, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(report);
    }

    public async Task<ReportResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var report = await repository.GetByIdAsync(id, cancellationToken);
        return report is null ? null : ToResponse(report);
    }

    public async Task<PagedResult<ReportResponse>> ListAsync(ReportQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.ListAsync(query, cancellationToken);
        return new PagedResult<ReportResponse>(
            result.Items.Select(ToResponse).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }

    public async Task<ReportResponse?> UpdateAsync(
        Guid id, UpdateReportRequest request, CancellationToken cancellationToken)
    {
        var report = await repository.GetByIdAsync(id, cancellationToken);
        if (report is null)
        {
            return null;
        }

        report.Update(
            request.Title!,
            request.Description,
            ReportRequestValidator.ParseObstacleType(request.ObstacleType),
            ReportRequestValidator.ParseSeverity(request.Severity),
            DateTimeOffset.UtcNow);

        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(report);
    }

    public async Task<IReadOnlyList<ReportResponse>> InViewportAsync(
        double minLatitude, double maxLatitude, double minLongitude, double maxLongitude,
        ObstacleType? obstacleType, VerificationStatus? verificationStatus,
        int limit, CancellationToken cancellationToken)
    {
        var reports = await repository.InViewportAsync(
            minLatitude, maxLatitude, minLongitude, maxLongitude,
            obstacleType, verificationStatus, limit, cancellationToken);
        return reports.Select(ToResponse).ToList();
    }

    public Task<MapSummaryResponse> SummaryAsync(CancellationToken cancellationToken) =>
        repository.SummaryAsync(cancellationToken);

    public async Task<IReadOnlyList<ReportResponse>> NearbyAsync(
        double latitude, double longitude, double radiusMeters, int limit, CancellationToken cancellationToken)
    {
        var reports = await repository.NearbyAsync(latitude, longitude, radiusMeters, limit, cancellationToken);
        return reports.Select(ToResponse).ToList();
    }

    public static ReportResponse ToResponse(AccessibilityReport report) => new(
        report.Id,
        report.Title,
        report.Description,
        report.ObstacleType.ToString(),
        report.Severity.ToString(),
        report.Latitude,
        report.Longitude,
        report.Status.ToString(),
        report.VerificationStatus.ToString(),
        report.CreatedAt,
        report.UpdatedAt,
        report.LastEvidenceAt,
        report.CameraSourceId);
}
