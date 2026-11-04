# PathLens AI

**Built solo — one developer, one repository, no team — for the Meta Global AI Developer Hackathon 2026,
Indie Developer Track.** Runs on the Meta Model API: `muse-spark-1.3` classifies the obstacle and `sam-3.1`
measures how much of the visible walkway it occupies.

**Evidence-Aware Accessibility Intelligence — Judge: Start at `docs/JUDGE_GUIDE.md` (3-min demo, 1 page)**
**Testing with real users? `docs/WHEELCHAIR_USER_GUIDE.md` is the click-by-click manual for how a wheelchair
user gives feedback through the UI — and what this build cannot take as feedback yet.**

**Live:** https://pathlens.cognitivebd.com | **Health:** https://pathlens.cognitivebd.com/health/ready

**Pilot city: Sylhet, Bangladesh.** The map opens there and the manual photo path is built around Sylhet
footpaths. Public traffic-camera feeds exist only for San Francisco today (Datumfeed / Caltrans / 511SF /
SFMTA), so they are shown as a separate evidence source — and the absence of any Sylhet camera is stated on
the map rather than hidden.



PathLens AI is being developed for the **Meta Global AI Developer Hackathon 2026**.

It helps wheelchair users, people with mobility difficulties, and their caregivers understand whether a pedestrian path is likely to be usable — before they travel.

A regular map shows where a road is. PathLens AI shows what is known about that place, how recent and reliable that knowledge is, and what is still unknown.

## The Problem

Before using a route, people with mobility difficulties need to know:

- Is a sidewalk or ramp blocked?
- Is a previously reported obstacle still there?
- How old is the information, and who provided it?
- Is another route supported by more reliable evidence?
- Is there simply not enough information about a route?

Ordinary maps cannot confirm the current accessibility of a specific place. PathLens AI makes that information gap visible instead of hiding it.

## What PathLens AI Does

### AI Accessibility Analysis

Analyzes a photo of a path, identifies possible obstacle types, describes the visible evidence, and reports confidence and limitations. AI observations are never presented as verified facts.

### Evidence Timeline & Comparison

Compares older and newer photos and reports from the same location. Possible outcomes are:

- `LikelyStillPresent`
- `NotObservedInNewImage` — the obstacle was not seen in the newer image; this does **not** prove it was removed
- `ConflictingEvidence`
- `Inconclusive`

Every comparison includes an explanation, not just a status badge.

### Accessibility Map

An interactive map (OpenStreetMap + Leaflet) with obstacle reports, filters, report details, evidence status, and an accessible list view alongside the map.

### Evidence-Aware Route Planner

Evaluates alternative walking routes using known obstacles, evidence freshness, verification status, and data gaps. A route with no obstacle reports is never automatically labelled "safe" or "clear" — unknown segments are shown as unknown.

### Human Review & Audit

Reviewers can verify, dispute, or reject reports, add notes, and leave a full audit trail. Human-verified, user-submitted, and AI-generated information are always kept distinct.

## Tech Stack

| Layer | Technology |
| --- | --- |
| Frontend | Next.js (App Router) + TypeScript |
| Backend | ASP.NET Core Web API (.NET) |
| Database | PostgreSQL + PostGIS |
| ORM | Entity Framework Core |
| Maps | OpenStreetMap + Leaflet |
| Routing | OSRM, Valhalla, or GraphHopper (to be validated) |
| AI | Meta AI image-capable API/model, behind a provider abstraction — `muse-spark-1.3` classifies (`/v1/chat/completions`), `sam-3.1` measures the occupied share of the visible walkway (`/v1/responses`, off unless `SAM31_ENABLED=true`) |
| Storage | S3-compatible private object storage |
| Testing | xUnit, integration tests, Playwright |

The backend is a modular monolith (Domain, Application, Infrastructure, API, Worker) — not microservices — to keep AI-assisted development, debugging, and deployment simple for the hackathon.

## Repository Structure

```text
pathlens-ai/
├── apps/
│   └── web/            # Next.js frontend
├── backend/            # ASP.NET Core solution (Api, Application, Domain, Infrastructure, Worker)
├── docs/               # Product, architecture, API, security, and test documentation
├── scripts/
├── docker-compose.yml
├── .gitignore
└── README.md
```

## Development Approach

Development proceeds in small, independently verifiable phases:

