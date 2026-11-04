# PathLens AI — Judge Guide (3-Minute Demo)

**Start here. This one page is enough to judge. The other 32 files in `docs/` are audit trail and backup —
including `WHEELCHAIR_USER_GUIDE.md`, the click-by-click manual for how a wheelchair user gives feedback,
`ERROR_ANALYSIS.md`, which explains every failure in the evaluation instead of only counting it, and
`THIRD_PARTY.md`, the licence inventory for everything we did not write.**

**Live Demo:** https://pathlens.cognitivebd.com · **Health:** https://pathlens.cognitivebd.com/health/ready
**Screens:** `/map` · `/report/new` · `/routes` · `/reports` · `/reviewer` · `/admin`

## What PathLens does (one line)

Helps a wheelchair user or pedestrian judge whether a footpath is *likely* usable **before they travel** — from
photo evidence compared over time, with the age, source and verification of every claim shown, and with
**unknown stated as unknown** instead of a fake "safe".

## Pilot city, and where the evidence comes from

| | |
|---|---|
| **Pilot city** | **Sylhet, Bangladesh** (24.9116, 91.8498) — the map opens here |
| **Manual photo evidence** | Works in any city; the judge can test it in 30 seconds |
| **Automatic camera feeds** | Only **San Francisco** public feeds exist today (Datumfeed / Caltrans / 511SF / SFMTA). **Sylhet has no public footpath camera** — the map says so instead of pretending otherwise |
| **AI** | Two Meta models, each doing one job: **`muse-spark-1.3`** classifies the photo (`POST /v1/chat/completions`) and **`sam-3.1`** measures how much of the walkway the obstacle occupies (`POST /v1/responses`) |

## Staff logins — one tap, nothing to ask for

The demo is **deliberately open**. Both consoles show all three seeded accounts with the password printed on
the card and a **Sign in** button on each — one tap, no typing, no need to message the author.

| Account | Email | Password | What it unlocks |
|---|---|---|---|
| **Reviewer** | `reviewer@pathlens.local` | `Reviewer@123` | the queue, verify / dispute / reject, audit trail |
| **Admin** | `admin@pathlens.local` | `Admin@123` | reviewer rights **plus** camera registry, pilot analytics, reputation |
| **User** | `user@pathlens.local` | `User@123` | signed-in reporter — review endpoints answer **403** (try it, it is the point) |

Sign in at `/reviewer` or `/admin`. These are seeded hackathon accounts on a demo dataset, not real users; a
real deployment rotates them through `Auth:Seed*` (`docs/SECURITY.md`), which is intentionally **not** enabled
here so the review console stays reachable.

## Measured state — 2026-11-03

```
GET /health/ready → Healthy
  database    Healthy   PostgreSQL is reachable.
  osrm        Healthy   OSRM reachable (status 400 on the probe pair; real routes return 201)
  ai-provider Healthy   Meta muse-spark-1.3 @ https://api.meta.ai/v1/chat/completions (key present)
```

| What | Live now |
|---|---|
| Reports | 13 total — **3 HumanVerified, 1 Disputed, 2 Rejected, 7 Unverified** |
| Audit events | 16 (`VerificationChanged ×8`, `ComparisonReviewed ×2`, `Feedback ×5`, `DuplicateConfirmed ×1`) |
| Cameras | 4 registered, 2 enabled, **1 flagged stale** |
| Unknown share of the map | **67.5%** — stated as unknown, never as clear |
| Analysis provider on evidence | `Meta` / `muse-spark-1.3`, not `mock` |

**About the reviewer decisions:** they were recorded through the seeded `reviewer@pathlens.local` account for
this hackathon, by reading the uploaded photo of each report. Every note says so, and the audit trail shows who
decided what. They are **not** a municipal field inspection, and PathLens never treats an AI answer as one
either.

## 3-minute demo flow

**0:00–0:30 — the problem.** A wheelchair user reaches a crossing, the footpath is blocked by parked
motorbikes, they must turn back. A normal map showed the road, not the blockage.

**0:30–1:30 — `/map`.** Opens on Sylhet. Marker letters carry meaning without colour:
`!` unverified · `V` human-verified · `?` disputed · `X` rejected. Square `C` markers are cameras, kept
visually separate from human reports. Click a marker → the list and the map stay in sync → open a report.
Turn on **Coverage layer**: cells marked `C` (hatched) mean *nobody has reported here* — explicitly not
"clear". Click a cell to zoom it.

**1:30–2:30 — `/report/new` → `/reports/{id}`.** Upload a footpath photo (consent required, EXIF GPS stripped)
→ **Run analysis** → `✓ Live Meta AI`, obstacle type, confidence bar with the 40%/70% thresholds, the evidence
the model says it saw, its **limitations**, and `Human review required`. Upload a second photo → **Compare
photos** → one of `LikelyStillPresent` / `NotObservedInNewImage` / `ConflictingEvidence` / `Inconclusive`, each
with an explanation. *Not observed* never becomes *removed*.

