# Architecture

PathLens AI is a modular monolith. One deployable backend, clear module boundaries, no microservices.

## Backend (.NET 10)

| Project | Responsibility |
| --- | --- |
| `PathLens.Domain` | Entities, enums, domain rules (reports, evidence, comparisons, users, route records) |
| `PathLens.Application` | Use-case services, contracts, validators, comparison and route evaluation engines |
| `PathLens.Infrastructure` | EF Core/PostgreSQL, private file storage, Meta AI and OSRM adapters, background job processor |
| `PathLens.Api` | HTTP endpoints, authentication, error handling, health checks |
| `PathLens.Worker` | Optional headless host for the analysis job processor (run it **or** the API's processor against a given database, not both) |

Dependency direction: Api → Application → Domain, Infrastructure → Application/Domain. Domain depends on nothing.

## Frontend (Next.js, TypeScript)

App Router pages for home, reports, report detail/edit, evidence, map, routes, and the reviewer dashboard. Server components fetch read data; client components handle uploads, analysis polling, maps, and review actions. The browser never receives API keys.

## Key flows

- **Analysis:** upload → evidence stored privately → `POST /api/evidence/{id}/analyze` creates a database job → background processor (2s poll, bounded retry) → validated schema-1.0 result stored on the evidence.
- **Comparison:** deterministic rules (location, timestamp trust, newer-photo strength) over two stored analyses; humans may override with a reason, stored beside — never over — the original outcome.
- **Routes:** OSRM geometry → reports within 50 m are known obstacles → coverage sampling measures unknown length → explainable ranking.

## Known architectural limitations

- **H3.1 update:** Location stays `latitude`/`longitude` double columns, but `ReportRepository` now uses PostGIS `ST_DWithin`/`ST_Within` via `ST_MakePoint(longitude,latitude)::geography` when `pg_extension postgis` exists (Docker `postgis:17-3.5`). Local 5433 (no PostGIS) falls back to bounding-box + Haversine. A GiST expression index `ix_reports_geography` can be created on Docker; local skips honestly. See `docs/H3_POSTGIS_REPORT.md` for EXPLAIN verification.
- **H3.2 update:** `AnalysisJobProcessor` uses `SELECT ... FOR UPDATE SKIP LOCKED` in a transaction, so two API/worker instances sharing one DB never process the same `Pending` job. Camera polling logs that multiple workers can share the DB safely (best-effort; advisory lock would need `pg_advisory`).
- JWT uses seeded accounts; there is no public registration by design for the hackathon build.
