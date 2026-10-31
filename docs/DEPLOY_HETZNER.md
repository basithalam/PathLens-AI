# Deploy to Hetzner — Live Demo (94.130.109.67)

**Host:** `94.130.109.67` | **User:** `deploy` | **Stack:** `docker compose` at `/opt/pathlens` (api 5145, web 3000, db 5432 PostGIS + storage)

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
curl http://localhost:3000/             # Next.js
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
FRONTEND_ORIGIN=http://94.130.109.67:3000
NEXT_PUBLIC_API_BASE_URL=http://94.130.109.67:5145
OSRM_BASE_URL=https://router.project-osrm.org
AI_PROVIDER=Mock
# Optional live:
META_AI_ENDPOINT=
META_AI_MODEL=
META_AI_API_KEY=
DATUMFEED_API_KEY=
CAMERAS_POLLING_ENABLED=false
```

## How CD works
`push main` → `.github/workflows/deploy-hetzner.yml` `verify` (build) → `deploy` via `appleboy/ssh-action` → `ssh deploy@94.130.109.67` → `git pull` → write `.env` from secrets → `docker compose up --build -d` → `curl /health/live` verify → live at `http://94.130.109.67:3000` (web) + `:5145/health/live` (api). Worker profile: `docker compose --profile worker up -d` (not both per DB).

## Vercel alternative
Frontend can stay on Vercel `pathlens-ai.vercel.app` with `NEXT_PUBLIC_API_BASE_URL=http://94.130.109.67:5145` (CORS allows `FRONTEND_ORIGIN`), but Hetzner single-VPS is cheaper and has local PostGIS.

## Next
Give `HETZNER_SSH_KEY` + `POSTGRES_PASSWORD` + `JWT_SIGNING_KEY` secrets → next `push main` auto live.
