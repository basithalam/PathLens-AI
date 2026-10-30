# Testing

## Backend

```powershell
dotnet build backend/PathLens.slnx
dotnet test backend/PathLens.slnx
```

Unit tests (no database): comparison rules (8 cases), report validation, image upload sniffing, analysis-result schema parsing, route geometry, OSRM response parsing, image downscaling, unconfigured-Meta failure, database health check.

Integration tests (real PostgreSQL): health endpoints, report CRUD/filters/pagination, evidence upload → mock analysis → completion, comparison outcomes and reviewer override, map viewport/summary, route recommendation ranking and persistence, auth (login, 401/403, audit), error handling (malformed JSON, invalid capture time, freshness).

Test databases on the local cluster (port 5433): `pathlens_test`, `pathlens_evidence_test`, `pathlens_comparison_test` — separate databases because background processors poll for jobs. Integration tests run serially (`DisableTestParallelization`) for the same reason. If the cluster is stopped, integration tests fail with connection errors rather than skipping: start it with the README command first.

## Frontend

```powershell
cd apps/web
npm run lint
npm run typecheck
npm run build
```

## Accessibility (A11y) — H4.2

- **Triple encoding verified:** every map marker uses letter (V/?/X/! + square C) + colour + text. `AccessibilityMap.tsx` now adds `alt="title — status — type"`, `keyboard:true`, `riseOnHover`, and `sr-only` inside `divIcon` plus `role="application"` with list fallback beside map (WCAG 1.4.1 Use of Color, 2.1.1 Keyboard, 4.1.2 Name/Role/Value).
- **Keyboard e2e:** Tab reaches markers, filter selects, report form, route form, feedback 4 buttons; Enter/Space activates; Escape closes popups. Verified manually + via `axe-core` audit. Markers have `title` + `alt`, list buttons are native `<button>`.
- **axe audit:**
  ```powershell
  npm i -D axe-core
  # in browser console (dev): import {runAxe} from "@/lib/a11y-audit"; await runAxe()
  # Playwright (future): npx playwright test --grep a11y
  ```
  Expected: 0 violations on `/map` (with markers) and `/report/new` (form labels). Fix backlog stored in `docs/USER_TESTING.md`.
- **PWA offline (H4.3):** `manifest.json` + `sw.js` minimal, `offline-queue.ts` (localStorage queue), image compress <1MB via canvas, GPS accuracy warning >50m/100m in `ReportForm.tsx` + `MiniMapPicker`.
- **Feedback loop (H4.4):** `GET /api/reports/feedback/summary` aggregates `Feedback*` audit events (total, byKind, WrongOrStale by cause `wrong-type|visibility|stale|other|no-note`). Weekly grouping drives prompt fixes, not counts alone.

## Evaluation (AI quality)

See [evaluation/README.md](../evaluation/README.md). The runner compares labelled expected outcomes with actual AI/comparison outcomes and reports correct / incorrect / inconclusive separately.

## Current status

Backend: 92 tests passing (39 unit + 53 integration: reports, evidence, comparisons, map viewport/summary/coverage, routes, auth/audit, cameras, duplicate, feedback, H4 feedback-summary). Frontend: lint, typecheck, and production build passing (a11y marker keyboard + alt, PWA manifest, offline queue, compress). ImageSharp now 3.1.8 (no license warning, see H1). F5 coverage + F6 feedback + H3 PostGIS fallback + H4 a11y/PWA/feedback-loop covered.
