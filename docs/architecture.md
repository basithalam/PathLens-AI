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

- Location is latitude/longitude columns, not PostGIS geometry: the local PostgreSQL has no PostGIS. Docker's PostGIS image enables the Phase-4 migration to geometry when used.
- One background processor instance per database is assumed.
- JWT uses seeded accounts; there is no public registration by design for the hackathon build.
