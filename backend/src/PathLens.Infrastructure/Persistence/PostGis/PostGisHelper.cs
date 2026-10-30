using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace PathLens.Infrastructure.Persistence.PostGis;

/// <summary>
/// Detects PostGIS availability and provides geography helpers that work with the
/// existing latitude/longitude columns via ST_MakePoint(longitude,latitude)::geography.
/// No new table column is required: reports stay lat/lon, queries become spatial
/// when the extension exists — otherwise the repository falls back to the old
/// bounding-box + Haversine logic. This keeps local 5433 (no PostGIS) and
/// Docker PostGIS (5432) both green — Blueprint Phase 4 promise without breaking local.
/// </summary>
public static class PostGisHelper
{
    private static bool? _availableCache;
    private static readonly SemaphoreSlim CacheLock = new(1, 1);

    /// <summary>Probe pg_extension for postgis; caches result per process (invalidated on failure).</summary>
    public static async Task<bool> IsAvailableAsync(PathLensDbContext db, CancellationToken ct = default)
    {
        if (_availableCache.HasValue) return _availableCache.Value;
        await CacheLock.WaitAsync(ct);
        try
        {
            if (_availableCache.HasValue) return _availableCache.Value;
            try
            {
                var conn = db.Database.GetDbConnection();
                var wasOpen = conn.State == System.Data.ConnectionState.Open;
                if (!wasOpen) await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1 FROM pg_extension WHERE extname='postgis' LIMIT 1";
                var result = await cmd.ExecuteScalarAsync(ct);
                if (!wasOpen) await conn.CloseAsync();
                _availableCache = result != null && result != DBNull.Value;
                return _availableCache.Value;
            }
            catch
            {
                // No extension, no permission, or local DB without PostGIS — treat as unavailable, not fatal
                _availableCache = false;
                return false;
            }
        }
        finally { CacheLock.Release(); }
    }

    /// <summary>Try to enable PostGIS at startup (best-effort, never throws).</summary>
    public static async Task TryEnableAsync(IServiceProvider services, ILogger logger, CancellationToken ct = default)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PathLensDbContext>();
            var conn = db.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DO $$ BEGIN CREATE EXTENSION IF NOT EXISTS postgis; EXCEPTION WHEN OTHERS THEN RAISE NOTICE 'PostGIS not available: %', SQLERRM; END $$;";
            await cmd.ExecuteNonQueryAsync(ct);
            if (!wasOpen) await conn.CloseAsync();
            _availableCache = null; // re-probe after attempt
            var available = await IsAvailableAsync(db, ct);
            logger.LogInformation("PostGIS availability after enable attempt: {Available}", available);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PostGIS enable probe failed — running in lat/lon fallback mode.");
            _availableCache = false;
        }
    }

    // SQL fragments for ST_MakePoint(longitude, latitude)::geography helpers
    public const string MakePointGeography = "ST_MakePoint({0}, {1})::geography";
    // Use ST_DWithin for radius search (geography, uses metres)
    public const string DWithinCondition = "ST_DWithin(ST_MakePoint(longitude, latitude)::geography, ST_MakePoint({0}, {1})::geography, {2})";
    // Viewport via ST_MakeEnvelope
    public const string WithinEnvelopeCondition = "ST_Within(ST_MakePoint(longitude, latitude)::geography, ST_MakeEnvelope({0}, {1}, {2}, {3}, 4326)::geography)";
}
