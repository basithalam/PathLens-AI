# Security — PathLens AI (G1 Updated 2026-10-31)

**Rule:** Never commit real keys, faces, plates, or exact private addresses. This file documents how secrets are handled for G1 live AI.

## Secrets handling

| Secret | Local | Production (Hetzner) | Never |
|---|---|---|---|
| `POSTGRES_PASSWORD` | `.env` (gitignored) + `docker-compose.yml` env | GitHub Secret `POSTGRES_PASSWORD` → `.env` via `deploy-hetzner.yml` | committed |
| `JWT_SIGNING_KEY` | `.env` | GitHub Secret `JWT_SIGNING_KEY` | committed, must be 32+ chars, fail-closed in Production (`Program.cs:39`) |
| `MetaAi__ApiKey` / `META_AI_API_KEY` | `.local/set-meta-env.ps1` (gitignored, contains `$env:MetaAi__ApiKey`) | GitHub Secrets `META_AI_API_KEY` (+ `META_AI_ENDPOINT`, `META_AI_MODEL`) → `.env` → `docker-compose.yml` → `api` env `MetaAi__ApiKey` | committed, frontend, logs, error messages (code truncates body to 600 chars, never logs key) |
| `DATUMFEED_API_KEY` | `.local/` | GitHub Secret `DATUMFEED_API_KEY` | committed |

## G1 Fix: Exposed key rotation

A Llama API key was written in plaintext into `.local/set-meta-env.ps1` and then **quoted in full into this
document** while writing the rotation note. That made the credential public in two ways:

| Where | State now |
|---|---|
| `.local/set-meta-env.ps1` | gitignored, holds a placeholder |
| `docs/SECURITY.md` (this file) | masked to `LLM_1134…<redacted>` on 2026-11-03 |
| `README.md` | key id masked to `LLM_<id>_…` on 2026-11-03 |
| **git history** | **still contains the full value** in `771a485` and `2386b7f` (plus cline checkpoint commits) — search history for the `LLM_1134…` prefix to list them |

The old key value is therefore treated as **compromised and revoked**, not as something this file still
protects. **Action required:** revoke it in the Meta/Groq developer console and issue a replacement; a repo
that was public at any point cannot be un-leaked by editing the current files.

History was **not** rewritten (`filter-repo` + force-push). Rewriting shared history days before a
submission deadline risks breaking the deployed clone at `/opt/pathlens`, which pulls `main` — the
revocation is the control that actually removes the risk.

Use endpoint `https://api.meta.ai/v1/chat/completions`, or the Groq fallback
`https://api.groq.com/openai/v1/chat/completions` / `https://api.llama.com/v1/chat/completions` (old
`api.meta.ai/compat/v1` returned 404, G1).

## Local setup (G1)

```powershell
# 1. Create .env from example
cp .env.example .env
# 2. Edit .local/set-meta-env.ps1: paste Groq gsk_... or Llama API key into $env:MetaAi__ApiKey
# 3. Load env
. .\.local\set-meta-env.ps1
# 4. Run API
dotnet run --project backend/src/PathLens.Api --urls http://localhost:5145
# 5. Check health
curl http://localhost:5145/health/ready  # ai-provider should be Healthy (not Degraded)
```

## Production setup (Hetzner CD)

`.github/workflows/deploy-hetzner.yml` writes `.env` on the VPS from GitHub Secrets (never committed). Required secrets:

```
HETZNER_HOST=94.130.109.67
HETZNER_USER=deploy
HETZNER_SSH_KEY=<private key>
POSTGRES_PASSWORD=<strong>
JWT_SIGNING_KEY=<32+>
META_AI_ENDPOINT=https://api.groq.com/openai/v1/chat/completions
META_AI_MODEL=meta-llama/llama-4-maverick-17b-128e-instruct
META_AI_API_KEY=gsk_...
# optional
AI_PROVIDER=Meta  # default is Mock if not set — G1 requires Meta
```

## API key is server-side only

- Frontend `apps/web` never sees `MetaAi__ApiKey` (only `NEXT_PUBLIC_API_BASE_URL`).
- Backend `MetaImageAnalysisService.cs` reads `MetaAi__ApiKey` via `IOptions<MetaAiOptions>` from `appsettings.json` + env override.
- `AiProviderHealthCheck` reports `Degraded` with missing fields if not set, `Healthy` with `key present` if set.
- Image EXIF (GPS) stripped via `ImageDownscaler` before sending to AI.

## Reporting

If a secret was committed: `git rm --cached <file>` + rotate key + `git push --force-with-lease` after team coordination. See `docs/PRIVACY_RETENTION.md` for data retention (30d camera, 1y evidence, soft-delete).

## References

- Blueprint §AI Integration: provider abstraction, mock labelled.
- `docs/AI_GATE_A_REPORT.md` G1 probe table (honest 404/401 logs).
- `backend/src/PathLens.Infrastructure/Health/OsrmHealthCheck.cs` — health never leaks key.