0. **Project foundation** — solution structure, database connection, health endpoints, frontend shell
1. **Database & report management** — core entities, report CRUD, pagination and filters
2. **Image upload & AI analysis** — evidence storage, analysis jobs, structured AI output
3. **Evidence comparison engine** — same-location and time-based comparison with explanations
4. **Accessibility map** — OpenStreetMap/Leaflet map with spatial queries
5. **Route recommendation engine** — real routing provider with evidence-aware ranking
6. **Reviewer dashboard & trust controls** — authentication, review queue, audit trail
7. **Production-quality demo & deployment** — deployment, demo dataset, final polish

A feature is complete only when its behavior is documented, its API and data contracts are consistent, validation and error handling are implemented, automated tests pass, and the application builds successfully.

## Core Principles

- AI confidence is not a guarantee of factual correctness.
- Absence of an obstacle report does not mean a route is obstacle-free.
- `NotObservedInNewImage` never means "removed."
- No hardcoded AI responses, fake route geometry, or fabricated success states in production code. Mock providers are used only for automated tests or a clearly labelled demo mode.
- API keys and secrets stay server-side and are never committed to this repository.
- Capture time and GPS supplied by users are treated as claims, not verified facts.

## Run Locally (Phase 0)

Prerequisites: .NET 10 SDK, Node.js 20+, and PostgreSQL with PostGIS (via Docker Compose below, or an existing local install).

Start the database (optional if you already have PostgreSQL + PostGIS on `localhost:5432` with a `pathlens` database):

```powershell
docker compose up -d db
```

