using PathLens.Domain.Reports;

namespace PathLens.Application.Reports;

public sealed record CreateReportRequest(
    string? Title,
    string? Description,
    string? ObstacleType,
    string? Severity,
    double Latitude,
    double Longitude,
    bool? HasStairs = null,
    bool? HasRamp = null,
    string? Surface = null);

public sealed record UpdateReportRequest(
    string? Title,
    string? Description,
    string? ObstacleType,
    string? Severity,
    bool? HasStairs = null,
    bool? HasRamp = null,
    string? Surface = null);

public sealed record ReportResponse(
    Guid Id,
    string Title,
    string? Description,
    string ObstacleType,
    string Severity,
    double Latitude,
    double Longitude,
    string Status,
    string VerificationStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastEvidenceAt,
    Guid? CameraSourceId,
    bool? HasStairs,
    bool? HasRamp,
    string? Surface,
    string? AttributeSource);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ReportQuery(
    string? Search,
    ObstacleType? ObstacleType,
    VerificationStatus? VerificationStatus,
    int Page = 1,
    int PageSize = 20,
    string Sort = "newest");
