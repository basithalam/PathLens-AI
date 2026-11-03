# Error Analysis — what we got wrong, and why

Companion to `docs/AI_EVALUATION_SF_REAL.md`. That file reports the numbers; this one explains the
mechanism behind each failure — including the failures in **our own labels, our own scoring rule and our own
test harness**. Written from `evaluation/results.sf-real-eval.json` (the record) and from re-reading each
photograph by hand.

Run: 2026-11-01 against the live deployment, 10 images, provider `Meta` / `muse-spark-1.3`.

| | count |
|---|---|
| Correct | 3 |
| Incorrect | 3 |
| Inconclusive (model abstained) | 2 |
| Errors (upload rejected) | 2 |
| Comparison pairs | **0** |

## 1. The three "incorrect" verdicts, one row each

| id | expected | model said | conf | what actually happened |
|---|---|---|---|---|
| `sf-real-eval-01` | BlockedSidewalk | StairsOrStep | 0.70 | **Both wrong.** The frame is a steep but clear hillside walkway — nothing on the walking line, one step at a doorway. Recorded **Disputed** |
| `sf-real-eval-06` | Unknown | Other | 0.30 | **Our scoring rule is the bug**, not the model. See §3 |
| `sf-real-eval-08` | Unknown | BlockedSidewalk | 0.60 | **The model was right and the label was wrong.** Café table, two chairs and a walking cane sit on the walkway. Report left **Unverified** |

Only one of the three is a clean model failure. That distinction is the reason this file exists: a headline
"30% wrong" would have been the more flattering number and the less accurate one.

### `sf-real-eval-01` also proved the model is not deterministic

On 2026-11-03, while verifying that SAM 3.1 measurement works on the live deployment, the analysis job for
this exact photo was re-run. Same file, same model, same prompt — the answer moved from
`StairsOrStep 0.70` to **`Other 0.65`**, and the model's own evidence list now includes *"sidewalk width
appears clear of objects, vehicles, or construction barriers"*.

Two consequences we accept rather than hide:

1. A single-run verdict on a 10-image set is **partly luck**. Repeated runs would move the 3/3/2 split.
2. The `results.sf-real-eval.json` record (dated 2026-11-01) no longer matches what the live demo returns
   for that photo. The record is left as the run happened; the live row now carries the newer answer plus a
   SAM 3.1 measurement of **1% of the visible walkway occupied** — a third signal, and the one that agrees
   with the reading we reached by eye.

We did not re-run the other nine images, so the comparison stays single-run for them.

## 2. The three ground-truth labels that collapsed

Labels were written **before** any model output — which is the right discipline and also exactly how this
mistake was made: they were written from the batch plan (filename plus intended obstacle) instead of from
the frame.

| id | label claimed | what the photo shows | how it was caught |
|---|---|---|---|
| `sf-eval-01` | BlockedSidewalk — "planters + narrow strip" | Clear steep hillside walkway; no planters; one step at a doorway | Reading the image while deciding a reviewer verdict |
| `sf-eval-02` | BlockedSidewalk — "scooters/bikes on sidewalk" | Close-up of tactile paving and a panel seam at Stockton St. No scooter anywhere in frame | Same |
| `sf-eval-08` | Unknown — "clear" | Café table, two chairs and a cane on the walkway | Same |
| `sf-eval-03` | Other | **Not a footpath at all** — a planted garden strip with a small wooden structure and a solar light | Same |

No label was quietly rewritten to agree with the model, and the model was not quietly rewritten to agree
with the label. `sf-eval-01` is `Disputed` precisely because *both* failed against the photo.

`sf-eval-03` matters beyond its own row: anyone quoting this set as a "sidewalk benchmark" should know one
of the ten images does not contain a sidewalk.

## 3. The scoring rule that manufactures an "incorrect"

`sf-real-eval-06` has expected `Unknown`. The runner compares expected label to actual label, so an image
with **no ground truth** can never be answered correctly by anything other than an abstention — and the
model named a class (`Other`, confidence 0.30), so it was scored Incorrect.

