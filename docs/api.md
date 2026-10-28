# API

Base URL (local): `http://localhost:5145`. Errors are RFC 9457 ProblemDetails. Malformed JSON returns `400`; routing-provider failure returns `502` with no invented route.

## Health

- `GET /health/live` — process is up (no dependency checks)
- `GET /health/ready` — also requires PostgreSQL; `503` when unreachable

## Reports

- `POST /api/reports` — create (title, description, obstacleType, severity, latitude, longitude)
- `GET /api/reports` — `search`, `obstacleType`, `verificationStatus`, `page`, `pageSize`, `sort`
- `GET /api/reports/{id}`
- `PATCH /api/reports/{id}` — title, description, obstacleType, severity
- `GET /api/reports/nearby` — `latitude`, `longitude`, `radiusMeters`, `limit`
- `GET /api/reports/{id}/reviews` — verification history (audit events)

## Evidence & analysis

- `POST /api/reports/{id}/evidence` — multipart photo (JPEG/PNG/WebP, ≤10 MB, content-sniffed), optional `captureTimeUtc`, `latitude`, `longitude`, `locationAccuracyMeters`
- `GET /api/reports/{id}/evidence`
- `GET /api/evidence/{id}`
- `GET /api/evidence/{id}/file` — the stored photo
- `POST /api/evidence/{id}/analyze` — creates a job (`202`)
- `GET /api/analysis-jobs/{id}` — `Pending | Processing | Completed | Failed`, attempts, error

## Comparisons

- `POST /api/comparisons` — `{ earlierEvidenceId, laterEvidenceId }`; self-comparison and cross-report pairs return `400`
- `GET /api/comparisons/{id}`
- `GET /api/reports/{id}/comparisons`
- `POST /api/comparisons/{id}/review` — Reviewer/Admin only; reason required

## Map

- `GET /api/map/reports` — viewport bounds + optional filters, `limit` ≤ 500
- `GET /api/map/summary` — counts by type and verification status

## Routes

- `POST /api/routes/recommend` — origin/destination; returns ranked candidates with geometry, known obstacles (with coordinates), unknown-length percent, and explanations. Persisted.
- `GET /api/routes/{id}` — reopen a stored recommendation

## Auth & review

- `POST /api/auth/login` — `{ email, password }` → JWT (2h)
- `GET /api/auth/me` — current identity (authenticated)
- `POST /api/reports/{id}/review` — Reviewer/Admin; `{ verificationStatus, note }`
- `GET /api/reviewer/queue` — Reviewer/Admin; unverified reports + conflicting comparisons
- `GET /api/reviewer/audit` — Reviewer/Admin; recent audit events
