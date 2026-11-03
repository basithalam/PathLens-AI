# Datumfeed SF camera — dated record from 2026-11-01

**This file describes an experiment on 2026-11-01, not the current deployment. The corrections at the
bottom outrank everything above them.**

- Camera: SF US-101 At Octavia St (cal-4-tv301) — reached via the Datumfeed aggregator's Caltrans mirror
- Polling: 60 s cadence, `CAMERAS_POLLING_ENABLED=true` set for that experiment
- Key: `DATUMFEED_API_KEY` held as a GitHub Secret; the value lives in `.local/datumfeed-key.txt` (gitignored)
- Expected chain: polls ? Candidate Report ? Meta `muse-spark-1.3` ? map marker drawn as a square `C`

## Corrections as of 2026-11-03 (measured, not remembered)

- **The secret fragment that used to be quoted in this file is removed.** The line read
  `GitHub Secret gsk_<3 chars>...<3 chars>`, and those six characters match the real 36-character key in
  `.local/datumfeed-key.txt` exactly — verified by comparison, not assumed. A tracked public file therefore
  carried a partial credential while claiming "never committed". It is masked now; see
  `docs/SECURITY.md` §"Second partial exposure".
- **There are 4 camera rows on the live deployment, not 37.** Measured 2026-11-03 with
  `GET https://pathlens.cognitivebd.com/api/cameras` ? 2 Datumfeed, 1 Caltrans direct, 1 placeholder. The
  "37 cameras" figure from the import experiment is not present in the live database.
- **Polling is off.** `deploy-hetzner.yml` writes `CAMERAS_POLLING_ENABLED=${{ secrets.CAMERAS_POLLING_ENABLED
  || 'false' }}`, so the deployed default is disabled and no frames are being pulled.
- **The cadence was never 15 s.** The original file said 60 s in one line and "polling every 15s check" in
  another; the configured value is the cadence per camera row, and the two numbers contradicted each other.
- **What the frames actually show** is recorded honestly elsewhere: the reachable Caltrans camera produced a
  report whose comparison is `Inconclusive` because both frames are 320×260 night highway captures with no
  footpath in frame (`docs/AI_EVALUATION_SF_REAL.md`, `docs/JUDGE_GUIDE.md`). A freeway camera is not
  sidewalk evidence, and PathLens says so instead of scoring it as path data.
- Feed terms for both the aggregator and the Caltrans mirror are **unverified** — tracked in
  `docs/THIRD_PARTY.md` §7.
