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

## Secrets — No Real Keys Committed (G6 Verification 2026-11-01)

```powershell
# Check entire history for real keys — should return ONLY placeholders, never a real secret
git log --all -p -- "*.md" "*.cs" "*.yml" "*.ps1" "*.json" | Select-String -Pattern "API_KEY|ApiKey|META_AI" | Select-Object -First 20
# → shows: META_AI_API_KEY=${{ secrets.META_AI_API_KEY }}, "gsk_..." placeholder, "LLM_..." placeholder, $env:MetaAi__ApiKey="gsk_..." placeholder, empty META_AI_API_KEY=  # paste here — never a real gsk_xxx or LLM_xxx key

# Stricter: grep for real key patterns (Groq gsk_ + 20+ alnum, OpenAI sk- , Llama LLM_ + digits)
git log --all -p | Select-String -Pattern "gsk_[a-zA-Z0-9]{20,}|sk-[a-zA-Z0-9]{20,}|LLM_[0-9]{10,}_[A-Za-z0-9]{10,}" | Select-Object -First 20
# → (no output) — proves no real key ever committed

# Verify ignored files never tracked
git ls-files | Select-String -Pattern "^\.env$|^\.local/|^\.env\.camera"
# → (no output)

# .env.example is the only env file tracked, and it holds placeholders:
Get-Content .env.example | Select-String "META_AI_API_KEY"
# → META_AI_API_KEY=  # paste real key here locally; production via GitHub Secrets META_AI_API_KEY
```

**Corrected 2026-11-03 — this line previously said the history held only placeholders. It did not.** Measured
with `git log -S "LLM_1134" --all`, the full key value is reachable in tracked commits `771a485`
and `2386b7f` (plus cline checkpoint commits), because the rotation note in `docs/SECURITY.md` quoted it.
Current state of the tracked tree:

- `docs/SECURITY.md` and `README.md` now carry masked forms (`LLM_1134…<redacted>`, `LLM_<id>_…`), never a usable value.
- `.env.example` is the only tracked env file and holds empty placeholders.
- `${{ secrets.META_AI_API_KEY }}` references in the workflows are the only live-key path.
- The exposed key is treated as **revoked**, and history was deliberately **not** rewritten — see
  `docs/SECURITY.md` G1 for why.

Frontend never sees `MetaAi__ApiKey` (only `NEXT_PUBLIC_API_BASE_URL`). Re-run the grep above before
submission; if it lists any file other than the masked docs, the mask regressed.

## Current status

Backend: 105 tests passing (52 unit + 53 integration: reports, evidence, comparisons, map viewport/summary/coverage, routes with G4 chips + honest foot profile, auth/audit, cameras, duplicate, feedback, H4 feedback-summary; the 13 new unit tests cover SAM 3.1 box parsing, the sweep-line union, the 100% cap and both "unknown, never 0%" invariants) + G5 CI postgres:17-3.5 service + strict `/health/ready` Healthy gate. Frontend: lint, typecheck, and production build passing (G3 confidence bar + humanReview callout + 429 backoff, G4 chips under map, a11y marker keyboard + alt, PWA manifest, offline queue, compress). ImageSharp 3.1.8 — pinned, with `NU1902/NU1903` advisories **suppressed via `NoWarn`**, so the 0-warning build is partly configured rather than clean (`docs/SECURITY.md`). F5 coverage + F6 feedback + H3 PostGIS fallback + H4 a11y/PWA/feedback-loop + G1 live Meta gate + G2 25-image SF dataset (synthetic placeholders) + G6 verification covered.
