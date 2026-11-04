# Manifest v2 — fair relabel + scoring fix (2026-11-04, NOT a re-run yet)

v1 (`manifest.sf-real-eval.csv`) is left untouched as the run record. This v2 fixes what re-reading the photos proved wrong — no image was swapped, only labels corrected to what the frame shows, each with its basis:

| id | v1 label | v2 label | basis (photo reading 2026-11-04) |
|---|---|---|---|
| `sf-eval-01` | BlockedSidewalk | **Unknown** | steep but clear hillside walkway, nothing on the walking line; a model must abstain here |
| `sf-eval-02` | BlockedSidewalk | **Unknown** | tactile-paving + panel-seam close-up, no scooter anywhere in frame; abstain is correct |
| `sf-eval-03` | Other | **excluded from v2** | not a footpath view (garden bed + wooden structure + solar light) — no benchmark claim possible |
| `sf-eval-06` | Unknown (scored Incorrect) | **excluded from v2** | wide empty Castro St — no ground truth to mark against; v1 runner scored Unknown-expected as Incorrect (§3 bug) |
| `sf-eval-08` | Unknown | **BlockedSidewalk** | café table + two chairs + walking cane ON the walkway — model `BlockedSidewalk 0.60` was right |
| `sf-eval-10` | Unknown | **BlockedSidewalk** | mailbox + bike rack narrowing the walking line — a fixed obstruction counts |
| `sf-eval-04/05/07` | unchanged | **unchanged** | reviewer-verified (curb seam / 3 scooters / tents-goods) — already correct |

**What v2 expects after the code fix:** `01,02` → model abstains (`Unknown`, Correct by v2 rule) · `04,05,07,08,10` → named correctly · `06,03` out of the set. v2 has 7 scored images (5 named + 2 abstain-must), down from v1's 10 — smaller but honest.

**Also fixed in code + runner (same commit):**
- `MetaImageAnalysisService` payload: `temperature: 0, top_p: 0.1` + prompt line *clear walking line → Unknown, low confidence* (variance down, not gone — LLM stays stochastic).
- `ImageAnalysisResultParser.AbstainBelowConfidence = 0.35`: a named type below 0.35 becomes `Unknown` (3 new unit tests, 58/58 pass).
- `run-evaluation.ps1`: `Unknown`-expected now scores abstention as Correct; default upload spacing 370 s (just over the 10/hour limit) so a 7-image run never trips our own 429.

**To run v2 (needs ~45 min wall clock for the spacing):** `./evaluation/run-evaluation.ps1 -ManifestPath evaluation/manifest.sf-real-eval.v2.csv -OutputPath evaluation/results.sf-real-eval.v2.json` then commit the v2 JSON beside v1. Do NOT overwrite `results.sf-real-eval.json`.
