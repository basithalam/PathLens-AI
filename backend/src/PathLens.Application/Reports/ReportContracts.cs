using PathLens.Domain.Reports;

namespace PathLens.Application.Reports;

public sealed record CreateReportRequest(
    string? Title,
    string? Description,
    string? ObstacleType,
    string? Severity,
    double Latitude,
    double Longitude);

public sealed record UpdateReportRequest(
    string? Title,
    string? Description,
    string? ObstacleType,
    string? Severity);

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
    DateTimeOffset? LastEvidenceAt);

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
