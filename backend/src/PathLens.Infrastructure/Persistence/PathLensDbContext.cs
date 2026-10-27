using Microsoft.EntityFrameworkCore;
using PathLens.Domain.Reports;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PathLensDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
