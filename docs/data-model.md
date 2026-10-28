# Data Model

PostgreSQL via EF Core. Migrations live in `backend/src/PathLens.Infrastructure/Persistence/Migrations` and are applied at API startup (failure is logged; `/health/ready` then reports the database honestly).

| Table | Purpose |
| --- | --- |
| `accessibility_reports` | One reported obstacle: title, type, severity, lat/lon, status, verification status, `LastEvidenceAt` |
| `evidence_items` | One photo per row: random storage key, content type/size, claimed capture time (separate from `UploadedAt`), optional GPS claim, analysis status/result/provider/model |
| `analysis_jobs` | Background analysis queue: status, attempts, error, timestamps |
| `evidence_comparisons` | Engine outcome + explanation + supporting JSON; review columns keep the reviewed outcome, reason, time, and reviewer |
| `route_recommendations` | Route request + full evaluated response JSON for audit/reopen |
| `users` | Seeded accounts: email (unique), display name, password hash, role, active flag |
| `review_audit_events` | Every trust change: entity, actor, action, previous/new value, note, time |

Enums are stored as strings for readability: `ObstacleType`, `SeverityLevel`, `ReportStatus`, `VerificationStatus`, `AnalysisStatus`, `EvidenceComparisonOutcome`, `UserRole`.

Invariants enforced in code and tests:

- Coordinates are validated (-90..90, -180..180) on every write path.
- An evidence item cannot be compared with itself; comparisons stay within one report.
- `LastEvidenceAt` updates at upload time, not only after analysis.
- Analysis output is stored only after schema-1.0 validation (obstacle type, confidence 0..1).
- Review actions never overwrite the original outcome or value; they append.
