namespace PathLens.Application.Reports;

public sealed record MapSummaryResponse(
    int TotalReports,
    IReadOnlyDictionary<string, int> ByObstacleType,
    IReadOnlyDictionary<string, int> ByVerificationStatus,
    DateTimeOffset? LatestReportAt);