Start the backend (http://localhost:5145):

```powershell
dotnet run --project backend/src/PathLens.Api --urls http://localhost:5145
```

Check it: `GET /health/live` (the API itself is up) and `GET /health/ready` (also requires PostgreSQL; returns `503` when the database is unreachable). OpenAPI is available in Development at `/openapi/v1.json`.

Start the frontend (http://localhost:3000) in a second terminal:

```powershell
cd apps/web
npm install
npm run dev
```

Open http://localhost:3000 in a browser. The home page shows the live API and database status from the backend.

Backend checks:

```powershell
dotnet build backend/PathLens.slnx
dotnet test backend/PathLens.slnx
```

Local development without Docker (Windows, using an existing PostgreSQL install on port 5433):

```powershell
& "C:\Program Files\PostgreSQL\18\bin\initdb.exe" -D .local/pgdata -U postgres --auth=trust -E UTF8
& "C:\Program Files\PostgreSQL\18\bin\pg_ctl.exe" -D .local/pgdata -o "-p 5433" -l .local/pg.log start
& "C:\Program Files\PostgreSQL\18\bin\createdb.exe" -h 127.0.0.1 -p 5433 -U postgres pathlens
```

The Development connection string targets this port-5433 cluster. Note: this local PostgreSQL has no PostGIS, so report location is stored as latitude/longitude; PostGIS geometry arrives in Phase 4 with the Docker PostGIS image.

Reports API (Phase 1): `POST /api/reports`, `GET /api/reports` (search, obstacle-type filter, pagination, sorting), `GET /api/reports/{id}`, `PATCH /api/reports/{id}`, and `GET /api/reports/nearby`. The web app provides `/reports`, `/reports/{id}`, and `/report/new`.

Evidence & AI analysis (Phase 2): `POST /api/reports/{id}/evidence` (multipart photo upload, JPEG/PNG/WebP up to 10 MB, validated by file content), `GET /api/reports/{id}/evidence`, `GET /api/evidence/{id}`, `GET /api/evidence/{id}/file`, `POST /api/evidence/{id}/analyze`, and `GET /api/analysis-jobs/{id}`. Photos are stored privately on local disk under a random key (`.local/storage` by default) and served only through the API. Analysis runs as a database-backed background job with bounded retry; provider output must pass the schema 1.0 validation before it is stored.

Real Meta AI analysis is configured server-side only (never in the frontend) — **G1 FIX 2026-10-31: old `api.meta.ai/compat/v1` 404, use Groq or Llama API**:

```powershell
# Option A - Groq (recommended, free, 30s setup): https://console.groq.com/keys
$env:MetaAi__Endpoint = "https://api.groq.com/openai/v1/chat/completions"
$env:MetaAi__Model = "meta-llama/llama-4-maverick-17b-128e-instruct"
$env:MetaAi__ApiKey = "gsk_..."

# Option B - Meta Llama API: https://llama.developer.meta.com/ -> API Keys
# $env:MetaAi__Endpoint = "https://api.llama.com/v1/chat/completions"
# $env:MetaAi__Model = "Llama-4-Maverick-17B-128E-Instruct-FP8"
# $env:MetaAi__ApiKey = "LLM_..."

# Or load from gitignored file:
#   . .\.local\set-meta-env.ps1
$env:Ai__Provider = "Meta"
dotnet run --project backend/src/PathLens.Api --urls http://localhost:5145
curl http://localhost:5145/health/ready  # ai-provider must be Healthy
```

Without this configuration an analysis job fails with an explicit field-level error (`missing: MetaAi:Endpoint, MetaAi:Model, MetaAi:ApiKey`) — it never invents a result. For tests or a clearly labelled demo, set `$env:Ai__Provider = "Mock"`; mock findings are marked in the UI as "Demo/mock analysis — not real AI". See `docs/AI_GATE_A_REPORT.md` G1 + `docs/SECURITY.md` for rotation after exposed key `LLM_1134...` (masked to a placeholder here; revocation happens in the provider console and cannot be verified from this repository).

Evidence comparison (Phase 3): `POST /api/comparisons`, `GET /api/comparisons/{id}`, `GET /api/reports/{id}/comparisons`, and `POST /api/comparisons/{id}/review`. The deterministic rules engine checks location match, timestamp trust, and newer-photo strength before allowing `LikelyStillPresent` or `NotObservedInNewImage`; anything weak becomes `Inconclusive`, and different findings become `ConflictingEvidence`. `NotObservedInNewImage` never means an obstacle was removed. A human review is stored beside the original outcome (never overwriting it) and changes the effective outcome. The report page shows a side-by-side evidence explorer with a timeline and the review form.

Accessibility map (Phase 4): `GET /api/map/reports` (viewport bounding box with obstacle/verification filters) and `GET /api/map/summary`. The `/map` page uses Leaflet with OpenStreetMap tiles and attribution, reloads reports when the viewport moves, shows markers whose letter and text (not colour alone) encode verification status, and keeps a synchronized list beside the map. Granting location permission recentres the map; denying it leaves manual panning fully usable. PostGIS geometry is still deferred: viewport queries use the latitude/longitude columns until the Docker PostGIS database is used.

Route recommendation (Phase 5): `POST /api/routes/recommend` and `GET /api/routes/{id}`. Real route geometry, distance, and duration come from OSRM (`Routing:OsrmBaseUrl`, `Routing:Profile=foot`); if the provider fails, the API returns `502` and the UI shows no route rather than inventing one. Each candidate is evaluated against nearby reports (known obstacles within 50 m), evidence freshness, and the share of the route with no nearby evidence — unknown is reported as unknown, never as clear — and ranked by an explainable rule (fewest known obstacles, then shortest). Recommendations are persisted and can be reopened by id. The `/routes` page lets you type or click start/destination on the map and compare route cards side by side with the map.

Reviewer dashboard & trust controls (Phase 6): `POST /api/auth/login` issues a JWT for seeded accounts (reviewer `reviewer@pathlens.local` / `Reviewer@123`, admin `admin@pathlens.local` / `Admin@123`, user `user@pathlens.local` / `User@123` — local development only, change them elsewhere; passwords are stored hashed). Only Reviewer/Admin can call `POST /api/reports/{id}/review`, `POST /api/comparisons/{id}/review`, `GET /api/reviewer/queue`, and `GET /api/reviewer/audit`; anonymous callers get `401` and plain users `403`. Every verification change and comparison review writes an audit event with actor, previous value, new value, note, and time; the original AI outcome is never overwritten. The `/reviewer` page provides login, the queue (unverified reports and conflicting comparisons), review actions, and the audit trail. Set `Jwt__SigningKey` in the environment outside local development.

## Status

🚧 In development for the Meta Global AI Developer Hackathon 2026. Phases 0–6 + F1–F6 are complete: reports/evidence persist, analysis/comparison end-to-end, map with coverage grid (C/B/A, cache, click-to-zoom), routes with explainable ranking + wheelchair v1, human review + duplicate + feedback, cameras A–E (SF demo city 37.7749,-122.4194; NYC leg blocked honestly, now 511SF/SFMTA/Caltrans candidates). 108 tests (55 unit + 53 integration) + frontend lint/typecheck/build green.

**H1 (2026-10-30):** leaflet 404 fixed (fixLeafletIcons + public images), pagination preserves search/type, full `docker-compose.yml` (api+web+db+worker profile), `output: standalone`, ImageSharp 3.1.8 Release green, `.env.example` — `docker compose up --build` one-click. See `docs/H1_FIX_REPORT.md`.

**H2 (2026-10-30):** honest demo — Meta Gate A 404 (`api.meta.ai/compat/v1/...` not found) + Groq 401 documented in `docs/AI_GATE_A_REPORT.md`; SF camera 525/timeout in `docs/camera-live-gate.md` (both blocked, no fabrication); Mock pipeline `evaluation/results.json` 3/3 correct proof; `docs/AI_EVALUATION.md` + `docs/DEMO_SCRIPT.md` updated (2-min honest script). Secrets never committed (see `docs/SECURITY.md`).

**H3 (2026-10-30):** scale foundation — PostGIS dual-mode `ST_DWithin/ST_Within` with fallback (see `H3_POSTGIS_REPORT.md`), `SKIP LOCKED` for 2 workers, `StaleEvidenceDays` configurable (see `ROUTING_WHEELCHAIR_VALIDATION.md`), camera thresholds documented (`camera-quality-thresholds.md`). Build 0W0E, 39 pass, next 8 routes. See `H3_FIX_REPORT.md`.

**H4 (2026-10-30):** real user & trust — hallway protocol `USER_TESTING.md`, a11y marker keyboard alt + `a11y-audit.ts` + `TEST_PLAN.md` A11y, PWA `manifest.json` + `sw.js` + `offline-queue.ts` + GPS warning, feedback `GET /api/reports/feedback/summary` grouped weekly. Build 0W0E, 39 pass, next 5.15kB. See `H4_FIX_REPORT.md`.

**H5 (2026-10-30):** production hardened — JWT fail-closed, CORS multi-origin, rate limiting login 5/min upload 10/hour (429), moderation blocklist, privacy retention `PRIVACY_RETENTION.md` (30d/1y/GDPR soft-delete `DELETE /api/reports/{id}`), CI `ci.yml`, health OSRM+AI, docs dedup. Build 0W0E. See `H5_FIX_REPORT.md`.

**H6 (2026-10-30):** SF pilot ready (was Dhaka Pilot Ready — now SF sole pilot, Dhaka deferred no camera) — SF research `SF_RESEARCH.md` (OSRM 5 samples 1478-2756m), i18n `lib/i18n.tsx` EN/Bengali + Unicode report, analytics `GET /api/admin/stats` + `AdminStatsPanel` + `ReputationPanel` Bronze/Silver/Gold. Build 0W0E. See `H6_FIX_REPORT.md`.
> **Superseded 2026-11-03:** Sylhet is the pilot city again (map default, route endpoints, judge demo flow). San Francisco stays only where it is factually true — the public traffic-camera feeds and the synthetic 25-image evaluation set.
> **Also corrected:** the "i18n `lib/i18n.tsx` EN/Bengali" claim in this entry is no longer true. Bengali was dropped on 2026-11-01 to English-only for judging; `lib/i18n.tsx` was left as a dead no-op stub that nothing imported, and it has now been **deleted**. If Bengali output returns it must be real, with a test, not a stub.

**H7 / G1 (2026-11-01) — LIVE ✅:** **Meta AI Live — `muse-spark-1.3` @ `https://api.meta.ai/v1/chat/completions` — 200 Verified (2026-11-01)** — `MetaImageAnalysisService` OpenAI-compatible, `AiProviderHealthCheck` Healthy, `docker-compose.yml` + `.env.example` default `AI_PROVIDER=Meta` with **primary `https://api.meta.ai/v1/chat/completions` + `muse-spark-1.3` (Meta, $150 credit, key id `LLM_<id>…` — masked)**, fallback Groq `https://api.groq.com/openai/v1/chat/completions` + `meta-llama/llama-4-maverick-17b-128e-instruct`** (old `api.meta.ai/compat/v1` 404 fixed 2026-10-31, see `docs/AI_GATE_A_REPORT.md` G1). **Live Evidence:** `GET /health/ready` → `ai-provider: Healthy "Meta muse-spark-1.3 @ https://api.meta.ai/v1/chat/completions (key present)"` + `POST /api/reports/{id}/evidence` → `provider: Meta, model: muse-spark-1.3, BlockedSidewalk 0.85, visibleEvidence 4, confidence 90%` (Sylhet 24.91,91.84 `1663695741.jpg` — not Mock `mock-deterministic-v1 0`). `EvidenceSection.tsx` shows `✓ Live Meta AI` vs `Demo/mock` badge. Live requires `GitHub Secrets META_AI_ENDPOINT=https://api.meta.ai/v1/chat/completions, META_AI_MODEL=muse-spark-1.3, META_AI_API_KEY=LLM_...` + `AI_PROVIDER=Meta`; local: `. .local/set-meta-env.ps1` (key id `LLM_<id>…` — masked, value never committed) → `curl /health/ready` Healthy. See `docs/AI_GATE_A_REPORT.md` G1 + `docs/JUDGE_GUIDE.md`.

**G2 (2026-11-01):** SF dataset 25 synthetic placeholders (own-generated 1024x768, no face/plate, 6-10KB, SF 37.7749,-122.4194 spread) + `manifest.sf.csv` 5 pairs (2× LikelyStillPresent, NotObserved, Conflicting, Inconclusive) + `results.sf.json` Mock pipeline proof (6 correct / 19 inconclusive, 1/5 pairs) + `scripts/seed-sf.ps1` 7 markers for judges. See `evaluation/README.md` G2 + `docs/AI_EVALUATION.md`.

**G3 (2026-11-01):** Evidence UX polished — confidence bar `<40 red Low / 40-70 orange Medium / >70 green High` + yellow `Human review required` callout (blue after review) in `EvidenceSection.tsx` + `AnalysisJobProcessor` 429 5s backoff for Groq free tier (IsRateLimited → Delay → Requeue). See `docs/AI_EVALUATION.md` G3.

**G4 (2026-11-01):** Routing wheelchair v2 chips — `RoutePlanner.tsx` preference chips `Preferences: [Avoid stairs ON] [Verified OFF] [Avoid stale OFF] [Shortest OFF]` under map (always visible, localStorage) + `Preferences applied:` as `pill--on` chips, honest `walking profile; not validated wheelchair profile` kept, Valhalla `wheelchair` costing still future (self-hosted OSM tags needed). See `docs/ROUTING_WHEELCHAIR_VALIDATION.md` G4 + `docs/H7_LIVE_PROOF.md` G4 screenshot.

**G5 (2026-11-01):** Deploy & observability — `ci.yml` postgres:17-3.5 service + 92 tests (39 unit + 53 integration via `Ai__Provider=Mock`) + `deploy-hetzner.yml` strict `/health/ready` must Healthy or deploy fails (was degraded pass), `nginx-pathlens.conf` 3001 via `WEB_PORT`, `DEPLOY_HETZNER.md` 3001 verified.

**G6 (2026-11-01):** Demo & submission ready — `docs/DEMO_SCRIPT.md` 2-min (0-20 problem, 20-50 coverage C/B/A, 50-90 G2 SF image + G3 bar/callout, 90-110 route G4 chips, 110-120 audit + G5/G6) + `docs/TEST_PLAN.md` G6 secrets grep log + 92 tests green. Live URL `https://pathlens.cognitivebd.com` (Hetzner 94.130.109.67 via `nginx-pathlens.conf` 3001/5145) or `docker compose up --build` + `./scripts/seed-sf.ps1`.
> **Corrected 2026-11-03:** the G6 secrets-grep claim ("history contains only placeholders") was **false** — a full Llama API key was printed inside `docs/SECURITY.md` and is reachable in tracked commits `771a485` and `2386b7f`. It is masked in the working tree, treated as compromised (revocation is a provider-console action this repository cannot verify), and the history was deliberately not rewritten. See `docs/SECURITY.md` G1 and `docs/TEST_PLAN.md`. Also: CI had not actually run a single job until 2026-11-03 (`docs/FINAL_VERIFICATION.md`).

## Known caveats, stated plainly

Judges and users should not have to dig for these.

- **CI only started actually running on 2026-11-03.** `ci.yml` line 44 had an unquoted `echo "health: …"` —
  YAML read the inner colon-space as a mapping key, so the workflow never parsed and **no job had ever run**.
  The commands themselves were fine; the workflow was not. Run #59 is the first green one (backend 39 unit +
  53 integration, frontend lint/typecheck/build).
- **The clean build is partly configured.** `PathLens.Infrastructure.csproj` pins ImageSharp **3.1.8** and sets
  `<NoWarn>NU1902;NU1903</NoWarn>`, so `dotnet build` reports 0 warnings while two package advisories against
  that version are suppressed. Reason and upgrade path: `docs/SECURITY.md`.
- **Real-image accuracy is small and mixed.** 10 borrowed SF footpath photos through `muse-spark-1.3`:
  **3 correct, 3 incorrect, 2 model abstentions, 2 upload errors, 0 comparison pairs.**
- **Three of that set's pre-written ground-truth labels did not survive re-reading the photos** (`sf-eval-01`,
  `sf-eval-02`, `sf-eval-08`; `sf-eval-03` is not a footpath view at all). No label was quietly changed to
  agree with the model. Detail: `docs/AI_EVALUATION_SF_REAL.md`.
