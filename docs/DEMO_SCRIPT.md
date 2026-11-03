# Demo Script — PathLens AI (3 min — Priority 5: 6 Steps for Judges)

**Judges: This 3-minute story proves Wheelchair impact — follow 6 steps, see Evidence-aware ranking, Unknown handling, and Human Review.**

**Step 1 — 0:00-0:30 Wheelchair User Uncertain:** Show hero `Can this path be used right now — and how sure are we?` + Sylhet wheelchair user story (or SF — user stuck at crossing, Map shows road but ramp blocked?). VO: Problem — *Is sidewalk blocked? Is old obstacle still there? How old is info? Another route better? Not enough info?* Ordinary maps cannot confirm — PathLens makes gap visible.

**Step 2 — 0:30-1:00 Evidence Collection + AI Analysis (Live):** Upload Sylhet `1663695741.jpg` (248KB, 24.91,91.84) → `POST /api/reports` → `POST /api/evidence` (EXIF stripped) → `POST /analyze` → `muse-spark-1.3 @ https://api.meta.ai/v1/chat/completions` → **Live `BlockedSidewalk 90% High` + 4 visibleEvidence + 3 limitations** (not Mock). Show `EvidenceSection` confidence bar `90% High — still needs human check` + `✓ Live Meta AI (muse-spark-1.3)` + yellow `Human review required`. *If AI not sure → Unknown + low confidence + limitations.*

**Step 3 — 1:00-1:30 AI Admits Unknown:** Show 2nd image `Market Street Unknown` → AI `Unknown 0% Low — Mock analysis received... No real image understanding` (labelled `Demo/mock — not real AI`) → VO: When AI cannot be sure, it says Unknown — never fabricates — confidence red Low, limitations honest.

**Step 4 — 1:30-2:10 Evidence Comparison (Change Over Time):** Same report → 2 photos (earlier + later) → `POST /api/comparisons` → `LikelyStillPresent` (scooters still there) / `NotObservedInNewImage` (not seen, **not proves removed**) / `ConflictingEvidence` / `Inconclusive` (4 states, location ≤25m strong, timestamp trust, 8 unit tests). Show Timeline → Comparison card → Explanation: *What changed, what did not, what cannot be known.*

**Step 5 — 2:10-2:40 Route Planner — Evidence Gaps + Alternatives (Sylhet):** Origin 24.9116,91.8498 → Dest 24.9248,91.8698 → `Plan routes` → 2-3 OSRM foot routes (real geometry, `walking profile; not validated wheelchair` honest) → **Ranked by fewest known obstacles, not distance** → `Route 1 ⚠ Blocked — alternative below` (1 BlockedSidewalk, red) vs `Route 2 ✓ Recommended` (0 known, green) vs `Route 3 ❓ Mostly Unknown 62%` → VO: `UnknownLengthPercent` (25m sampling, no report within 100m) → Unknown ≠ clear — `hasConfirmedStairs` warning + `Stale 30d` + `Certain (HumanVerified) vs Uncertain (camera)` — Preferences `Avoid stairs ON` flips ranking, never hides obstacle. *Evidence gap + known obstacle explained per route.*

**Step 6 — 2:40-3:00 Human Reviewer Verifies (Audit):** `/reviewer` → Login `reviewer@pathlens.local` → Queue `Unverified + Conflicting + Stale camera` → `HumanVerified/Disputed/Rejected` + Note → Audit trail (`/reviewer/audit` actor/prev/new/note/time) → Map marker `V` — VO: Human decides, audit remembers — AI never auto-verifies.

**Backup & Live Proof (G1,G3,G5):** `GET /health/ready` → `database Healthy, ai-provider Healthy Meta muse-spark-1.3` + `GET /api/reports` → `analysisProvider: Meta, confidence 90%` — not Mock. `G3 confidence bar + callout + 429 5s backoff` + CI 92 tests + `JUDGE_GUIDE.md` 1 page.

**Old 2-min script archived — new 6-step 3-min is Judge-facing.**



**110–120s Honest + Impact (F6+G5+G6):** F6 QuickFeedback 4 buttons (StillVisible/NotInNewPhoto/WrongOrStale/NotSure — never auto-verifies) + camera `isStale` on `/map`. **G5:** CI `postgres:17-3.5` service + 92 tests (39 unit + 53 integration via `Ai__Provider=Mock`) + strict `/health/ready` must Healthy (or deploy fails). **G6:** `git grep` for key shapes now returns only masked forms and `${{ secrets.* }}` references — **but a
full Llama key was printed inside `docs/SECURITY.md` and is reachable in tracked commits `771a485`/`2386b7f`**;
it is masked in the working tree and must be treated as revoked (`docs/SECURITY.md` G1). History was not
rewritten. `evaluation/results.sf.json`: 25 synthetic images, 6 correct / 19 inconclusive under **Mock**,
5 pairs of which 1 correct — pipeline proof, not accuracy.

**Backup & Polish (H1→G6):**
- H1: `marker-*.png` 404 fixed, pagination preserves `search+type`, `docker compose up --build` one-click (api+web+db+worker profile + `scripts/seed-sf.ps1` 7 markers), Release `0W0E`, `.env.example` documented.
- H2→G1: `AI_GATE_A_REPORT.md` 404/Groq 401 honest → now Groq `https://api.groq.com/openai/v1/chat/completions` + Llama `https://api.llama.com/v1` validated, deploy default `Meta`, `SECURITY.md` rotation.
- G2: `evaluation/sf-dataset` 25×1024x768 synthetic + `manifest.sf.csv` 5 pairs (LikelyStillPresent×2/NotObserved/Conflicting/Inconclusive) + `results.sf.json` Mock pipeline proof.
- G3: confidence bar `<40 red/40-70 orange/>70 green` + yellow warn callout + 429 5s backoff.
- G4: preference chips under map + honest foot profile + Valhalla future.
- G5: CI postgres + integration, nginx 3001 via WEB_PORT, strict ready Healthy.
- G6: secrets grep no leak log, 2-min script above. Pre-recorded 1080p + localhost offline (`pgdata 5433` + `docker compose up` fallback) if live fails. Live URL `https://pathlens.cognitivebd.com` via Hetzner 94.130.109.67 + `nginx-pathlens.conf` 3001/5145.
