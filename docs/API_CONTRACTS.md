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

## Cameras (Phase A+B)

- `GET /api/cameras` — public list with freshness (`isStale`), last attempt/change/error
- `GET /api/cameras/{id}` — public detail
- `POST /api/cameras` — Admin only; a source cannot be enabled without a permission/terms note (`400` otherwise); cadence minimum 30s (NYC profile 60s)
- `PATCH /api/cameras/{id}` — Admin only; cadence/permission/enabled (removing the note auto-disables)
- `POST /api/cameras/{id}/poll` — Admin only; one manual poll: `Changed` / `Unchanged` (hash dedupe, no new evidence or AI cost) / `NoMatchingReport` (no report within 100 m; change recorded only) / `Failed` (backoff applies)

Phase C behaviour: after a camera snapshot finishes analysis, it is auto-compared with the prior snapshot from the same camera (existing comparison engine; no manual call needed). A changed image far from any report attaches to that camera's unverified `CameraDetected` candidate (`cameraSourceId` set, `isCameraCandidate` in the reviewer queue) instead of being dropped.

Reviewer queue now also returns `staleCameras` (enabled cameras with no successful check within the stale threshold) and queue reports carry `isCameraCandidate`. Route obstacles carry `isCameraCandidate` too.

Camera cost gate: `Cameras:DailyAnalysisBudgetPerCamera` (default 240, 0 = unlimited). When spent, a changed snapshot returns `BudgetExceeded`, stores nothing, and leaves the reason in the camera's `lastError` until the UTC day resets. Privacy: snapshots are stored privately and go through the same downscale + EXIF strip as uploads before AI; no face/plate blurring is claimed (no detector integrated).

Phase F1 (explainable ranking): route explanations now include per-obstacle evidence age with a 30-day stale flag, a certain/uncertain certainty split per route, and a cross-route ranking reason (`Route 2 ranks below Route 1 because...`) naming the exact key that separated them — obstacle count, human-verified count, or distance. Ranking order itself is unchanged (F2 will revisit scoring).

Phase F2 (wheelchair scoring v1): reports accept optional observed attributes `hasStairs`, `hasRamp`, `surface` (Smooth|Uneven|Broken) with `attributeSource`; omitted means unknown and is never estimated. Route requests accept `preferences` (avoidStairs hard rule for human-verified stairs, preferVerifiedEvidence, avoidStaleEvidence, preferShortest); responses echo `appliedPreferences` and flag `hasConfirmedStairs`. Preferences reorder only - they never hide known obstacles.

Phase F3 (camera frame quality gate): camera frames are measured locally before any AI call (brightness, sharpness, perceptual hash; thresholds in `Cameras` config). Evidence responses carry `qualityFlag`/`qualityNote`/`perceptualHash`/`imageBrightness`/`imageSharpness`. TooDark/TooBlurred frames are stored but never analysed - the job records why and the pair comparison stays Inconclusive. ViewChanged frames are analysed but their comparison is forced Inconclusive with the repositioning reason. Frames too small to judge are Unknown, never Suitable.

Phase F4 (duplicate detection): creating a report (or camera candidate) within 25 m of a non-duplicate report of the same type (or Unknown) sets `possibleDuplicateOfReportId` - a suggestion, never an auto-merge. `POST /api/reports/{id}/duplicate/confirm` and `/reject` (Reviewer/Admin, audited) decide it; without a suggestion they return 400. Confirmed duplicates (`duplicateOfReportId`) are excluded from lists, map, summaries and route evaluation, while `GET /api/reports/{id}` still returns the pointer report with its evidence.
