# Security

## Secrets

- No API key, JWT signing key, or database password is committed. Meta AI and JWT settings come from server-side environment variables (`MetaAi__ApiKey`, `Jwt__SigningKey`). The frontend receives no keys.
- Local seed passwords (`Reviewer@123`, etc.) exist for development only and are documented as such; passwords are stored as ASP.NET Core password hashes.

## Uploads

- JPEG/PNG/WebP only, ≤10 MB, verified by magic bytes — never by extension alone.
- Storage keys are random GUIDs; user file names never become paths. Files live outside the web root and are served only through `GET /api/evidence/{id}/file`.
- Oversized bodies are cut off while streaming; a failed database write deletes the stored object.

## AI data handling

- Before an image is sent to the AI provider it is downscaled and re-encoded, which strips EXIF metadata (including embedded GPS).
- While free hackathon credits are in use, provider terms say submitted content may be used for product improvement. Therefore demo/evaluation data must be our own photos without identifiable faces or number plates. Real-user photos must not be sent under free credits.
- Provider output is untrusted: it is schema-validated before storage, and `confidence` is never presented as proof.

## AuthN/Z

- JWT bearer auth; Reviewer/Admin role policy on every review and queue endpoint. Anonymous → `401`, wrong role → `403` (covered by tests).
- Review decisions require a reason for comparisons and are audit-logged with the actor.

## Errors

- RFC 9457 ProblemDetails everywhere; stack traces and provider internals are never returned. Malformed JSON is a `400`.

## Known gaps (honest list)

- No rate limiting or account registration/recovery in the hackathon build.
- The local PostgreSQL uses trust authentication on a private port (development only); the Docker setup uses a password.
- ImageSharp 4 requires a (free community) license key for Release builds; the build warns until one is configured.
