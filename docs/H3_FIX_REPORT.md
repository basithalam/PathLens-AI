# H3 — Scale Foundation (P1 Technical) — Completion Report
Date: 2026-10-30 | Status: DONE (dual-mode honest) | Depends: H1+H2

## Summary
PostGIS প্রতিশ্রুতি dual-mode দিয়ে পূরণ: Docker PostGIS থাকলে ST_DWithin/ST_Within+GiST, local 5433 না থাকলে fallback. Job SKIP LOCKED, stale configurable, camera thresholds documented.

## H3.1 PostGIS
- `PostGis/PostGisHelper.cs` NEW — pg_extension probe + TryEnable best-effort.
- `DependencyInjection.cs` UseNetTopologySuite, `ReportRepository.cs` Nearby/InViewport try ST_DWithin/ST_Within else fallback, no new column (ST_MakePoint on lat/lon).
- Future GiST `ix_reports_geography` on Docker; local honest skip. See `H3_POSTGIS_REPORT.md` EXPLAIN steps.
- Verification: build 0W0E, 39 pass, next 8 routes green.

## H3.2 Distributed Lock
- `AnalysisJobProcessor.cs` — SELECT FOR UPDATE SKIP LOCKED in tx, fallback to Where. `CameraPollingWorker` log multi-worker safe.
- `ARCHITECTURE.md` H3.2 update. 2 workers no longer double-process.

## H3.3 Wheelchair Routing
- `RoutingOptions.StaleEvidenceDays=30` + `Application\RouteEvaluationOptions` + `RouteRecommendationService` EffectiveStaleDays drives all stale checks. `appsettings.json` Routing:StaleEvidenceDays. `Application.csproj` Options added.
- `ROUTING_WHEELCHAIR_VALIDATION.md` benchmark OSRM foot vs Valhalla (not integrated, honest), limitations + unblock steps. hasStairs/hasRamp/surface null=unknown kept.

## H3.4 Camera Quality
- Thresholds documented in `camera-quality-thresholds.md` (10/2/30 rationale + 20-sample calibration plan). Code defaults unchanged conservative.

## Files Changed (H3)
PostGisHelper, ReportRepository, AnalysisJobProcessor, Camera*, RoutingOptions, RouteRecommendationService, Application.csproj, DependencyInjection, appsettings.json, ARCHITECTURE, 3 new docs + README H3 note.

## Build Proof
`dotnet build Release 0W0E`, `unit 39 pass`, `next build 8 routes` green.

## Next: H4 Real User & Trust
Wheelchair hallway test + A11y audit + PWA offline.

See `H3_POSTGIS_REPORT.md`, `ROUTING_WHEELCHAIR_VALIDATION.md`, `camera-quality-thresholds.md`.
