using PathLens.Domain.Reports;

namespace PathLens.Application.Reports;

public sealed class CoverageService(IReportRepository repository)
{
    public const double DefaultCellSizeMeters = 50;
    public const double MinCellSizeMeters = 20;
    public const double MaxCellSizeMeters = 500;
    public const int MaxCells = 1000;
    public const int StaleDays = 30;

    public async Task<CoverageResponse> GetCoverageAsync(
        double minLatitude,
        double maxLatitude,
        double minLongitude,
        double maxLongitude,
        double? cellSizeMeters,
        string? obstacleType,
        string? verificationStatus,
        CancellationToken cancellationToken)
    {
        var cellSize = Math.Clamp(cellSizeMeters ?? DefaultCellSizeMeters, MinCellSizeMeters, MaxCellSizeMeters);

        ObstacleType? obstacle = null;
        if (!string.IsNullOrWhiteSpace(obstacleType) && Enum.TryParse<ObstacleType>(obstacleType, true, out var o))
            obstacle = o;
        VerificationStatus? verification = null;
        if (!string.IsNullOrWhiteSpace(verificationStatus) && Enum.TryParse<VerificationStatus>(verificationStatus, true, out var v))
            verification = v;

        var reports = await repository.InViewportAsync(
            minLatitude, maxLatitude, minLongitude, maxLongitude,
            obstacle, verification, 500, cancellationToken);

        var midLat = (minLatitude + maxLatitude) / 2.0;
        var latDelta = cellSize / 111_320.0;
        var cosLat = Math.Max(0.01, Math.Cos(midLat * Math.PI / 180.0));
        var lonDelta = cellSize / (111_320.0 * cosLat);

        var latSpan = maxLatitude - minLatitude;
        var lonSpan = maxLongitude - minLongitude;
        var rows = (int)Math.Ceiling(latSpan / latDelta);
        var cols = (int)Math.Ceiling(lonSpan / lonDelta);

        while (rows * cols > MaxCells && cellSize < MaxCellSizeMeters)
        {
            cellSize *= 2;
            latDelta = cellSize / 111_320.0;
            lonDelta = cellSize / (111_320.0 * cosLat);
            rows = (int)Math.Ceiling(latSpan / latDelta);
            cols = (int)Math.Ceiling(lonSpan / lonDelta);
        }
        rows = Math.Max(1, Math.Clamp(rows, 1, 100));
        cols = Math.Max(1, Math.Clamp(cols, 1, 100));

        var cells = new List<CoverageCell>(rows * cols);
        var now = DateTimeOffset.UtcNow;

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                var minLat = minLatitude + r * latDelta;
                var maxLat = Math.Min(minLat + latDelta, maxLatitude);
                var minLon = minLongitude + c * lonDelta;
                var maxLon = Math.Min(minLon + lonDelta, maxLongitude);
                var centerLat = (minLat + maxLat) / 2.0;
                var centerLon = (minLon + maxLon) / 2.0;
                var cellId = $"{r}-{c}";
                cells.Add(new CoverageCell(cellId, centerLat, centerLon, minLat, minLon, maxLat, maxLon, CoverageStatus.InsufficientInfo.ToString(), 0, 0, null, null, 0));
            }
        }

        if (reports.Count == 0)
        {
            var summaryEmpty = new CoverageSummary(cells.Count, 0, cells.Count, 0, 0);
            return new CoverageResponse(cells, summaryEmpty, cellSize, new MapViewport(minLatitude, maxLatitude, minLongitude, maxLongitude));
        }

        var buckets = new Dictionary<string, List<AccessibilityReport>>();
        foreach (var report in reports)
        {
            var r = Math.Clamp((int)Math.Floor((report.Latitude - minLatitude) / latDelta), 0, rows - 1);
            var col = Math.Clamp((int)Math.Floor((report.Longitude - minLongitude) / lonDelta), 0, cols - 1);
            var id = $"{r}-{col}";
            if (!buckets.TryGetValue(id, out var list)) { list = []; buckets[id] = list; }
            list.Add(report);
        }

        var filled = new List<CoverageCell>(cells.Count);
        foreach (var cell in cells)
        {
            if (!buckets.TryGetValue(cell.CellId, out var bucket)) { filled.Add(cell); continue; }
            var reportCount = bucket.Count;
            var verifiedCount = bucket.Count(x => x.VerificationStatus == VerificationStatus.HumanVerified);
            var newest = bucket.Max(x => x.LastEvidenceAt ?? x.CreatedAt);
            var oldest = bucket.Min(x => x.LastEvidenceAt ?? x.CreatedAt);
            var staleCount = bucket.Count(x => (now - (x.LastEvidenceAt ?? x.CreatedAt)).TotalDays > StaleDays);
            var status = CoverageStatus.ObstacleEvidence.ToString();
            filled.Add(cell with { Status = status, ReportCount = reportCount, VerifiedCount = verifiedCount, NewestEvidenceAt = newest, OldestEvidenceAt = oldest, StaleCount = staleCount });
        }

        var covered = filled.Count(x => x.Status != CoverageStatus.InsufficientInfo.ToString());
        var obstacleCells = filled.Count(x => x.Status == CoverageStatus.ObstacleEvidence.ToString());
        var evidenceCells = filled.Count(x => x.Status == CoverageStatus.EvidencePresent.ToString());
        var insufficient = filled.Count - covered;
        var summary = new CoverageSummary(filled.Count, covered, insufficient, obstacleCells, evidenceCells);
        return new CoverageResponse(filled, summary, cellSize, new MapViewport(minLatitude, maxLatitude, minLongitude, maxLongitude));
    }
}

public sealed record MapViewport(double MinLatitude, double MaxLatitude, double MinLongitude, double MaxLongitude);
