# Evaluation

Reproducible AI evaluation for PathLens AI (Blueprint Section 19).

## Manifest

CSV columns: `id`, `image` (path relative to the manifest), `expectedObstacle`, `latitude`, `longitude`, `captureTimeUtc`, `pairId`, `expectedOutcome`.

Labels are written **before** any AI run. Rows sharing a `pairId` form an old/new pair; the row carrying `expectedOutcome` states what the comparison should produce.

- `manifest.example.csv` — 3 tiny images (pipeline proof, Mock only, 1x1 PNG) — kept for CI.
- `manifest.sf.csv` — **G2 SF dataset: 25 synthetic placeholders** (see below) — winner demo, 5 pairs covering 4 outcomes.

Rules for the dataset:

- Use only your own photos, with no identifiable faces or number plates (free-tier API content may be used for product improvement). Synthetic placeholders below are own-generated via `System.Drawing` (no face/plate, EXIF stripped).
- 20–30 images is a demo evaluation, never proof of general accuracy.

## G2 SF Dataset — 25 images + 5 pairs

**Location:** SF demo city 37.7749,-122.4194 spread (Market St, Civic Center, SoMa, Mission, Embarcadero). Each image 1024x768, 6-10 KB, generated locally — not real SF photos yet, but diverse colours/shapes per obstacle type (replaces old 3x 1x1).

**Categories (25):**
- BlockedSidewalk 5, BrokenSurface 4, MissingOrBlockedRamp 3, StairsOrStep 3, ConstructionBarrier 3, Other 1, Unknown 6 (clear path + dark blur)

**Pairs (5, 10 images):**
- `pair-a` LikelyStillPresent — BlockedSidewalk persists same place (sf-16/sf-17, 11m apart, 2026-10-28 vs 29)
- `pair-b` LikelyStillPresent — BrokenSurface persists (sf-18/sf-19)
- `pair-c` NotObservedInNewImage — old ConstructionBarrier, new clear Unknown (sf-20/sf-21)
- `pair-d` ConflictingEvidence — old BlockedSidewalk vs new StairsOrStep same coords (sf-22/sf-23)
- `pair-e` Inconclusive — dark blur Unknown both (sf-24/sf-25, low visibility)

`captureTimeUtc` logged before run (phone note simulation), lat/lon within 25m for Strong match (per `ComparisonService.EvaluateLocation` ≤25m strong, ≤75m possible, >75m mismatch).

## Run

Start the API first (with the AI provider configured server-side), then:

```powershell
# Mock pipeline (no key needed, labelled Demo/mock — not real AI) — 3-image example
$env:Ai__Provider = "Mock"
dotnet run --project backend/src/PathLens.Api --urls http://localhost:5145
./evaluation/run-evaluation.ps1 -ManifestPath evaluation/manifest.example.csv -OutputPath evaluation/results.json

# G2 SF dataset — Mock (pipeline) or Meta live (real accuracy)
$env:Ai__Provider = "Mock"  # or Meta with real key per docs/SECURITY.md
./evaluation/run-evaluation.ps1 -ManifestPath evaluation/manifest.sf.csv -OutputPath evaluation/results.sf.json
# then copy best run to results.json for CI badge:
Copy-Item evaluation/results.sf.json evaluation/results.json
```

The runner uploads each image, polls the analysis job with a bounded wait, compares pairs, and writes expected-vs-actual counts. `Correct`, `Incorrect`, and `Inconclusive` are reported separately; errors fail the run (exit code 1) instead of being hidden.

Mock note: Mock always returns `Unknown confidence 0`, so with real labels most pairs become `Inconclusive` or `Incorrect` — this proves pipeline, not accuracy. Live Meta (Groq gsk_... or Llama LLM_... per `AI_GATE_A_REPORT.md G1`) is needed for real `BlockedSidewalk 0.82` etc. See `docs/AI_EVALUATION.md` for latest Mock vs Live table.

## Seed for judges

```powershell
./scripts/seed-sf.ps1                # local: http://localhost:5145
./scripts/seed-sf.ps1 -ApiBaseUrl https://pathlens.cognitivebd.com  # live Hetzner
```
Creates 7 diverse SF reports (Market St, Civic Center, SoMa, Mission, Embarcadero, Ferry Building) with evidence + analysis so `docker compose up --build` never shows empty map. See `scripts/seed-sf.ps1`.

## Error analysis

Group every incorrect result by cause — wrong obstacle type, location mismatch, poor visibility, timestamp problem, or model error — and fix causes, not just counts. Keep the short limitations statement from the latest run with the submission materials.
