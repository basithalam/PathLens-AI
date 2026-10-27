# PathLens AI

**Evidence-Aware Accessibility Intelligence**

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
| AI | Meta AI image-capable API/model, behind a provider abstraction |
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

Real Meta AI analysis is configured server-side only (never in the frontend):

```powershell
$env:MetaAi__Endpoint = "https://api.llama.com/compat/v1/chat/completions"
$env:MetaAi__Model = "<vision-capable model id>"
$env:MetaAi__ApiKey = "<your key>"
```

Without this configuration an analysis job fails with an explicit "not configured" error — it never invents a result. For tests or a clearly labelled demo, set `$env:Ai__Provider = "Mock"`; mock findings are marked in the UI as "Demo/mock analysis — not real AI". A live Meta AI image test (Blueprint Gate A) still requires a real API key.

Evidence comparison (Phase 3): `POST /api/comparisons`, `GET /api/comparisons/{id}`, `GET /api/reports/{id}/comparisons`, and `POST /api/comparisons/{id}/review`. The deterministic rules engine checks location match, timestamp trust, and newer-photo strength before allowing `LikelyStillPresent` or `NotObservedInNewImage`; anything weak becomes `Inconclusive`, and different findings become `ConflictingEvidence`. `NotObservedInNewImage` never means an obstacle was removed. A human review is stored beside the original outcome (never overwriting it) and changes the effective outcome. The report page shows a side-by-side evidence explorer with a timeline and the review form.

Accessibility map (Phase 4): `GET /api/map/reports` (viewport bounding box with obstacle/verification filters) and `GET /api/map/summary`. The `/map` page uses Leaflet with OpenStreetMap tiles and attribution, reloads reports when the viewport moves, shows markers whose letter and text (not colour alone) encode verification status, and keeps a synchronized list beside the map. Granting location permission recentres the map; denying it leaves manual panning fully usable. PostGIS geometry is still deferred: viewport queries use the latitude/longitude columns until the Docker PostGIS database is used.

Route recommendation (Phase 5): `POST /api/routes/recommend` and `GET /api/routes/{id}`. Real route geometry, distance, and duration come from OSRM (`Routing:OsrmBaseUrl`, `Routing:Profile=foot`); if the provider fails, the API returns `502` and the UI shows no route rather than inventing one. Each candidate is evaluated against nearby reports (known obstacles within 50 m), evidence freshness, and the share of the route with no nearby evidence — unknown is reported as unknown, never as clear — and ranked by an explainable rule (fewest known obstacles, then shortest). Recommendations are persisted and can be reopened by id. The `/routes` page lets you type or click start/destination on the map and compare route cards side by side with the map.

## Status

🚧 In development for the Meta Global AI Developer Hackathon 2026. Phases 0–5 are complete: reports and evidence persist, analysis and comparison work end-to-end (mock AI provider until a Meta AI key is configured), reports are explorable on a map, and walking routes can be compared with evidence-aware explanations. Builds and tests pass.

## License

To be decided.
