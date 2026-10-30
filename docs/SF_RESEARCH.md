# SF Research — H6.1 (SF sole pilot, Dhaka deferred)

**Date:** 2026-10-30 | **Status:** DONE (OSRM verified, CCTV terms documented) | **City:** San Francisco, CA (37.7749,-122.4194) — sole pilot. Dhaka deferred — no camera source available.

## OSM Coverage (SF)
- SF Bay has dense OSM `highway`, `footway`, `sidewalk`, `crossing` coverage; `wheelchair=*`, `incline`, `surface`, `width`, `kerb` tags are sparse (per OSM TagInfo SF extract 2026-10). Tag coverage unknown is why `hasStairs/hasRamp/surface` stays null=unknown (H3.3).
- PathLens does not rely on OSM wheelchair tags for routing; it layers evidence-aware `Unknown%` on top of OSRM geometry.

## OSRM 5 Sample Routes (SF, foot profile, 2026-10-30 live)

| # | Sample (SF proxy) | Origin → Destination | Distance | Duration | Alternatives | Code |
|---|---|---|---|---|---|---|
| 1 | Market St & 5th → Ferry Building (demo) | 37.7749,-122.4194 → 37.7790,-122.4100 | 1478 m | 214 s | 1 | Ok |
| 2 | Civic Center → Union Square | 37.7794,-122.4192 → 37.7880,-122.4075 | 1928 m | 303 s | 2 | Ok |
| 3 | Embarcadero → Fisherman's Wharf | 37.7955,-122.3937 → 37.8080,-122.4177 | 2756 m | 307 s | 1 | Ok |
| 4 | Mission → SoMa | 37.7599,-122.4148 → 37.7786,-122.4050 | 2475 m | 297 s | 1 | Ok |
| 5 | Presidio → Golden Gate | 37.7989,-122.4662 → 37.8078,-122.4750 | 2156 m | 237 s | 1 | Ok |

Endpoint: `https://router.project-osrm.org/route/v1/foot/{lon},{lat};{lon},{lat}?overview=full&geometries=geojson&alternatives=2`. All returned `code: Ok` with real geometry; no invention. Alternatives vary (1–2) — PathLens evaluates all.

**Implication:** SF routing works city-wide with `foot` profile; wheelchair-specific costing still needs Valhalla (see `ROUTING_WHEELCHAIR_VALIDATION.md`). No Dhaka sample needed — SF sole pilot.

## CCTV Terms (SF, legality note — H6.1)

| Source | Access | Terms | Status |
|---|---|---|---|
| **511SF Bay** (511.org) | 511 Developer API, key required | *511 Developer Access Agreement* — accept, "powered by 511SF Bay" attribution, data not altered, rate limit per ToS | **Not yet obtained** — needs user-supplied key (like Meta key). `camera-live-gate.md` blocked honestly. |
| **SFMTA** | SFMTA Open Data / CCTV feed | SFMTA Terms of Use — public feed, check `sfmta.com` ToS before automated reuse | **To verify** — no key in repo, permission note required before enable |
| **Caltrans** | Caltrans QuickMap CCTV | Caltrans ToS — public, non-commercial, attribution | **To verify** — same gate |

**Probe 2026-10-30:** `https://www.511.org/about/terms` → 200 (terms page reachable); `511sfbay.org/developers` → 525 earlier (see `camera-live-gate.md`). No camera enabled without permission note (enforced in `CameraService`).

## Conclusion
- SF OSM + OSRM foot is sufficient for demo; 5 samples prove routing. Dhaka not needed for pilot (no camera).
- Next to unblock live camera: obtain 511SF Bay key + accept agreement → register one SF camera with permission note → 3 polls.
