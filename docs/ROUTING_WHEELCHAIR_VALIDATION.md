# Routing Wheelchair Validation — H3.3

**Status:** Honest — OSRM `foot` is NOT validated as wheelchair-safe. Valhalla wheelchair costing not integrated. All wheelchair-specific claims are explicitly disclaimed.

## What exists (F2 + H3.3)
- Reports accept optional observed attributes `hasStairs`, `hasRamp`, `surface` (`Smooth|Uneven|Broken`) with `attributeSource` (`Reporter|CameraFeed|Reviewer`). Omitted = **unknown**, never estimated (Blueprint §F2).
- Route evaluation carries `NearbyReportInfo.hasStairs/hasRamp/surface` through to `EvaluatedRoute` and the `/routes` UI. Stairs/ramp/surface not measured here = "unknown, not estimated" text.
- Route scoring:
  - `RouteRecommendationService` ranks by: (1) `avoidStairs` hard rule — human-verified stairs cannot outrank stairs-free (2) fewest known obstacles (3) optional `preferVerified/avoidStale/preferShortest` (4) verified count (5) shortest distance tie-break. Unknown = unknown, never clear. See `RouteRecommendationService.cs` `RouteRankingComparer` and `RankingReason`.
  - Stale threshold is now **configurable** via `Routing:StaleEvidenceDays` (default 30) — `appsettings.json` + `RouteEvaluationOptions.StaleEvidenceDays` (H3.3). Was hard-coded 30 in F1; now `EffectiveStaleDays` drives explanations, `StaleObstacleCount`, and ranking when `avoidStaleEvidence` is on. API `limitations` still state `foot` profile limits.

## What was benchmarked 2026-10-30 (SF demo city)

| Provider | Profile/Costing | Endpoint tested | Geometry | Result | Verdict |
|---|---|---|---|---|---|
| **OSRM (current)** | `foot` | `https://router.project-osrm.org/route/v1/foot/{lon},{lat};{lon},{lat}?overview=full&geometries=geojson&alternatives=2` | 37.7749,-122.4194 → 37.7790,-122.4100 | 3 candidates, distance/duration true (502 → no fake route) | **Walking, not wheelchair** — stairs/slope/sidewalk width not applied. Kept as default with honest limitation. |
| **Valhalla (candidate)** | `pedestrian` / `costing:wheelchair` | Not deployed — requires self-hosted Valhalla with OSM wheelchair tags (`wheelchair=yes/no/limited`, `incline`, `surface`, `width`, `kerb`) | — | Not tested live | Needs Valhalla instance + OSM data build; deferred to H5/H6. Tag coverage in SF and especially Dhaka is unknown — Dhaka Phase-2 must evaluate. |
| **OSRM wheelchair lua** | `wheelchair.lua` custom | Not tested | — | — | Community profile exists but not validated against local OSM; would still need city-specific tuning. |

**Conclusion:** Keep OSRM `foot` with strong disclaimers; do not claim wheelchair-safe navigation. See `RouteRecommendationService.cs` limitations array (always includes `"Routing uses OSRM foot profile: walking geometry only, not a validated wheelchair profile."` when `Profile==foot`).

## Limitations (shown in every `/routes` response)
- `Routing uses OSRM foot profile: walking geometry only, not a validated wheelchair profile.`
- `HasStairs/hasRamp/surface null = unknown — not estimated. A report without measured attributes does not mean no stairs.`
- `UnknownLengthPercent` = share of 25m-sampled route with no report within 100m — unknown ≠ clear.
- `NotObservedInNewImage` etc. never prove removal.
- Stale threshold is `EffectiveStaleDays` (configurable, default 30) — evidence older than that is flagged stale, not fresh.

## Unblock to truly wheelchair-aware
1. Deploy Valhalla (or OSRM custom) with wheelchair costing, import OSM `highway=*` + `footway`, `wheelchair`, `incline`, `surface`, `width`, `kerb:height` tags for SF + Dhaka.
2. Collect 5–10 measured routes with wheelchair users (idea.txt §6) — record slope (clinometer), width (tape), surface, curb height.
3. Tune costing (`avoid stairs` weight, `incline` penalty, `surface` penalty) and re-benchmark against those ground-truth routes.
4. Update `RoutingOptions.Profile` to `wheelchair` only after field validation; keep `avoidStairs` hard rule.

## Config
```json
"Routing": {
  "Provider": "OSRM",
  "OsrmBaseUrl": "https://router.project-osrm.org",
  "Profile": "foot",
  "StaleEvidenceDays": 30,
  "TimeoutSeconds": 20
}
```
`StaleEvidenceDays` is read by `RouteRecommendationService.EffectiveStaleDays`; change per city (e.g., Dhaka monsoon weathering may need 14).

## Reference
`idea.txt:7-21` (wheelchair-specific scoring), Blueprint §Routing Requirements, `docs/ARCHITECTURE.md` Known limitations.
