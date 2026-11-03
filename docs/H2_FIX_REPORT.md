# H2 — Honest Demo Complete (days 4–10) — Completion Report
Date: 2026-10-30 | Status: DONE (blocked gates documented honestly) | Depends: H1

## Summary
To prove "real, not mock" for H2, three gates were probed: Live Meta AI, Live Camera, and Evaluation. Two of
them were blocked by network and credential limits — but no fake data was produced, because the Blueprint
forbids it. Instead the end-to-end evaluation was run successfully through the Mock pipeline, and every
honest document was updated to say so. Combined with H1, the demo is now one-click and free of 404s.

## H2.1 Live Meta AI Gate A
**Goal:** following Blueprint §AI Integration Requirements, verify image input capability, endpoint, auth and
response schema before claiming "Live AI".

**Probes 2026-10-30:**
- Meta `https://api.meta.ai/compat/v1/chat/completions` (key `LLM_1134...` from `.local/set-meta-env.ps1`) → **404** `Path '/compat/v1/chat/completions' was not found`
- Groq `https://api.groq.com/openai/v1/chat/completions` (key `gsk_9ce6...`) → **401 Unauthorized**
- No image was analysed live; no result was fabricated.

**Files:**
- `docs/AI_GATE_A_REPORT.md` (NEW) — full probe table + body + unblock steps (user must supply exact OpenAI-compatible host/model/key)
- `docs/AI_EVALUATION.md` — updated with H2 Mock run + both Mock/Live run commands + limitations statement

**Verification:**
- `MetaImageAnalysisService` still fails loud: `Meta AI is not configured...` — never invents.
- With `Ai__Provider=Mock`, pipeline green (see H2.3).

## H2.2 SF Camera Gate
**Goal:** verify one live camera from 511SF Bay / SFMTA / Caltrans (permission note plus 3 polls).

**Probes 2026-10-30:**
- `webcams.nyctmc.org/api/cameras` (NYC) → timeout (US-only, 2026-10-28 + 2026-10-30)
- `https://511.org` Head → inconclusive, `https://511sfbay.org/developers` → 525 SSL/timeout
- No live source enabled, no permission note fabricated — registry still refuses enable without note.

**Files:**
- `docs/camera-live-gate.md` — updated to **BLOCKED on both legs (2026-10-30 probe)** + new rows for 511 probes + H2 honest status (DB on 5433, Mock pipeline, map/coverage still work)

**Verification:**
- `CameraApiTests` still cover Changed→Unchanged→Changed locally — proves pipeline, not provider access.

## H2.3 Evaluation Pipeline Proof (Mock)
**Goal:** Reproducible `evaluation/run-evaluation.ps1` pipeline proof (labels BEFORE AI, Correct/Incorrect/Inconclusive separate, errors exit 1).

**Run 2026-10-30 11:57 UTC:**
- DB: `localhost:5433` trust (`.local/pgdata`, started via `pg_ctl -o "-p 5433"`)
- API: `dotnet run --urls http://localhost:5145` with `Ai__Provider=Mock`, `ConnectionStrings__DefaultConnection=Host=localhost;Port=5433;Database=pathlens;Username=postgres` → `/health/ready` Healthy
- Manifest: `evaluation/manifest.example.csv` (3 images, 1 pair, `sample-tiny.png` 1×1)
- Result: `evaluation/results.json` → **images 3/3 correct, pairs 1/1 correct, 0 incorrect, 0 inconclusive, 0 errors** — provider `Mock` `mock-deterministic-v1` (Unknown, confidence 0) → Inconclusive as expected
- Note: Mock always returns `Unknown` — proves upload→job→parser→comparison end-to-end, not accuracy. Live 20-30 own-photo evaluation still needs correct endpoint/key (see AI_GATE_A_REPORT).

**Files:**
- `evaluation/results.json` (NEW, 3336 bytes, Mock) — committed for demo backup/offline
- `docs/AI_EVALUATION.md` — Latest run table + limitations statement: "A 20-30 image set is a demo evaluation, never proof of general accuracy. Inconclusive is reported separately from incorrect. Mock provider results are not real AI inference."

## H2.4 Demo Script Polish
**Goal:** 2-min script reflects H1/H2 honest state + backup plan.

**Files:**
- `docs/DEMO_SCRIPT.md` — rewritten to H2 Ready: 0-20s problem, 20-50s Coverage shock (F5 grid 50m, ≤1000 cells), 50-90s Route+F2 (OSRM real geometry, avoidStairs hard rule), 90-110s AI+audit (honest Mock label + Gate A blocked note), 110-120s F6 QuickFeedback + camera freshness, plus H1/H2 backup checklist (docker compose, health/live+ready, no secrets, 5433 offline seeded DB, 1080p pre-record).

## Additional H2 fixes
- `backend/src/PathLens.Api/appsettings.Development.json` — `Port=5432;Password=pathlens_local_dev` → `Port=5433;Database=pathlens;Username=postgres` (trust, matches `.local` cluster; no secret)
- `.env.example` already documents `POSTGRES_PASSWORD`, `JWT_SIGNING_KEY`, `NEXT_PUBLIC_API_BASE_URL` etc. — no real key committed.
- `.gitignore` already ignores `Blueprint.md`, `.env`, `*.local`, `Cameras__DatumfeedApiKey` — `appsettings.Development.json` restored to non-secret state before commit (verified via `git diff`).

## Build Proof (H1+H2)
```
dotnet build backend/PathLens.slnx -c Release → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test PathLens.UnitTests -c Release → Passed 39
npm run typecheck → pass
npm run lint → 0 error, 4 warnings (pre-existing exhaustive-deps)
npm run build (apps/web) → Compiled successfully, 8 routes
API /health/ready → Healthy (Mock pipeline run)
evaluation/run-evaluation.ps1 → 3 correct / 0 incorrect (Mock)
```

## Files Changed (H2 on top of H1)
- backend/src/PathLens.Api/appsettings.Development.json
- docs/AI_GATE_A_REPORT.md (NEW)
- docs/camera-live-gate.md
- docs/AI_EVALUATION.md
- docs/DEMO_SCRIPT.md
- evaluation/results.json (NEW)
- docs/H2_FIX_REPORT.md (this file)

## Next: H3 Scale Foundation
PostGIS geometry migration (lat/lon → PostGIS Point + GiST), distributed processor lock, wheelchair routing validation (OSRM wheelchair profile vs Valhalla), camera quality calibration. Requires H2 approval.
