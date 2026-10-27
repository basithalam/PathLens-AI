using Microsoft.EntityFrameworkCore;

namespace PathLens.Infrastructure.Persistence;

/// <summary>
/// Phase 0 DbContext: establishes the PostgreSQL + PostGIS foundation.
/// Business entities and their configurations are added in Phase 1.
/// </summary>
public sealed class PathLensDbContext(DbContextOptions<PathLensDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        base.OnModelCreating(modelBuilder);
    }
}
