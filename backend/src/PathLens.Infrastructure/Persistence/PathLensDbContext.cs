using Microsoft.EntityFrameworkCore;
using PathLens.Domain.Evidence;
using PathLens.Domain.Reports;
using PathLens.Domain.Routing;
using PathLens.Domain.Cameras;
using PathLens.Domain.Users;

namespace PathLens.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL DbContext. Location columns are latitude/longitude for now:
/// the local PostgreSQL install has no PostGIS, so geometry columns and
/// spatial indexes arrive with the Phase 4 PostGIS work (docker-compose
/// already uses a PostGIS image). Do not claim PostGIS behaviour before then.
/// </summary>
public sealed class PathLensDbContext(DbContextOptions<PathLensDbContext> options) : DbContext(options)
{
    public DbSet<AccessibilityReport> Reports => Set<AccessibilityReport>();
    public DbSet<EvidenceItem> EvidenceItems => Set<EvidenceItem>();
    public DbSet<AnalysisJob> AnalysisJobs => Set<AnalysisJob>();
    public DbSet<EvidenceComparison> EvidenceComparisons => Set<EvidenceComparison>();
    public DbSet<RouteRecommendationRecord> RouteRecommendations => Set<RouteRecommendationRecord>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<ReviewAuditEvent> ReviewAuditEvents => Set<ReviewAuditEvent>();
    public DbSet<CameraSource> CameraSources => Set<CameraSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PathLensDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