**2:30–3:00 — `/routes`.** Two Sylhet points (24.9116, 91.8498 → 24.9248, 91.8698) → **Plan routes** → real
OSRM geometry, ranked by fewest known obstacles, with the **unknown share of each route** drawn as a hatched
bar. Preference chips reorder; they never hide an obstacle. The profile is `foot`, and the page says plainly
that this is **not a validated wheelchair profile**.

## The 20-second honesty moment (worth showing)

Open `/reports/9ef305d4-48eb-4e77-af6e-db57b437b4ae` — the live **CameraDetected candidate** from the
US-101 at Octavia St feed, which has 482 camera frames and 519 comparisons, all `Inconclusive`.

A reviewer read two of those frames (02:59:08 and 03:04:08 PST, five minutes apart) and recorded a review:
they are 320×260 night captures of a freeway sign and car tail-lights — **no footpath is in frame at all**, so
neither image can establish or refute anything about a walking surface. The engine's `Inconclusive` was
confirmed, not overwritten, and the note says what the camera can and cannot prove.

That is the whole thesis on one screen: automatic evidence that cannot decide stays labelled as unable to
decide, and a human saying so does not erase the machine's record.

A second finding, reported rather than hidden: two comparisons on the demo came back `ConflictingEvidence`
and were reviewed to `Inconclusive` because the "earlier" and "newer" photos were **the same file** (identical
SHA-256, only the claimed capture times differed) — model variance on identical input, not a changed place.
Those two reviews are in the audit trail at `/reviewer` (`ComparisonReviewed ×2`), but the reports they were
attached to have since been deleted, so they no longer open from a report page.


## Verify in 30 seconds

1. `curl https://pathlens.cognitivebd.com/health/ready` → all three checks `Healthy`
2. `curl "https://pathlens.cognitivebd.com/api/reports?page=1&pageSize=5"` → reports with `HumanVerified` present
3. Open any report with a photo → evidence shows `✓ Live Meta AI` with provider `Meta`, not `Demo/mock`
4. `/reviewer` → sign in with the **Reviewer** demo card → queue, decisions, audit trail
5. `/admin` → sign in with the **Admin** card → camera registry, pilot analytics, reputation

## What is NOT proven (our own limits, stated)

- **Real-image accuracy is small and mixed.** 10 real SF footpath photos through `muse-spark-1.3`:
  **3 correct, 3 incorrect, 2 inconclusive, 2 upload errors, 0 usable pairs.** 10 images is a demo, not
  accuracy. See `evaluation/results.sf-real-eval.json`.
- **The 25-image SF dataset is synthetic** (own-generated placeholders, no faces or plates) and its
  comparison pairs were run under the **Mock** provider — pipeline proof, not accuracy proof.
- **The ground truth of that 10-image set is worse than the model's answer.** Re-reading every photo by hand,
  three pre-written labels do not survive: `sf-eval-01` is labelled `BlockedSidewalk (planters)` but the frame
  is a clear, steep walkway (marked **Disputed**); `sf-eval-02` is labelled "scooters on sidewalk" but is a
  close-up of tactile paving with no scooter in frame; `sf-eval-08` is labelled "clear" while a café table,
  chairs and a cane sit on the path. `sf-eval-03` is not a footpath view at all — it is a planted garden strip.
  We are reporting the label problem rather than hiding it, and we did not flip any label to agree with the
  model. Full detail in `docs/AI_EVALUATION_SF_REAL.md`, and the *why* behind every failure — including the
  scoring rule that manufactured one "incorrect" and the fact that re-running one photo changed the model's
  answer — in `docs/ERROR_ANALYSIS.md`.
- **The seeded evaluation rows are visible on the live demo and labelled as such.** The ten reports the
  evaluation run created are titled `Demo evidence 01…10` and each description says plainly that the photo was
  borrowed from Wikimedia Commons (CC BY-SA, per-file credit still owed) and is **not** a community submission.
  Two of them have no photo at all and are marked **Rejected** — that is the rejection path on screen, not an
  accident.
- **No real footpath camera exists for Sylhet yet**, so the automatic-update story runs on SF highway cameras,
  which show traffic, not footpaths.
- **Reporter reputation is empty** on the live demo because the seeded reports were created anonymously; the
  panel shows an honest empty state instead of fake badges.
- **One API key was committed in plaintext** in `docs/SECURITY.md` and remains reachable in git history
  (`771a485`, `2386b7f`). It is masked in the working tree and must be treated as revoked — see
  `docs/SECURITY.md` G1. History was not rewritten.
