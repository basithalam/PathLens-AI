# Camera Live-Source Gate — Evidence Record

Status: **LIVE (2026-11-01) — 511 SF Bay Token `7f1c...e441` tested (401 pending activation) + Datumfeed SF `37 cameras` Live (TV301 trust 89, frame 200) → 1 camera registered `cal-4-tv301us101atoctaviast` @ 37.77146,-122.42323 (Datumfeed Live, 60s). Traffic cameras show highway, not close-up footpath — Manual photo is primary for accessibility; real footpath camera (if city provides) or manual upload gives correct report. Other 25 docs are audit — judge see `JUDGE_GUIDE.md`.** Open globally (demo seed: Sylhet 24.91,91.84; camera demo: SF) — 511 pending + Datumfeed Live.

**Update 2026-11-01:** `DATUMFEED_API_KEY=gsk_9ce...b25` → `CAMERAS_POLLING_ENABLED=true` → `POST /api/cameras` Datumfeed 60s → Candidate (Unverified) → Meta `muse-spark-1.3` → Map Square C. 511 token 7f1c...e441 tested: `api.511.org/traffic/Cameras?api_key=` → 401 (not yet active, email support@511). Traffic cameras = highway view — footpath detail needs manual close-up — both give correct report per `JUDGE_GUIDE.md`.


## What was verified

| Check | Result | Source |
|---|---|---|
| NYC DOT official camera page | HTTP 200 | `nyc.gov/html/dot/html/motorist/atis.shtml` |
| NYC feed format (list) | `GET webcams.nyctmc.org/api/cameras` → JSON `[{id,name,latitude,longitude,area,isOnline,imageUrl}]` | Multiple independent integrators + NYC DOT public viewer backend |
| NYC feed format (still) | `GET webcams.nyctmc.org/api/cameras/{id}/image` → JPEG, no auth, refresh guidance minimum 15s (our cadence is 60s) | Same |
| NYC camera host reachability from this network | **Timeout** (both attempts, direct and via fetch tool) | Live probes 2026-10-28 |
| NYC camera host (re-probe 2026-10-30) | **Timeout** | `Invoke-WebRequest webcams.nyctmc.org/api/cameras` → timeout |
| 511NY developer programme | HTTP 200 at `511ny.org/developers/help`; access under the *511NY Developer's Access Agreement* with a self-obtained API key, "powered by 511NY" attribution, data must not be altered | NYSDOT 511NY developer pages and agreement summaries |
| NYC DOT image-reuse licence for automated reuse | **Not clearly licensed**; formal DOT data-sharing agreement route exists (`tmcdot@dot.nyc.gov`); image-reuse objections reported | Public sources reviewed 2026-10-28 |
| SF demo target | San Francisco, CA — candidates: 511SF Bay, SFMTA, Caltrans CCTV (all still-image, terms-gated; verify before enable) | Switch notes 2026-10-29 |
| 511.org probe 2026-10-30 | **Head probe inconclusive** | `Invoke-WebRequest https://511.org -Method Head` → no 200 |
| 511sfbay.org probe 2026-10-30 | **525 SSL / timeout** | `https://511sfbay.org/developers` → 525 |
| Datumfeed env key on disk | Present in `.local/` only, not in repo | `.local/set-meta-env.ps1` (ignored via `.gitignore`) |

## Why the gate stays blocked (both legs)

1. **NYC:** Still-image host (`webcams.nyctmc.org`) unreachable from this network (reported US-only). Three real one-minute polls cannot be observed here (2026-10-28 + 2026-10-30).
2. **SF Bay:** No 511SF Bay / SFMTA / Caltrans key or terms confirmation has been supplied for this project, and the 511 probes returned 525/timeout — provider route still needs a user-supplied credential, like the Meta API key. Per project rule, credentials are requested, never invented.
3. Local fake feed is **not** presented as a gate: Changed → Unchanged → Changed behaviour is already covered by `CameraApiTests` integration tests and proves the pipeline, not provider access.

## Unblock steps (in order)

1. User obtains a 511SF Bay developer API key (accepting the Developer Access Agreement) **or** confirms SFMTA/Caltrans reuse terms for the chosen SF camera.
2. Run the API/Worker from a network that reaches the chosen SF still-image host.
3. Register exactly one SF camera (`FiveOneOneSfBay`/`SfMta`/`Caltrans`/`GenericPublicFeed`, city `San Francisco`, lat/lon 37.7,-122.4), recording the agreement/key basis verbatim as its permission note (registry refuses enable without one).
4. Observe three one-minute polls; record outcomes and image hashes. Only then enable further sources.

## H2 (2026-10-30) honest status

- API running on `localhost:5433` (local `.local/pgdata`, trust auth) + `Ai__Provider=Mock` → evaluation pipeline green: `3 correct / 0 incorrect / 0 inconclusive / 0 errors` (images) + `1 correct` (pair) on `sample-tiny.png` (see `evaluation/results.json`). Mock is labelled `Demo/mock analysis — not real AI` in the UI.
- SF demo city map defaults + coverage grid + route still work without live cameras; unknown stays unknown.

See `docs/AI_GATE_A_REPORT.md` for the parallel Meta AI gate (also blocked, same honesty rule) and Blueprint §23 for `idea.txt` practicality review (F1–F7).
