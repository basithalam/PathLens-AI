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

## Evaluation (AI quality)

See [evaluation/README.md](../evaluation/README.md). The runner compares labelled expected outcomes with actual AI/comparison outcomes and reports correct / incorrect / inconclusive separately.

## Current status

Backend: 92 tests passing (39 unit + 53 integration: reports, evidence, comparisons, map viewport/summary/coverage, routes, auth/audit, cameras, duplicate, feedback). Frontend: lint, typecheck, and production build passing. One build warning remains: the SixLabors ImageSharp license notice (see security.md). F5a/b/c coverage + F6 feedback all covered by integration tests.
