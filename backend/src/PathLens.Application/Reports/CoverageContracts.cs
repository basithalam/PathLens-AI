namespace PathLens.Application.Reports;

public enum CoverageStatus
{
    InsufficientInfo,
    ObstacleEvidence,
    EvidencePresent,
    Mixed
}

public sealed record CoverageCell(
    string CellId,
    double CenterLatitude,
    double CenterLongitude,
    double MinLatitude,
    double MinLongitude,
    double MaxLatitude,
    double MaxLongitude,
    string Status,
    int ReportCount,
    int VerifiedCount,
    DateTimeOffset? NewestEvidenceAt,
    DateTimeOffset? OldestEvidenceAt,
    int StaleCount);

public sealed record CoverageSummary(
    int TotalCells,
    int CoveredCells,
    int InsufficientCells,
    int ObstacleCells,
    int EvidenceCells);

public sealed record CoverageResponse(
    IReadOnlyList<CoverageCell> Cells,
    CoverageSummary Summary,
    double CellSizeMeters,
    MapViewport Viewport);
