# H3.1 PostGIS — Dual-Mode Spatial Queries

**Date:** 2026-10-30 | **Status:** Code complete, runtime dual-mode (Docker PostGIS → index scan; local 5433 fallback → bounding-box + Haversine). Honest — no fake EXPLAIN.

## Why
Blueprint promised PostgreSQL+PostGIS, but local 5433 was trust-auth without extension (see `docs/architecture.md` 2026-10-28). City-scale needs `GiST` + `ST_DWithin/ST_Within`; without it, every viewport/nearby is a full scan.

## What changed
- **`PathLens.Infrastructure.Persistence.PostGis.PostGisHelper` (NEW):** Probes `pg_extension WHERE extname='postgis'`, caches, `TryEnable` best-effort (`DO $$ CREATE EXTENSION IF NOT EXISTS postgis EXCEPTION ... $$`). Never throws — local falls back.
- **`DependencyInjection.cs`:** `AddDbContext` now `UseNpgsql(..., o => o.UseNetTopologySuite())` (translation for `ST_MakePoint` etc.). Legacy timestamp switch added.
- **`ReportRepository.cs`:** `NearbyAsync` and `InViewportAsync` try `ST_DWithin(...::geography)` / `ST_Within(ST_MakeEnvelope(...)::geography)` via `FromSqlInterpolated<AccessibilityReport>` when helper says available; catch → fallback to pre-H3 bounding-box + `DistanceMetersTo` Haversine. `SummaryAsync` unchanged (counts only). SQL is parameterised, injection-safe.
- **No new column:** Reports stay `latitude`/`longitude` double columns; spatial is via `ST_MakePoint(longitude,latitude)::geography` on those columns. This keeps migrations backward-compatible and avoids breaking local.
- **Future GiST index (Docker):** When PostGIS exists, create expression index:
  ```sql
  DO $$ BEGIN
    CREATE EXTENSION IF NOT EXISTS postgis;
    CREATE INDEX IF NOT EXISTS ix_reports_geography
      ON accessibility_reports USING GIST (ST_MakePoint(longitude, latitude)::geography);
    EXCEPTION WHEN OTHERS THEN RAISE NOTICE 'PostGIS index skipped: %', SQLERRM;
  END $$;
  ```
  Run on a Docker PostGIS (5432) — local 5433 skips honestly.

## Verification
- **Local 5433 (trust, no PostGIS):** `PostGisHelper.IsAvailableAsync` → false → fallback path. `dotnet build Release 0W0E`, `dotnet test UnitTests 39 pass`, `npm build 8 routes` green. No EXPLAIN index scan expected here.
- **Docker PostGIS 5432 (postgis:17-3.5):** `docker compose up -d db` → `SELECT * FROM pg_extension WHERE extname='postgis'` → true → repository uses `ST_DWithin`/`ST_Within`. Verify:
  ```powershell
  docker exec pathlens-db psql -U postgres -d pathlens -c "EXPLAIN (COSTS OFF) SELECT * FROM accessibility_reports WHERE ST_DWithin(ST_MakePoint(longitude,latitude)::geography, ST_MakePoint(-122.4194,37.7749)::geography, 50);"
  # Expect: Bitmap Heap Scan on ix_reports_geography or Index Scan — not Seq Scan on 100k rows
  ```
  Until Docker is run, state honest fallback.
- **Tests:** Integration tests run serially on `pathlens_test` (5433) — still bounding-box, so 53 integration tests remain valid. PostGIS path is covered by unit-level helper probe, not by mandatory DB extension.

## Limitations
- Geography `ST_MakePoint` geography uses WGS84 spheroid; distance thresholds (50m obstacle, 100m coverage) are metres as required.
- Viewport `ST_Within` with `ST_MakeEnvelope(...,4326)::geography` handles antimeridian poorly — the Sylhet demo-seed view and the San Francisco camera viewports used so far both sit well away from it.
- No migration enforces PostGIS — local stays usable for hackathon judging without Docker.

## Next
Docker one-click already has PostGIS image; add the `DO $$` index creation to a future migration `20261030180000_AddPostGisSupport` once Docker flow is the default for reviewers.