- **Those 10 images are Wikimedia Commons (CC BY-SA) and the per-file credit is still owed.** That is a licence
  condition, not a nicety. The 25-image SF set is **synthetic** and its pairs were run under the **Mock**
  provider — pipeline proof only.
- **Reporter reputation is empty on the live demo** because the seeded reports have no signed-in author; the
  panel shows an empty state rather than invented badges.
- **One API key is reachable in git history** (`771a485`, `2386b7f`), although it is masked in the working
  tree. It must be treated as compromised until the owner confirms revocation in the provider console;
  history was not rewritten. `docs/SECURITY.md` G1.
- **No public footpath camera exists for Sylhet**, so the automatic-update path runs on SF highway cameras that
  show traffic, not footpaths — and the product says so on screen.
- **Routing uses OSRM's `foot` profile, which is not a validated wheelchair profile.** No route is ever labelled
  wheelchair-safe here.
- **The staff logins are published on purpose.** `reviewer@pathlens.local / Reviewer@123`,
  `admin@pathlens.local / Admin@123` and `user@pathlens.local / User@123` work on the live demo and the sign-in
  panel shows all three with one-tap access, because a judge should be able to try the review console without
  asking the author for a password. This is a demo dataset on a throwaway hackathon deployment, not a
  production system. The rotation control exists (`Auth:Seed*` + `DEMO_*_PASSWORD` secrets, see
  `docs/SECURITY.md`) for anyone who runs this for real; it is **not** enabled here, deliberately.
