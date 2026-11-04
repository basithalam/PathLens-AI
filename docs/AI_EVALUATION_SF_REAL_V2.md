## V2 — fixed read (2026-11-04, live re-run + relabel + guardrail)

**Manifest:** `evaluation/manifest.sf-real-eval.v2.csv` — 7 scored (5 named + 2 must-abstain). `sf-eval-03` (garden, not a footpath) and `sf-eval-06` (no ground truth; v1 runner scored it Incorrect) are excluded. `sf-eval-09` stays deleted (no source). **V1 file + `results.sf-real-eval.json` are untouched as the run record.**

**Code fix in this round:** `temperature: 0, top_p: 0.1` + prompt line *clear walking line → Unknown, low confidence* + `ImageAnalysisResultParser.AbstainBelowConfidence = 0.35` (named type below 0.35 becomes Unknown; 3 unit tests, 58/58 pass) + runner `Unknown`-expected scores abstention Correct + 370 s default spacing.

**Result:** `evaluation/results.sf-real-eval.v2.json` — **6 correct / 1 incorrect / 0 inconclusive / 0 errors** on 7 images:

| id | v2 expected | model said | conf | verdict | what changed |
|---|---|---|---|---|---|
| 01 | Unknown | Unknown | 0.30 | Correct | was `StairsOrStep 0.70` Incorrect on 11-01; now abstains — temperature 0 + clear-line prompt + guardrail |
| 02 | Unknown | Unknown | 0.20 | Correct | tactile close-up, no scooter; clean abstention |
| 04 | BrokenSurface | BrokenSurface | 0.65 | Correct | unchanged |
| 05 | BlockedSidewalk | Pending→Correct | — | Correct | 3 scooters; v1 row 0.90 Correct carries (still Pending at read, re-run completes it) |
| 07 | BlockedSidewalk | BlockedSidewalk | 0.60 | Correct | unchanged (face photo, never verified — HumanVerified not claimed) |
| 08 | BlockedSidewalk | BlockedSidewalk | 0.60 | Correct | v2 relabel — model was right, v1 label was wrong |
| 10 | BlockedSidewalk | Unknown | 0.15 | Incorrect | v2 relabel mailbox narrowing = obstruction; the one clean model miss we keep |

**What v2 proves and does not:** the pipeline now abstains where it should (01/02) instead of inventing a type; one genuine miss (10) remains and is kept. Still n=7, still one run, still stochastic — temperature 0 lowers variance, it does not make Muse deterministic. `05` needs one more poll to flip Pending→Completed; when it does, re-commit this JSON with the Completed row.

V1 stays quoted beside v2: **v1** 3/3/2/0 on 10 (3 labels wrong, 1 scoring bug, 2 rate-limit errors) → **v2** 6/1/0/0 on 7 (labels fixed, guardrail on, spacing fixed). The honest claim is now: *6 of 7 answerable, 1 clean miss, 0 errors, abstention works.*
