# PathLens AI — Judge Guide (3-Minute Demo)

**Start here. 25 other docs are audit/backup — this one page is enough to judge.**

**Live Demo:** https://pathlens.cognitivebd.com | **API Health:** https://pathlens.cognitivebd.com/health/ready | **Map:** /map | **Report:** /report/new | **Route:** /routes

## What PathLens Does (One Line)
Helps wheelchair users know if a footpath is *likely* usable **before travel** — with photo-evidence age, source, verification, and **honest unknown** (no fake “safe”).

## Real Data Today (2026-11-01)
- **AI Live:** Meta `muse-spark-1.3` @ `https://api.meta.ai/v1/chat/completions` — `health/ready` → `ai-provider: Healthy`
- **Camera Live:** Datumfeed SF `TV301 US-101 At Octavia St` (Caltrans, 37 SF cameras, trust 89) — `POST /api/keys` → `DATUMFEED_API_KEY` → 60s polling → Candidate Report (Unverified) → Map Square C
- **Manual Live:** Sylhet `24.91,91.84` — `Test` report → `BlockedSidewalk 90%` (Meta) — manual close-up footpath is **primary** for accessibility; traffic cameras show highway, not footpath detail

**When real footpath camera exists → auto correct photo → report. Otherwise manual photo upload → correct info. Both work — judge can test either.**

## 3-Minute Demo Flow

**0:00-0:30 Problem (Story):** Wheelchair user reaches crossing, footpath blocked by scooters, must turn back — normal map showed road, not blockage.

**0:30-1:30 Map (`/map`):** Pan Sylhet (24.91,91.84) — markers `! V ? X` + Square C cameras — click marker → Report detail — `No reports ≠ clear` — Coverage grid `C/B/A` (C grey = insufficient)

**1:30-2:30 Report (`/report/new` → `/reports/{id}`):** Upload footpath photo (consent required) → `Run analysis` → **Live Meta** `BlockedSidewalk 90%` + 4 visibleEvidence + 3 limitations + `Human review required` → Upload 2nd photo → `Compare photos` → `LikelyStillPresent / NotObserved / Conflicting / Inconclusive` (NotObserved ≠ removed)

**2:30-3:00 Route (`/routes`):** Origin 24.9116,91.8498 → Dest 24.9248,91.8698 → `Plan routes` → 2-3 OSRM foot routes → Ranked by fewest known obstacles → `Route 1 ⚠ Blocked — alternative below` (red) vs `Route 2 ✓ Recommended` (green) → `Unknown 62%` + `Stale 30d` + `hasConfirmedStairs` warning → **Alternative suggestion when blocked** — Unknown stays unknown, AI confidence ≠ guarantee

## Live Camera vs Manual — Judge Note
- **Traffic cameras (Datumfeed/511 Caltrans):** Highway-facing, low-res — footpath not clear — **Expected** — they prove *change detection* (Changed/Unchanged) and auto Candidate, not footpath detail
- **Footpath cameras (if city provides) or Manual upload:** Close-up → Meta detects `BlockedSidewalk, BrokenSurface, Ramp` correctly → **Use manual for accessibility accuracy** — both paths give correct report, judge can test manual in 30s

## Verify in 30s
1. `curl https://pathlens.cognitivebd.com/health/ready` → `database Healthy, ai-provider Healthy: Meta muse-spark-1.3`
2. `curl https://pathlens.cognitivebd.com/api/reports?page=1&pageSize=1` → 1+ reports with `analysisProvider: Meta`
3. `/reports/{id}` → Evidence `✓ Live Meta AI` (not `Demo/mock`)

## Tech (Short)
Next.js 15 + ASP.NET Core 10 + PostGIS + EF Core + Leaflet OSM + OSRM + Meta AI (provider abstraction) + S3 object storage — modular monolith, `docker compose up --build` + `./scripts/seed-sf.ps1`

## Archive
Other 25 docs (`AI_GATE_A_REPORT.md`, `camera-live-gate.md`, `H1_FIX_REPORT.md` …) are audit trail — judge confusion avoided — start here. Full list in `docs/` but not needed for 3-min demo.

**Footer:** Unknown is unknown. PathLens never labels a route safe just because no obstacle was reported.