- **License is MIT** (`LICENSE`), covering code and docs only — the ten borrowed Wikimedia Commons street
  photos stay CC BY-SA and their per-file attribution is still owed. See the License section below.

## License

Released under the **MIT License** — see [LICENSE](LICENSE).

The licence covers the code and the documentation in this repository. It deliberately does **not** relicense
third-party assets, which keep their own terms. The full inventory — every dependency with its version and
licence, every hosted service and what its terms require, the Leaflet BSD-2 notice, and the per-file
attribution ledger for the borrowed photographs — is in **[docs/THIRD_PARTY.md](docs/THIRD_PARTY.md)**.
Summary:

| Asset | Licence | Status |
|---|---|---|
| `evaluation/sf-real-eval/*.jpg` (10 street photos) | Wikimedia Commons, **CC BY-SA 4.0** | **attribution still owed** — source page and author per file are not recorded, and they are **not recoverable from the files** (no `Artist`/source field; the only `Copyright` strings inside are Adobe/Apple colour-profile boilerplate). Ledger with the fill-in columns: `docs/THIRD_PARTY.md` §6. CC BY-SA is copyleft for those images, independent of MIT on the code |
| `evaluation/sf-dataset/*.png` (25 images) | own work, synthetic placeholders | no third-party licence |
| Map tiles | © OpenStreetMap contributors (ODbL for the data) | rendered at runtime by Leaflet, not redistributed here; attribution control on all three maps |
| Geocoding | Nominatim public service | called from the browser on Enter; its ≤1 req/s + identification policy is **not enforced in code** — `docs/THIRD_PARTY.md` §4 |
| Routing | OSRM demo service responses | runtime only; custom User-Agent + Referer sent |
| `leaflet` 1.9.4 + its default marker/layer images in `apps/web/public/` | **BSD-2-Clause** | copyright notice reproduced in `docs/THIRD_PARTY.md` §5, as that licence requires |
| `SixLabors.ImageSharp` 3.1.8 | **Six Labors Split License v1.0**, used under its **Apache-2.0 branch** (granted because this project is open source) | pinned, with two advisories suppressed via `NoWarn` — see `docs/SECURITY.md` |
| Traffic camera feeds (Caltrans CCTV, one aggregator, one placeholder service) | terms **unverified** | hotlinked server-side into evidence storage; polling off on live — `docs/THIRD_PARTY.md` §7 |

No warranty. This is a hackathon prototype: the figures in `evaluation/` are a 10-image demo, not a validated
accessibility measurement — and `docs/ERROR_ANALYSIS.md` records that three of that set's own ground-truth
labels were wrong. Routing uses the `foot` profile, which is not a certified wheelchair profile.
