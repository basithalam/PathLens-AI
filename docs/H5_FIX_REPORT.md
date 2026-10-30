# H5 - Production Hardened (P2) - Completion Report
Date: 2026-10-30 | Status: DONE (minimal honest) | Depends: H4

## H5.1 Security Close
- JWT fail-closed: Program.cs throws if Jwt:SigningKey missing in Production, fallback only Dev.
- CORS multi-origin: Cors:FrontendOrigin split by ',' (e.g. https://a.vercel.app,https://b.com) AllowCredentials.
- Rate limiting: AddRateLimiter fixed window login 5/min, evidence-upload 10/hour, 429, applied via RequireRateLimiting on AuthEvidence.
- Trust auth -> password: docker-compose already password pathlens_local_dev, local 5433 trust documented in PRIVACY_RETENTION.
- Content moderation: Application/Reports/ContentModeration.cs blocklist (profanity) -> Disputed queue, NSFW placeholder false, re-moderate on edit.

## H5.2 Privacy & Retention
- Face/plate blur PoC docs/PRIVACY_RETENTION.md - client face-api.js / YoloDotNet placeholder, EXIF strip verified, consent checkbox in ReportForm required, retention camera 30d evidence 1y report until deleted, GDPR DELETE /api/reports/{id} soft-delete (IsDeleted+DeletedAt, HasQueryFilter).
- Report SoftDelete, endpoint added, config index IsDeleted.
- EXIF via ImageDownscaler already.

## H5.3 CI/CD + Observability
- .github/workflows/ci.yml - backend build+unit, frontend lint/typecheck/build, health echo
- Health extended: DatabaseHealthCheck + OsrmHealthCheck (degraded on 400) + AiProviderHealthCheck (Mock vs missing key degraded)
- OpenTelemetry/Sentry placeholder: health shows degraded not unhealthy, next iteration full OTel.

## H5.4 Docs Dedup
- Removed duplicates: docs/api.md, architecture.md, data-model.md, testing.md, security.md lowercase - keep API_CONTRACTS.md, ARCHITECTURE.md etc canonical.
- README H1-H4 status already, H5 note added below.

## Files
- Program.cs (JWT fail-closed, CORS multi, RateLimiter), AuthEvidence endpoints RequireRateLimiting, ContentModeration.cs, ReportService moderation, Domain Report IsDeleted+SoftDelete, Config HasQueryFilter+Index, ReportEndpoints DELETE, PRIVACY_RETENTION.md, ci.yml, Health OsrmHealthCheck+AiHealthCheck, DependencyInjection health, docs H5_FIX_REPORT + cleanup

## Build Proof
- dotnet build Release 0W0E, unit 39 pass, next 8 routes 5.15kB