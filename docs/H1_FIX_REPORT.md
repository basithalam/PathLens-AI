# H1 — Hotfix & Demo Unblocked — Completion Report
Date: 2026-10-30 | Scope: days 1–3 | Status: DONE

## Summary
The five P0 blockers from the audit were fixed together. Backend Release and the frontend production build
were both green afterwards (0 errors, 0 warnings), so a judge opening the demo would not meet a crash or a
404 in the first 30 seconds.

## H1.1 Leaflet 404 fix
- **Root cause:** `leaflet.css` asks for `images/marker-*.png` relatively, while Next.js serves from `/public`.
  No custom icon path was configured, so the browser requested `/marker-icon.png` and got a 404
  (dev log: `GET /marker-shadow.png 404`).
- **Fix:**
  - copied 5 files from `apps/web/node_modules/leaflet/dist/images/` into `apps/web/public/`
    (marker-icon.png, marker-icon-2x.png, marker-shadow.png, layers.png, layers-2x.png)
  - `apps/web/src/lib/leaflet-icons.ts` — new shared helper `fixLeafletIcons(L)`: deletes the prototype
    `_getIconUrl` and sets absolute `/marker-*.png` paths through `mergeOptions`
  - `AccessibilityMap.tsx`, `RoutePlanner.tsx`, `MiniMapPicker.tsx` — call `fixLeafletIcons(L)` immediately
    after `await import("leaflet")`
- **Verification:** `npm run build` succeeded, markers visible when opening `http://localhost:3000/map`,
  zero 404s in the dev log

## H1.2 Pagination preserves filters
- **Root cause:** the pagination links in `apps/web/src/app/reports/page.tsx` built only `?page=N`, so
  `search` and `obstacleType` were dropped when the reader moved pages.
- **Fix:** added the `buildPageHref(params, page)` helper — it rebuilds the query with `URLSearchParams`,
  preserving the active filters and setting the page. Previous and Next both go through it.
- **Test:** `/reports?search=ramp&obstacleType=BlockedSidewalk&page=2` → click Previous → page 1 still carries
  the same search and type
- **Verification:** `npm run typecheck` and `npm run lint` pass (0 errors)

## H1.3 Full docker-compose
- **Before:** only the `db` service existed, so a one-click demo was impossible
- **After:** 4 services
  - `db` — postgis:17-3.5, password `${POSTGRES_PASSWORD:-pathlens_local_dev}`
  - `api` — `backend/Dockerfile` (sdk→aspnet, entrypoint sets `DOTNET_ROLE=api`), healthcheck on
    `/health/live`, volume `pathlens-storage:/app/storage`
  - `web` — `apps/web/Dockerfile` (node:20-alpine, standalone output), build arg `NEXT_PUBLIC_API_BASE_URL`
  - `worker` — same image with `DOTNET_ROLE=worker`, under the `worker` profile (starts only with
    `docker compose --profile worker up` — per ARCHITECTURE.md §31, api and worker do not run in one container)
- **Supporting files:**
  - `backend/Dockerfile` + `backend/docker-entrypoint.sh`
  - `apps/web/Dockerfile`
  - `apps/web/next.config.ts` → `output: "standalone"` enabled
  - `.env.example` — every env default documented
  - `docker-compose.yml` with volumes `pathlens-db-data` and `pathlens-storage`
- **Run:** `docker compose up --build` (or `docker compose --profile worker up` for the headless worker)
- **Verification:** `docker compose config` valid, `npm run build` produces the standalone output

## H1.4 ImageSharp Release licence gate
- **Before:** `SixLabors.ImageSharp 4.1.3` failed the Release build with
  `error : No Six Labors license found`, so `dotnet build -c Release` failed and Docker publish was impossible
- **Fix:** downgraded `4.1.3` → `3.1.8` (Apache 2.0, no licence gate) and added
  `<NoWarn>NU1902;NU1903</NoWarn>` to suppress the known advisories for the hackathon. The suppression is
  disclosed in `docs/SECURITY.md` ("Known advisories left open on purpose") — a 0-warning build is partly
  configured, not incidental. Production TODO: move to 4.x under a community licence, or switch to SkiaSharp
- **Verification:** `dotnet build backend/PathLens.slnx -c Release` → `Build succeeded. 0 Warning(s) 0 Error(s)`
- **Tests:** `dotnet test PathLens.UnitTests -c Release` → 39 passed

## H1.5 Mock label audit
- **Method:** `Select-String -Pattern Mock|Demo` across `apps/web/src/**/*.{tsx,ts}`
- **Result:** all correct
  - `EvidenceSection.tsx` — `isMock = provider === "mock"` renders
    `<p class="mock-label">Demo/mock analysis — not real AI</p>` plus the disclaimer
  - `RoutePlanner.tsx` — `mock-label` notes for unknown share and `hasConfirmedStairs`
  - `ReviewerDashboard.tsx` — seeded-accounts disclaimer
  - Nothing presents mock output as real AI, and no live-AI banner was missing
- **Gate:** H2.1 adds the backend-side mock vs real provider distinction

## Build proof
```
dotnet build backend/PathLens.slnx -c Release → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test PathLens.UnitTests -c Release → Passed 39
npm run typecheck → pass
npm run lint → 0 errors, 4 warnings (pre-existing exhaustive-deps, non-blocking)
npm run build (next) → Compiled successfully, 8 routes
```

## Left for H2
- Finishing H1 does not unblock the live Meta AI key or a live SF camera — both need the user's credentials (H2)
- Next: H2.1 Gate A live verify, H2.2 one SF camera source, H2.3 evaluation dataset, H2.4 demo video

## Files changed (H1)
- apps/web/public/marker-icon.png (+ 4 more images)
- apps/web/src/lib/leaflet-icons.ts (NEW)
- apps/web/src/components/map/AccessibilityMap.tsx
- apps/web/src/components/routes/RoutePlanner.tsx
- apps/web/src/components/map/MiniMapPicker.tsx
- apps/web/src/app/reports/page.tsx
- apps/web/next.config.ts
- apps/web/Dockerfile (NEW)
- backend/Dockerfile (NEW) + docker-entrypoint.sh (NEW)
- backend/src/PathLens.Infrastructure/PathLens.Infrastructure.csproj
- docker-compose.yml
- .env.example (NEW)
- docs/SECURITY.md
