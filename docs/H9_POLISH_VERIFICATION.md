# H9 Polish & Production — Verification (2026-10-31)

**Goal:** Demo ready — all P1 polish verified, only `docker compose up --build` + health check remains. H1/H4 already Done, this is verification only.

## Checks (all passed 2026-10-31)

| Item | Expected | Found | Status |
|---|---|---|---|
| **Leaflet 404** (H1) | 5 icons + `fixLeafletIcons` + `keyboard:true` | `public/marker-icon.png` true, `leaflet-icons.ts` true, `AccessibilityMap.tsx:236 keyboard:true alt` true | ✅ |
| **Pagination search sync** (H1) | `buildPageHref` preserves `search+type` | `reports/page.tsx:21 buildPageHref` + `Previous/Next` use it | ✅ |
| **Map/list sync** | Same reports, selectable | `AccessibilityMap.tsx` `map-layout` + `map-list` `selectedReport` `aria-live` | ✅ |
| **AI confidence + Demo/mock** | Label distinct, confidence 0..100 | `EvidenceSection.tsx:35 isMock` → `Demo/mock analysis — not real AI` + `MockImageAnalysisService.cs:13 Mock` + `mock-deterministic-v1` | ✅ |
| **Unknown vs Unverified** | Color never alone, text+letter | `VERIFICATION_STATUSES Unverified/HumanVerified...` + `Unknown is not proof of clear` + `C is unknown, not proof` + `Unknown is not clear; route may cross C` | ✅ |
| **Camera unavailable** | Clear error, stale | `AdminCamerasPanel.tsx` `Failed` + `lastError` pill, `AccessibilityMap` `no camera coverage is not proof` + `Cameras:PollingEnabled` false → no double poll | ✅ |
| **Mobile 320px** (H9 requirement) | MiniMap 320px on wheelchair tray | Fixed `ReportForm.tsx:223 height: 260 → 320` (user backlog H4.1) | ✅ fixed |
| **Keyboard Tab** | Tab to markers, filter, form, route, feedback | `AccessibilityMap marker keyboard:true alt title riseOnHover` + `role=application` + list fallback, lint 0 error 7 warnings (pre-existing exhaustive-deps only) | ✅ |
| **docker compose 1-click** | 4 services + health | `docker-compose.yml` services: db (postgis 17-3.5), api (healthcheck /health/live, depends db healthy), web (depends api healthy, NEXT_PUBLIC_API_BASE_URL), worker profile | ✅ |
| **Health live+ready** | live always, ready needs DB + osrm+ai degraded | `Program.cs:175 /health/live Predicate false, /health/ready true` + `DatabaseHealthCheck Healthy/Unhealthy` + `OsrmHealthCheck Healthy/Degraded` + `AiProviderHealthCheck Healthy/Degraded (Mock vs missing)` | ✅ |
| **Builds** | 0W0E 39 pass next 8 routes | `dotnet build Release 0W0E`, `unit 39 pass`, `next build 6.13kB routes` | ✅ |

## Mobile video 1 min (to record)
- Open SF map on phone 390px → pan/zoom → tap marker (keyboard also) → filter search → report form MiniMap 320px drag → submit → evidence upload Demo/mock badge → route 3 cards Unknown% → feedback 4 buttons → reviewer queue

## Evidence
- H1 `H1_FIX_REPORT.md` + H4 `TEST_PLAN.md` A11y section + this verification
- No new code beyond 320px fix — all H1/H4 already green, only `docker compose up --build` remains for judge 30s test
