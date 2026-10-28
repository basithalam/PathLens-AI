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
            DateTimeOffset.UtcNow,
            cameraSourceId: null,
            request.HasStairs,
            request.HasRamp,
            ParseSurface(request.Surface),
            HasAnyAttribute(request.HasStairs, request.HasRamp, request.Surface) ? "Reporter" : null);

        await repository.AddAsync(report, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        // Duplicate detection (F4): same type (or Unknown) within 25 m is a
        // suggestion only — a reviewer decides, nothing merges by itself.
        var duplicate = await repository.FindDuplicateCandidateAsync(
            report.Id, report.Latitude, report.Longitude, report.ObstacleType,
            DuplicateRadiusMeters, cancellationToken);
        if (duplicate is not null)
        {
            report.SuggestDuplicate(duplicate.Id, DateTimeOffset.UtcNow);
            await repository.SaveChangesAsync(cancellationToken);
        }

        return ToResponse(report);
    }

    public const double DuplicateRadiusMeters = 25;

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

        if (request.HasStairs is not null || request.HasRamp is not null || request.Surface is not null)
        {
            report.SetAttributes(
                request.HasStairs ?? report.HasStairs,
                request.HasRamp ?? report.HasRamp,
                request.Surface is not null ? ParseSurface(request.Surface) : report.Surface,
                "Reporter",
                DateTimeOffset.UtcNow);
        }

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
        report.CameraSourceId,
        report.HasStairs,
        report.HasRamp,
        report.Surface?.ToString(),
        report.AttributeSource,
        report.PossibleDuplicateOfReportId,
        report.DuplicateOfReportId);

    public static SurfaceCondition? ParseSurface(string? value) =>
        Enum.TryParse<SurfaceCondition>(value, ignoreCase: true, out var parsed) ? parsed : null;

    private static bool HasAnyAttribute(bool? hasStairs, bool? hasRamp, string? surface) =>
        hasStairs is not null || hasRamp is not null || ParseSurface(surface) is not null;
}
