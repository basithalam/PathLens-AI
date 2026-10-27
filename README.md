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

## Status

🚧 In development for the Meta Global AI Developer Hackathon 2026. Setup and run instructions will be added as Phase 0 is completed.

## License

To be decided.
