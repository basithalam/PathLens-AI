# Deploy to Hetzner — Live Demo (94.130.109.67 → https://pathlens.cognitivebd.com)

**Host:** `94.130.109.67` | **User:** `deploy` | **Demo URL:** `https://pathlens.cognitivebd.com` | **Stack:** `docker compose` at `/opt/pathlens` (api 5145, web 3001 via `WEB_PORT`, db 5432 PostGIS + storage, Caddy/Nginx → 443)

## Setup on Hetzner VPS (once)
```bash
ssh deploy@94.130.109.67
sudo apt update && sudo apt install -y docker.io docker-compose-plugin git
sudo usermod -aG docker $USER && newgrp docker
sudo mkdir -p /opt && sudo chown $USER:$USER /opt
git clone https://github.com/basithalam/PathLens-AI.git /opt/pathlens
cd /opt/pathlens
# Will be populated by deploy workflow .env — create placeholder for first run
cp .env.example .env
docker compose up --build -d
curl http://localhost:5145/health/live  # Healthy
curl http://localhost:3001/             # Next.js (WEB_PORT=3001, host shares 3000)
sudo ufw allow 22,80,443,3000,5145/tcp
```

## GitHub Secrets (Settings → Secrets → Actions)
```
HETZNER_HOST=94.130.109.67
HETZNER_USER=deploy
HETZNER_SSH_KEY=<private key for deploy user, cat ~/.ssh/id_rsa>
HETZNER_SSH_PORT=22
POSTGRES_PASSWORD=<strong>
JWT_SIGNING_KEY=<32+ char, fail-closed in Production>
JWT_ISSUER=PathLens
JWT_AUDIENCE=PathLensWeb
FRONTEND_ORIGIN=https://pathlens.cognitivebd.com
NEXT_PUBLIC_API_BASE_URL=https://pathlens.cognitivebd.com
OSRM_BASE_URL=https://router.project-osrm.org
AI_PROVIDER=Meta  # G1: live — set META_AI_ENDPOINT/MODEL/API_KEY secrets for real Meta AI (Groq gsk_... or Llama API LLM_...); Mock only for tests
META_AI_ENDPOINT=https://api.groq.com/openai/v1/chat/completions
META_AI_MODEL=meta-llama/llama-4-maverick-17b-128e-instruct
META_AI_API_KEY=gsk_...  # GitHub Secret META_AI_API_KEY — never committed, see docs/SECURITY.md
DATUMFEED_API_KEY=
CAMERAS_POLLING_ENABLED=false
```

## How CD works
`push main` → `.github/workflows/deploy-hetzner.yml` `verify` (build) → `deploy` via `appleboy/ssh-action` → `ssh deploy@94.130.109.67` → `git pull` → write `.env` from secrets (`AI_PROVIDER=Meta` + `META_AI_*` + `WEB_PORT=3001`) → `docker compose up --build -d` → `curl /health/live` verify → live at `https://pathlens.cognitivebd.com` (Caddy/Nginx reverse proxy → web 3001 + api 5145) + `https://pathlens.cognitivebd.com/health/live`. Worker profile: `docker compose --profile worker up -d` (not both per DB).

## DNS
`pathlens.cognitivebd.com` → `A 94.130.109.67` (Cloudflare/Hetzner DNS). Caddy/Nginx: `pathlens.cognitivebd.com { reverse_proxy web:3001; handle /api/* { reverse_proxy api:8080 } handle /health/* { reverse_proxy api:8080 } }` + auto TLS — see `nginx-pathlens.conf`.

## Vercel alternative
Frontend can stay on Vercel `pathlens-ai.vercel.app` with `NEXT_PUBLIC_API_BASE_URL=http://94.130.109.67:5145` (CORS allows `FRONTEND_ORIGIN`), but Hetzner single-VPS is cheaper and has local PostGIS.

## Next
Give `HETZNER_SSH_KEY` + `POSTGRES_PASSWORD` + `JWT_SIGNING_KEY` secrets → next `push main` auto live.
