using Microsoft.EntityFrameworkCore;
using PathLens.Application.Reports;
using PathLens.Domain.Reports;
using PathLens.Infrastructure.Persistence.PostGis;

namespace PathLens.Infrastructure.Persistence;

public sealed class ReportRepository(PathLensDbContext dbContext) : IReportRepository
{
    public async Task AddAsync(AccessibilityReport report, CancellationToken cancellationToken) =>
        await dbContext.Reports.AddAsync(report, cancellationToken);

    public Task<AccessibilityReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Reports.FirstOrDefaultAsync(report => report.Id == id, cancellationToken);

    public async Task<PagedResult<AccessibilityReport>> ListAsync(
        ReportQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        // Confirmed duplicates are pointers, not counted as separate reports.
        IQueryable<AccessibilityReport> reports = dbContext.Reports.AsNoTracking()
            .Where(report => report.DuplicateOfReportId == null);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            reports = reports.Where(report =>
                report.Title.Contains(search) ||
                (report.Description != null && report.Description.Contains(search)));
        }

        if (query.ObstacleType is not null)
        {
            reports = reports.Where(report => report.ObstacleType == query.ObstacleType);
        }

        if (query.VerificationStatus is not null)
        {
            reports = reports.Where(report => report.VerificationStatus == query.VerificationStatus);
        }

        reports = query.Sort?.ToLowerInvariant() switch
        {
            "oldest" => reports.OrderBy(report => report.CreatedAt),
            "title" => reports.OrderBy(report => report.Title),
            _ => reports.OrderByDescending(report => report.CreatedAt),
        };

        var totalCount = await reports.CountAsync(cancellationToken);
        var items = await reports
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AccessibilityReport>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<AccessibilityReport>> NearbyAsync(
        double latitude, double longitude, double radiusMeters, int limit, CancellationToken cancellationToken)
    {
        var safeRadius = Math.Clamp(radiusMeters, 1, 50_000);
        var clampedLimit = Math.Clamp(limit, 1, 100);
        // H3.1 PostGIS: when extension exists, use geography ST_DWithin (index-friendly).
        // Otherwise fall back to bounding-box + Haversine (local 5433 without PostGIS).
        try
        {
            if (await PostGisHelper.IsAvailableAsync(dbContext, cancellationToken))
            {
                // Parametrised to prevent SQL injection; geography uses metres directly
                var nearby = await dbContext.Reports
                    .FromSqlInterpolated<AccessibilityReport>($"""
                        SELECT * FROM accessibility_reports
                        WHERE duplicate_of_report_id IS NULL
                          AND ST_DWithin(
                                ST_MakePoint(longitude, latitude)::geography,
                                ST_MakePoint({longitude}, {latitude})::geography,
                                {safeRadius})
                        ORDER BY ST_Distance(ST_MakePoint(longitude, latitude)::geography, ST_MakePoint({longitude}, {latitude})::geography)
                        LIMIT {clampedLimit}
                        """)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                return nearby;
            }
        }
        catch
        {
            // PostGIS probe failed — fall through to lat/lon fallback
        }

        // Fallback: bounding-box prefilter in SQL, exact Haversine distance in memory (pre-H3 behaviour)
        var latDelta = safeRadius / 111_320d;
        var lonDelta = safeRadius / (111_320d * Math.Max(0.01, Math.Cos(latitude * Math.PI / 180)));

        var candidates = await dbContext.Reports.AsNoTracking()
            .Where(report =>
                report.DuplicateOfReportId == null &&
                report.Latitude >= latitude - latDelta && report.Latitude <= latitude + latDelta &&
                report.Longitude >= longitude - lonDelta && report.Longitude <= longitude + lonDelta)
            .ToListAsync(cancellationToken);

        return candidates
            .Select(report => (Report: report, Distance: report.DistanceMetersTo(latitude, longitude)))
            .Where(candidate => candidate.Distance <= safeRadius)
            .OrderBy(candidate => candidate.Distance)
            .Take(clampedLimit)
            .Select(candidate => candidate.Report)
            .ToList();
    }