That is our metric, not the model's behaviour. The honest reading: a low-confidence concrete guess on an
unlabelled photo is *unverifiable*, not *wrong*. The rule we would write instead: when
`expectedObstacle == Unknown`, score `Unknown` as **abstention-correct**, a concrete label as
**unverifiable**, and never as incorrect — because we cannot claim what the truth was.

`sf-real-eval-08` is the mirror case with the same expected value, and there the concrete label was the
better answer. Counting both as "incorrect" flattens two opposite situations into one number.

## 4. The two errors were our own rate limiter

`sf-eval-09` and `sf-eval-10` never reached the model. The API caps evidence uploads at **10 per hour per
client** (H5), so rows 9 and 10 were rejected with `429` — and the runner reported
*"Upload did not return an evidence id"* because `curl -s` discarded the status code. The failure looked
like an AI problem for a day.

Fixed in the runner: it now reads the status code, spaces uploads (`-UploadSpacingSeconds`) and retries
with backoff (`-Upload429Retries`). Both reports stay **Rejected** on the demo, because no evidence exists
at all for them.

## 5. The manifest's own coordinates and timestamps are invented

`evaluation/manifest.sf-real-eval.csv` carries `latitude`, `longitude` and `captureTimeUtc` per image. They
are placeholders, and the pattern gives it away:

- All ten positions sit within roughly 600 m of `37.7749,-122.4194`, regardless of what the photo shows.
- Every timestamp is exactly `10:00:00Z`, one day apart: `2026-11-02` through `2026-11-11`.
- The run happened on **2026-11-01**, so every "capture time" in the file is dated in the future.

This is not cosmetic. The comparison engine's *timestamp trust* rule decides `LikelyStillPresent` versus
`NotObservedInNewImage` partly from capture times, and the location rule matches within 25 m. Feeding it
fabricated times and positions would produce outcomes that look evidence-based and are not — which is one
reason the set contains **0 real comparison pairs**. Real EXIF capture times and real GPS must replace these
columns before any comparison claim is made from this dataset.

## 6. What this analysis changed in the product

| Finding | Change that survived into the build |
|---|---|
| Labels can be wrong | Reviewer decisions were recorded only against what we could see in the frame; three labels left failing rather than edited |
| A model can guess confidently | Every AI answer carries `humanReviewRequired` and a confidence bar; nothing auto-verifies |
| Abstention is not failure | The runner reports `Inconclusive` separately from `Incorrect` — they are never summed |
| Missing data must not read as safe | SAM 3.1 returns `null`, never `0%`; routes show an Unknown bar; unmeasured obstruction says *"absence of a measurement is unknown, not a clear path"* |
| Contradictions must not be overwritten | A reviewed comparison keeps the original `ConflictingEvidence` beside the reviewer's `Inconclusive` (`docs/JUDGE_GUIDE.md`) |
| Rate limits are part of the system | Runner now surfaces `429` instead of swallowing it |

## 7. What this set does **not** prove

- **n = 10**, one run, one model, one city — and that city is San Francisco, not our pilot city Sylhet.
- No wheelchair user was involved, so nothing here measures impact (see `docs/USER_TESTING.md`).
- 0 comparison pairs, so the comparison engine has **no evidence from real photography** — only 5 synthetic
  Mock pairs in `manifest.sf.csv`.
- The 25-image `evaluation/sf-dataset/` set is generated placeholders run under **Mock**: 6 correct / 19
  inconclusive. Pipeline proof, never accuracy.
- Three of ten labels and all ten coordinates/timestamps were wrong, which says more about how quickly a
  small "dataset" degrades than about `muse-spark-1.3`.

## 8. What would make this a real evaluation

1. 20–30 photographs taken by us, in Sylhet, with ground truth written down **before** any model output.
2. Real capture times from EXIF and real coordinates from the device, replacing §5's placeholders.
3. At least two same-place old/new pairs so the comparison states are tested on real imagery.
4. Fix the §3 scoring rule so an unlabelled image cannot be scored as an error.
5. Report per-class results and abstentions separately, on the same run, with the runner's output committed
   beside this analysis.

Until then the honest claim is: **the pipeline works end to end on real photographs, the model is right
three times out of six answerable cases, and our own ground truth was wrong at least three times.**
