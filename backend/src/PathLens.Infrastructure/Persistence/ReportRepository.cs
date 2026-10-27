using Microsoft.EntityFrameworkCore;
using PathLens.Application.Reports;
using PathLens.Domain.Reports;

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

        IQueryable<AccessibilityReport> reports = dbContext.Reports.AsNoTracking();

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
        // Bounding-box prefilter in SQL, exact Haversine distance in memory.
        // Replaced by a PostGIS spatial query in Phase 4.
        var safeRadius = Math.Clamp(radiusMeters, 1, 50_000);
        var latDelta = safeRadius / 111_320d;
        var lonDelta = safeRadius / (111_320d * Math.Max(0.01, Math.Cos(latitude * Math.PI / 180)));

        var candidates = await dbContext.Reports.AsNoTracking()
            .Where(report =>
                report.Latitude >= latitude - latDelta && report.Latitude <= latitude + latDelta &&
                report.Longitude >= longitude - lonDelta && report.Longitude <= longitude + lonDelta)
            .ToListAsync(cancellationToken);

        return candidates
            .Select(report => (Report: report, Distance: report.DistanceMetersTo(latitude, longitude)))
            .Where(candidate => candidate.Distance <= safeRadius)
            .OrderBy(candidate => candidate.Distance)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(candidate => candidate.Report)
            .ToList();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
