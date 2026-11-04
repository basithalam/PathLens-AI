# AI Evaluation — Real SF Sidewalks (Meta `muse-spark-1.3`)

**Status: complete, 10 images, run 2026-11-01 against the live deployment.**
Supersedes the earlier "running / results pending" state of this file, which was written before the run
finished and reported `totalCount 7` when the deployment now serves 13 reports.

**Dataset:** `evaluation/sf-real-eval/` — the 2026-11-01 run used **10** real SF footpath photos; the set now
**ships 9**, because `sf-eval-09.jpg` was deleted on 2026-11-04 when its source could not be recovered (see
`docs/THIRD_PARTY.md` §6). `evaluation/manifest.sf-real-eval.csv` lists the nine shipped images, each with
`sourcePage` / `author` / `license`. Ground truth was recorded **before** any model output. Runner:
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

That is the honest state of a 10-image set: the model disagrees with our labels three times, **the ground
truth is wrong at least three times**, and neither is quietly corrected to match the other.

## Re-scored against the photograph, not the pre-written label

The 3/3/2/2 headline compares the model with **our own labels** — and three of those labels do not survive
looking at the image. Re-reading every photograph by hand gives a different, more accurate picture.
`results.sf-real-eval.json` is left exactly as the run produced it; this table sits beside it, not on top of
it.

| id | pre-written label | model said | what the photograph actually shows | on re-reading |
|---|---|---|---|---|
| 01 | BlockedSidewalk (planters) | StairsOrStep 0.70 | clear steep hillside walkway, nothing on the line | **label wrong and model wrong** — recorded Disputed |
| 02 | BlockedSidewalk (scooters) | Unknown 0.30 | close-up of tactile paving at Stockton St, no obstruction | label wrong; **abstention was the right answer** |
| 03 | Other | Unknown 0.15 | a planted garden strip — not a footpath at all | label meaningless; **abstention was the right answer** |
| 04 | BrokenSurface | BrokenSurface 0.65 | broken curb seam with a void, ribbed joint across the path | **model right** |
| 05 | BlockedSidewalk | BlockedSidewalk 0.90 | three e-scooters lying across the walkway | **model right** |
| 06 | Unknown | Other 0.30 | Castro St sidewalk | no ground truth exists; scored "incorrect" by **a defect in our own rule** |
| 07 | BlockedSidewalk | BlockedSidewalk 0.60 | Market St, obstruction across the walking line | **model right** |
| 08 | Unknown ("clear") | BlockedSidewalk 0.60 | café table, two chairs and a walking cane on the path | label wrong; **model right** |
| 09 | Unknown | — | never analysed | our own upload rate limit (`429`) |
| 10 | Unknown | — | never analysed | our own upload rate limit (`429`) |

**Of the eight photographs that reached the model, six produced an answer that still stands when the
photograph is re-read** — four named the obstacle correctly, and two abstained on images where abstaining was
the right call. One did not stand (`sf-eval-01`). One cannot be judged either way (`sf-eval-06`, no ground
truth to compare against). Two never reached the model because of our own rate limiter.

What this is **not**: a claim that the model is accurate, or that every image was answered correctly. It is
ten images, one city (San Francisco evaluation set — PathLens itself is open globally), and a re-scoring that depends on our own reading of each
frame — the same reading that found three of our own labels wrong. The machine record still says 3 correct /
3 incorrect / 2 inconclusive / 2 errors, and both numbers have to be quoted together to be honest.

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

## Dataset provenance, and what is still open

- **Attribution — resolved 2026-11-04, nine credited and one file deleted.** The images are San Francisco
  street photos from Wikimedia Commons. The credit is not stored inside the files (every `Copyright` string in
  them is colour-profile boilerplate), but all of them are exactly 960 px wide — Commons' standard thumbnail
  width — so each was identified by downloading candidate 960px thumbnails and comparing **SHA-256**: nine of
  the ten matched byte-for-byte. Source page, author and licence are recorded in `docs/THIRD_PARTY.md` §6 and
  in `manifest.sf-real-eval.csv` (`sourcePage` / `author` / `license`). **The licences are not uniform** — CC
  BY 4.0, CC BY-SA 4.0, CC0 ×2 and CC BY 2.0 ×4 — so the earlier blanket "all CC BY-SA" description was wrong.
  **`sf-eval-09.jpg` did not match anything** in the pool searched, so the file has been removed from the
  repository and from the manifest rather than shipped uncredited; its report stays on the demo as the
  rejection path, with no image behind it. `results.sf-real-eval.json` is left as the run produced it.
- **No real comparison pairs.** 0 of the 10 images form a same-place old/new pair, so the comparison engine
  has no evidence from real photography — only the 5 synthetic Mock pairs in `manifest.sf.csv`.
- **The 25-image SF set is synthetic** (`evaluation/sf-dataset/`, own-generated placeholders) and its
  `results.sf.json` run used the **Mock** provider: 6 correct / 19 inconclusive. Pipeline proof, not accuracy.

**Limits, stated:** 10 images is a demo. Traffic cameras (Datumfeed / Caltrans US-101 at Octavia St) show a
freeway, not a footpath, and PathLens says so instead of scoring them as path evidence. The `foot` routing
profile is not a validated wheelchair profile.