- **A second, smaller exposure was found on 2026-11-03 while writing the licence inventory**:
  `docs/DATUMFEED_LIVE.md` quoted six characters of a camera-aggregator key (`head...tail`) on a line that
  claimed the key was "never committed". We verified the fragment matches the real key, masked it, and
  recorded it in `docs/SECURITY.md` §"Second partial exposure". It is not the AI provider's key and polling
  is off, so nothing is at immediate risk — but the honest lesson is that our own secrets grep (which
  demands 20+ characters after the prefix) cannot see a partial key, and we had been quoting that grep as
  proof.
- **ImageSharp is pinned to 3.1.8** with `NU1902/NU1903` advisories suppressed for the hackathon
  (`PathLens.Infrastructure.csproj`); the upgrade path is noted in `docs/H1_FIX_REPORT.md`.
- **License is MIT** (`LICENSE`), covering code and docs. It does not relicense the ten borrowed Wikimedia
  Commons street photos, which stay **CC BY-SA** — their per-file attribution is still owed, and that is the
  one legal item left open. **`docs/THIRD_PARTY.md` is the full inventory**: every dependency with its
  version and licence, the Leaflet BSD-2 notice reproduced as that licence requires, the OpenStreetMap /
  Nominatim / OSRM / Meta service terms, the camera-feed terms we have *not* verified, and the ten-row
  attribution ledger with the columns to fill in. It also records why the credit cannot be recovered from
  the image files (their only `Copyright` strings are Adobe/Apple colour-profile boilerplate), so the gap
  has a named owner and a shape, not just an apology.
- **The SAM 3.1 obstruction share is measured, not evaluated.** End-to-end on one real photo (three fallen
  e-scooters across a walkway): `muse-spark-1.3` returned `BlockedSidewalk 0.85`, `sam-3.1` returned 2
  obstacle boxes over 1 walkway box, and the computed share was **23% of the visible walkway** — verified by
  hand against the box coordinates. The code default is **off** (`Sam31:Enabled=false`) because it costs two
  extra billed calls per photo; on this live demo it is switched on through the `SAM31_ENABLED` deployment
  secret, and **one row has been measured so far** — `Demo evidence 01`, whose walkway is clear, came back at
  **1% occupied**, which is the negative control agreeing with the photo. It has not been run across the
  evaluation set, so there is **no accuracy figure for it**, and measurement only happens during analysis:
  the 581 evidence rows analysed before it was switched on still report *not measured* rather than a number.
  Two limits are stated in the UI itself: the share is of the walkway **in the photo**, never metres (nothing
  gives a scale reference), and SAM returns **no confidence score**.
- **Occupancy is measured only where the number means something.** On 2026-11-04 a Sylhet photo whose
  walking line is clear, with an open drain along its edge, came back as **100% occupied** — the model had
  classified it `BrokenSurface`, and the surface concept segmented the whole damaged pavement. The
  arithmetic was correct; the metric was not. Broken paving degrades a walkway, it does not occupy it, so
  `ObstacleConcepts.MeasuresOccupancy` now restricts measurement to obstacles that physically stand on the
  path (`BlockedSidewalk`, `ConstructionBarrier`, `Other`) and returns no measurement for surface defects,
  which the UI shows as unknown. Three unit tests pin that, including the one asserting a type can have a
  valid segmentation concept and still be unmeasurable. The full account is `docs/ERROR_ANALYSIS.md` §9.
  **The limit this leaves:** PathLens still cannot put a number on the commonest hazard in Sylhet. An open
  drain is recorded as a report and shown on the map, but it has no occupancy percentage — the type
  vocabulary has no `OpenDrain` value, and area-share is the wrong instrument for a trench anyway.
- **A zero-detection is never shown as "0% blocked".** Measured on that same photo: the bare concept
  `scooter` returned **no boxes at all**, while `scooters, bikes, goods or vehicles blocking the sidewalk`
  returned two. So an empty result means *not measured*, and the code returns `null` for it — reporting 0%
  would tell a wheelchair user the path is clear, which is the one claim this product exists to avoid.
- **The staff logins on this demo are the published defaults.** `reviewer@pathlens.local / Reviewer@123` and
  `admin@pathlens.local / Admin@123` work here today — deliberately, so you can open the review console
  without requesting access. The rotation control exists and is tested (`docs/SECURITY.md`), but it engages
  only once the deployment secrets are set. Treat this as an open hackathon demo, not a secured production
  system.

## Tech (short)

Next.js 15 App Router + TypeScript · ASP.NET Core 10 modular monolith (Domain/Application/Infrastructure/
API/Worker) · PostgreSQL + EF Core, dual-mode PostGIS with a lat/lon fallback · OpenStreetMap + Leaflet ·
OSRM foot routing · Meta AI behind a provider abstraction (Mock is labelled as such, never presented as AI) ·
private image storage served only through the API · JWT reviewer auth with an append-only audit trail.

`docker compose up --build` then `./scripts/seed-sf.ps1` for a populated map. Migrations apply at API startup
(`Program.cs:124`).

**Footer:** Unknown is unknown. PathLens never labels a route safe just because no obstacle was reported.