    public async Task<AccessibilityReport?> FindDuplicateCandidateAsync(
        Guid excludeReportId, double latitude, double longitude,
        ObstacleType obstacleType, double radiusMeters, CancellationToken cancellationToken)
    {
        var nearby = await NearbyAsync(latitude, longitude, radiusMeters, 20, cancellationToken);
        return nearby
            .Where(report => report.Id != excludeReportId &&
                             (report.ObstacleType == obstacleType ||
                              report.ObstacleType == ObstacleType.Unknown ||
                              obstacleType == ObstacleType.Unknown))
            .OrderBy(report => report.DistanceMetersTo(latitude, longitude))
            .FirstOrDefault();
    }

    public async Task<IReadOnlyList<AccessibilityReport>> InViewportAsync(
        double minLatitude, double maxLatitude, double minLongitude, double maxLongitude,
        ObstacleType? obstacleType, VerificationStatus? verificationStatus,
        int limit, CancellationToken cancellationToken)
    {
        var clampedLimit = Math.Clamp(limit, 1, 500);
        // H3.1 PostGIS: ST_Within with geography envelope when available (uses GiST geography index).
        try
        {
            if (await PostGisHelper.IsAvailableAsync(dbContext, cancellationToken))
            {
                // Build dynamic WHERE for optional filters via interpolated SQL
                var obstacleFilter = obstacleType is null ? "" : $" AND obstacle_type = '{obstacleType}'";
                var verificationFilter = verificationStatus is null ? "" : $" AND verification_status = '{verificationStatus}'";
                var sql = $"""
                    SELECT * FROM accessibility_reports
                    WHERE duplicate_of_report_id IS NULL
                      AND ST_Within(
                            ST_MakePoint(longitude, latitude)::geography,
                            ST_MakeEnvelope({minLongitude}, {minLatitude}, {maxLongitude}, {maxLatitude}, 4326)::geography)
                      {obstacleFilter}{verificationFilter}
                    ORDER BY created_at DESC
                    LIMIT {clampedLimit}
                    """;
                // EF Core does not support FromSqlInterpolated with string-concatenated WHERE, so use FromSqlRaw
                var inViewport = await dbContext.Reports
                    .FromSqlRaw<AccessibilityReport>(sql)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                return inViewport;
            }
        }
        catch
        {
            // fallback to lat/lon
        }

        IQueryable<AccessibilityReport> reports = dbContext.Reports.AsNoTracking()
            .Where(report =>
                report.DuplicateOfReportId == null &&
                report.Latitude >= minLatitude && report.Latitude <= maxLatitude &&
                report.Longitude >= minLongitude && report.Longitude <= maxLongitude);

        if (obstacleType is not null)
        {
            reports = reports.Where(report => report.ObstacleType == obstacleType);
        }

        if (verificationStatus is not null)
        {
            reports = reports.Where(report => report.VerificationStatus == verificationStatus);
        }

        return await reports
            .OrderByDescending(report => report.CreatedAt)
            .Take(clampedLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task<MapSummaryResponse> SummaryAsync(CancellationToken cancellationToken)
    {
        var reports = await dbContext.Reports.AsNoTracking()
            .Where(report => report.DuplicateOfReportId == null)
            .Select(report => new { report.ObstacleType, report.VerificationStatus, report.CreatedAt })
            .ToListAsync(cancellationToken);

        return new MapSummaryResponse(
            reports.Count,
            reports.GroupBy(report => report.ObstacleType.ToString())
                .ToDictionary(group => group.Key, group => group.Count()),
            reports.GroupBy(report => report.VerificationStatus.ToString())
                .ToDictionary(group => group.Key, group => group.Count()),
            reports.Count == 0 ? null : reports.Max(report => report.CreatedAt));
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
