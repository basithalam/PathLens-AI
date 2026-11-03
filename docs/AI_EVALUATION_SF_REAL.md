# AI Evaluation — Real SF Sidewalks (Meta `muse-spark-1.3`)

**Status: complete, 10 images, run 2026-11-01 against the live deployment.**
Supersedes the earlier "running / results pending" state of this file, which was written before the run
finished and reported `totalCount 7` when the deployment now serves 13 reports.

**Dataset:** `evaluation/sf-real-eval/` — 10 real SF footpath photos, `evaluation/manifest.sf-real-eval.csv`,
ground truth recorded **before** any model output. Runner:
`./evaluation/run-evaluation.ps1 -ManifestPath evaluation/manifest.sf-real-eval.csv -ApiBaseUrl https://pathlens.cognitivebd.com`

## Headline numbers (`evaluation/results.sf-real-eval.json`)

| | count |
|---|---|
| Images | 10 |
| **Correct** | **3** |
| **Incorrect** | **3** |
| **Inconclusive** (model said `Unknown`) | 2 |
| **Errors** (upload rejected) | 2 |
| Comparison pairs | **0** |

Provider on every analysed image: `Meta` / `muse-spark-1.3`. This is a 10-image demo, **not** a measurement of
general accuracy.

| id | expected | model said | conf | verdict |
|---|---|---|---|---|
| 01 | BlockedSidewalk | StairsOrStep | 0.70 | Incorrect |
| 02 | BlockedSidewalk | Unknown | 0.30 | Inconclusive |
| 03 | Other | Unknown | 0.15 | Inconclusive |
| 04 | BrokenSurface | BrokenSurface | 0.65 | Correct |
| 05 | BlockedSidewalk | BlockedSidewalk | 0.90 | Correct |
| 06 | Unknown | Other | 0.30 | Incorrect |
| 07 | BlockedSidewalk | BlockedSidewalk | 0.60 | Correct |
| 08 | Unknown | BlockedSidewalk | 0.60 | Incorrect |
| 09 | Unknown | — | — | Error |
| 10 | Unknown | — | — | Error |

**The two errors were our own rate limiter.** The API caps evidence uploads (H5: 10 per hour per client), so
rows 09 and 10 failed at upload; the runner reported it as "Upload did not return an evidence id" because
`curl -s` hid the HTTP 429. The runner now reads the status code and retries with backoff
(`-UploadSpacingSeconds`, `-Upload429Retries`).

## Three ground-truth labels that did not survive looking at the photo

Re-reading every image by hand while recording reviewer decisions:

- **`sf-eval-01`** is labelled `BlockedSidewalk — planters + narrow strip`. The frame shows a steep, **clear**
  hillside walkway: no planters, nothing on the walking line; the only step is at a doorway. The model's
  `StairsOrStep` is also unsupported. This report is recorded **Disputed** — label and model answer both fail
  against the photo.
- **`sf-eval-02`** is labelled `BlockedSidewalk — scooters/bikes on sidewalk`. The frame is a **close-up of
  tactile paving and a panel seam at Stockton St** — there is no obstruction in it, and no scooter anywhere.
  Here the model's abstention (`Unknown 0.30`) is the more defensible answer, so the report was left
  **Unverified**.
- **`sf-eval-08`** is labelled `Unknown` ("clear"). The frame shows a café table, two chairs and a walking cane
  **on the walkway**. The model said `BlockedSidewalk 0.60`. Here the model is the more defensible reading, so
  the report was left **Unverified** rather than flipped to agree with a label we no longer trust.

A fourth image, **`sf-eval-03`**, is not a footpath view at all — it is a planted garden strip with a small
wooden structure and a solar light. Its label `Other` is harmless, but no footpath claim can be made from it,
which is worth knowing before anyone quotes this set as a sidewalk benchmark.

That is the honest state of a 10-image set: the model is wrong three times, **the ground truth is wrong at
least three times**, and neither is quietly corrected to match the other.

## Reviewer decisions recorded on the live demo

| target | decision | basis |
|---|---|---|
| `af3fedab` (eval-05) | HumanVerified | three shared e-scooters lie across the walkway, one through the bike-rack hoop |
| `3a8690c2` (eval-04) | HumanVerified | curb seam broken out with a void at the top left, ribbed metal joint across the path |
| `1dd7d264` (Madina Market, Sylhet) | HumanVerified | motorcycles parked along a narrow footpath, rider passing through the middle |
| `3f1a230d` (eval-01) | Disputed | see above |
| `5222b63d`, `f9c3fbdc` (eval-09/10) | Rejected | no evidence exists at all — the failed uploads |
| comparison `a24fcd92` (camera candidate) | Inconclusive confirmed | both frames are 320×260 night highway captures; no footpath is in frame |

Every note is labelled as a hackathon demo review. `docs/JUDGE_GUIDE.md` states this too.

## Still open

- **Attribution.** The 10 images are San Francisco street photos sourced from Wikimedia Commons under
  **CC BY-SA**, but neither `manifest.sf-real-eval.csv` nor this file records the source page, author or
  licence notice per image. That credit is a licence condition and **must be added before submission**.
  The fill-in ledger is `docs/THIRD_PARTY.md` §6, which also records that the credit **cannot be recovered
  from the files themselves** — every `Copyright` string inside them is colour-profile boilerplate — so only
  the download history can supply it.
- **No real comparison pairs.** 0 of the 10 images form a same-place old/new pair, so the comparison engine
  has no evidence from real photography — only the 5 synthetic Mock pairs in `manifest.sf.csv`.
- **The 25-image SF set is synthetic** (`evaluation/sf-dataset/`, own-generated placeholders) and its
  `results.sf.json` run used the **Mock** provider: 6 correct / 19 inconclusive. Pipeline proof, not accuracy.

**Limits, stated:** 10 images is a demo. Traffic cameras (Datumfeed / Caltrans US-101 at Octavia St) show a
freeway, not a footpath, and PathLens says so instead of scoring them as path evidence. The `foot` routing
profile is not a validated wheelchair profile.
