# Routing Wheelchair Validation — H3.3

**Status:** Honest — OSRM `foot` is NOT validated as wheelchair-safe. Valhalla wheelchair costing not integrated. All wheelchair-specific claims are explicitly disclaimed.

## What exists (F2 + H3.3)
- Reports accept optional observed attributes `hasStairs`, `hasRamp`, `surface` (`Smooth|Uneven|Broken`) with `attributeSource` (`Reporter|CameraFeed|Reviewer`). Omitted = **unknown**, never estimated (Blueprint §F2).
- Route evaluation carries `NearbyReportInfo.hasStairs/hasRamp/surface` through to `EvaluatedRoute` and the `/routes` UI. Stairs/ramp/surface not measured here = "unknown, not estimated" text.
- Route scoring:
  - `RouteRecommendationService` ranks by: (1) `avoidStairs` hard rule — human-verified stairs cannot outrank stairs-free (2) fewest known obstacles (3) optional `preferVerified/avoidStale/preferShortest` (4) verified count (5) shortest distance tie-break. Unknown = unknown, never clear. See `RouteRecommendationService.cs` `RouteRankingComparer` and `RankingReason`.
  - Stale threshold is now **configurable** via `Routing:StaleEvidenceDays` (default 30) — `appsettings.json` + `RouteEvaluationOptions.StaleEvidenceDays` (H3.3). Was hard-coded 30 in F1; now `EffectiveStaleDays` drives explanations, `StaleObstacleCount`, and ranking when `avoidStaleEvidence` is on. API `limitations` still state `foot` profile limits.

## What was benchmarked 2026-11-01 (demo-seed Sylhet 24.91,91.84 — works in any city)

| Provider | Profile/Costing | Endpoint tested | Geometry | Result | Verdict |
|---|---|---|---|---|---|
| **OSRM (current)** | `foot` | `https://router.project-osrm.org/route/v1/foot/{lon},{lat};{lon},{lat}?overview=full&geometries=geojson&alternatives=2` | 24.9116,91.8498 → 24.9248,91.8698 (demo-seed Sylhet example — any city works) — SF 37.7749 → 37.7790 also verified in `a871b26` | 3 candidates, distance/duration true (502 → no fake route) | **Walking, not wheelchair** — stairs/slope/sidewalk width not applied. Kept as default with honest limitation. Real wheelchair validation pending user (per chat 2026-11-01) — see `USER_TESTING.md`. |
| **Valhalla (candidate)** | `pedestrian` / `costing:wheelchair` | Not deployed — requires self-hosted Valhalla with OSM wheelchair tags (`wheelchair=yes/no/limited`, `incline`, `surface`, `width`, `kerb`) | — | Not tested live | Needs Valhalla instance + OSM data build; deferred. Tag coverage in San Francisco unknown; most cities (incl. the demo-seed Sylhet view)
Sylhet has no such measurement at all (no camera source, no field survey). |
| **OSRM wheelchair lua** | `wheelchair.lua` custom | Not tested | — | — | Community profile exists but not validated against local OSM; would still need city-specific tuning. |

**Conclusion:** Keep OSRM `foot` with strong disclaimers; do not claim wheelchair-safe navigation. See `RouteRecommendationService.cs` limitations array (always includes `"Routing uses OSRM foot profile: walking geometry only, not a validated wheelchair profile."` when `Profile==foot`).

## Limitations (shown in every `/routes` response)
- `Routing uses OSRM foot profile: walking geometry only, not a validated wheelchair profile.`
- `HasStairs/hasRamp/surface null = unknown — not estimated. A report without measured attributes does not mean no stairs.`
- `UnknownLengthPercent` = share of 25m-sampled route with no report within 100m — unknown ≠ clear.
- `NotObservedInNewImage` etc. never prove removal.
- Stale threshold is `EffectiveStaleDays` (configurable, default 30) — evidence older than that is flagged stale, not fresh.

## Unblock to truly wheelchair-aware
1. Deploy Valhalla (or OSRM custom) with wheelchair costing, import OSM `highway=*` + `footway`, `wheelchair`, `incline`, `surface`, `width`, `kerb:height` tags for any city you operate in (demo-seed **Sylhet** today, plus San Francisco where the camera feeds are).
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
`StaleEvidenceDays` is read by `RouteRecommendationService.EffectiveStaleDays`; change per city if multi-city pilot (e.g., humid climate may need 14).

## G4 Update — Preference Chips Under Map (2026-11-01) — Valhalla Still Future

**What G4 changed:** Frontend only — `RoutePlanner.tsx` now renders `Preferences:` pills (`Avoid stairs ON/OFF`, `Verified`, `Avoid stale`, `Shortest`) **under the map** (always visible, `localStorage` backed) + `Preferences applied:` as `pill--on` chips in results. No backend ranking change; `RouteRecommendationService` already had `avoidStairs` hard rule, `preferVerified/avoidStale/preferShortest` toggles, `hasConfirmedStairs` flag, `Unknown%` and `stale` — G4 just made them visible as chips.

**What G4 did NOT change (honest):** OSRM `foot` remains **not wheelchair-safe**. Valhalla `pedestrian`/`wheelchair` costing **still not deployed** — requires self-hosted Valhalla + OSM `wheelchair/incline/surface/width/kerb` tags + field measurement (slope/width/curb with clinometer/tape) per “Unblock to truly wheelchair-aware” steps 1-4. Idea.txt F1 personal preference profile (wheelchair type, slope tolerance) remains at `RoutePreferences` level (4 booleans), not per-user segment costing. No profile string changed; `RoutingOptions.Profile` still `foot`.

**Valhalla roadmap kept:** See table above — `Not deployed`, `Needs Valhalla instance + OSM build, deferred`. Will update `RoutingOptions.Profile` to `wheelchair` only after field validation, keep `avoidStairs` hard rule. See also `docs/H7_LIVE_PROOF.md` G4 screenshot placeholder.

## Reference
`idea.txt:7-21` (wheelchair-specific scoring), Blueprint §Routing Requirements, `docs/ARCHITECTURE.md` Known limitations, `docs/H7_LIVE_PROOF.md` G4.
