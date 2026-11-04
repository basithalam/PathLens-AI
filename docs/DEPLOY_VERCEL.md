# Deploy to Vercel (auto-deploy) + Render (backend)

**Vercel auto-deploy (frontend `apps/web`) — 5 min, no code change:**

1. **vercel.com → Add New Project → Import `basithalam/PathLens-AI`**
   - **Root Directory:** `apps/web` (IMPORTANT — monorepo)
   - **Framework Preset:** Next.js (auto)
   - **Build Command:** `npm run build` (default)
   - **Output:** `.next`

2. **Environment Variables (Vercel Dashboard → Project → Settings → Environment Variables):**
   ```
   NEXT_PUBLIC_API_BASE_URL=https://your-api.onrender.com
   ```
   Add also for Preview + Production. Redeploy after change.

3. **Push to `main` → auto deploy:** Every `git push origin main` triggers Vercel build (see Actions → Deploy workflow `verify` passed → `deploy-frontend`). No `VERCEL_TOKEN` needed if using Vercel GitHub integration. For Action-based deploy, set GitHub Secrets:
   - `VERCEL_TOKEN` (Vercel → Settings → Tokens)
   - `VERCEL_ORG_ID` (`vercel --version` → `.vercel/project.json`)
   - `VERCEL_PROJECT_ID` (same)
   - `NEXT_PUBLIC_API_BASE_URL` (your Render URL)

4. **Check:** `https://your-project.vercel.app` → `/map` markers load (needs backend live).

**Render backend (PostGIS) — Docker:**

1. **Render.com → New → Web Service → Connect `basithalam/PathLens-AI`**
   - **Root:** `backend`
   - **Dockerfile:** `./backend/Dockerfile` (auto)
   - **Env:** `POSTGRES_PASSWORD`, `JWT_SIGNING_KEY` (fail-closed in Production), `NEXT_PUBLIC_API_BASE_URL`, `OSRM_BASE_URL=https://router.project-osrm.org`, `ROUTING_STALE_DAYS=30`
   - **Database:** Render PostgreSQL with PostGIS extension (`CREATE EXTENSION postgis;` + `ix_reports_geography` as in `H3_POSTGIS_REPORT.md`)

2. **Deploy Hook:** Render Dashboard → Deploy Hook → Copy URL → GitHub → Settings → Secrets → `RENDER_DEPLOY_HOOK_URL` → next `push` triggers `deploy-backend` job.

3. **Health:** `https://your-api.onrender.com/health/live` → `Healthy`, `/health/ready` → `Healthy` (Postgres + `osrm` Degraded OK + `ai-provider`).

**Local alternative (no deploy):** `docker compose up --build` → `http://localhost:3000` + `http://localhost:5145/health/live` — valid per Rules (public repo + 3-min video).

**CD flow that actually exists:** `push main` → `ci.yml` (backend build + 55 unit + 53 integration tests against
a `postgis/postgis:17-3.5` service, then frontend lint + typecheck + build) → `deploy-hetzner.yml` (verify →
SSH to Hetzner → `docker compose up --build -d` → strict `/health/ready` gate requiring `database` and
`ai-provider` Healthy) → live URL for the demo video.

**This file is an unused alternative.** There is **no** `deploy.yml`, no Vercel workflow and no Render workflow
in `.github/workflows/` — only `ci.yml` and `deploy-hetzner.yml`, verified 2026-11-03. Everything below
describes how it *would* work if that path were taken; it is kept for reference, not as a claim about this
repository.
