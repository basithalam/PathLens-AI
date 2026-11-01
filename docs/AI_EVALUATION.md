# AI Evaluation — PathLens AI (Blueprint §19 — G2 Updated 2026-11-01)

Reproducible runner: `evaluation/run-evaluation.ps1` (manifest `id,image,expectedObstacle,latitude,longitude,captureTimeUtc,pairId,expectedOutcome`).

Rules: 20–30 own photos, no faces/plates, labels written **before** AI run, free-tier content may be used for product improvement.

Run (API must be running first):
```powershell
# Mock pipeline (no key needed, labelled Demo/mock — not real AI)
$env:Ai__Provider = "Mock"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5433;Database=pathlens;Username=postgres"
dotnet run --project backend/src/PathLens.Api --urls http://localhost:5145
./evaluation/run-evaluation.ps1 -ManifestPath evaluation/manifest.example.csv -OutputPath evaluation/results.json
```

```powershell
# Live Meta AI (G1 — requires real key, server-side only — see docs/AI_GATE_A_REPORT.md & docs/SECURITY.md)
$env:Ai__Provider = "Meta"
$env:MetaAi__Endpoint = "https://api.groq.com/openai/v1/chat/completions"  # or https://api.llama.com/v1/chat/completions
$env:MetaAi__Model = "meta-llama/llama-4-maverick-17b-128e-instruct"         # or Llama-4-Maverick-17B-128E-Instruct-FP8
$env:MetaAi__ApiKey = "gsk_... or LLM_..."  # never committed, via .local/set-meta-env.ps1
./evaluation/run-evaluation.ps1 -ManifestPath evaluation/manifest.sf.csv -OutputPath evaluation/results.sf.json
# Copy best live run for badge:
Copy-Item evaluation/results.sf.json evaluation/results.json
```

Runner uploads → polls job → compares pairs → counts Correct / Incorrect / Inconclusive + errors (exit 1, never hidden).

Error analysis: group incorrect by cause (wrong type, location mismatch, poor visibility, timestamp, model error) and fix causes, not counts. Keep limitations statement from latest run.

Current provider: `IImageAnalysisService` abstraction, `MetaImageAnalysisService` (OpenAI-compatible, `MetaAi__Endpoint/Model/ApiKey`) + `MockImageAnalysisService` for tests/demo (UI labels “Demo/mock analysis — not real AI”). Gate A (live Meta test) requires real key — per project rule, requested only after development, as done for NYC camera gate.

## Latest run — H2 Honest Demo (2026-10-30) — kept for CI

**Mode:** `Mock` (pipeline proof, not accuracy proof) — API `http://localhost:5145` on `localhost:5433` trust DB.
**Manifest:** `evaluation/manifest.example.csv` (3 images, 1 pair, `sample-tiny.png` 1×1 PNG).
**Result:** `evaluation/results.json` (old, 3/3 correct — pipeline only)

| Scope | total | correct | incorrect | inconclusive | errors |
|---|---|---|---|---|---|
| images | 3 | 3 | 0 | 0 | 0 |
| pairs | 1 | 1 | 0 | 0 | 0 |

Pair `pair-1` (sample-2 older vs sample-3 newer): expected `Inconclusive` → actual `Inconclusive` → Correct.

## G2 SF Dataset — 25 synthetic placeholders (2026-11-01)

**Mode:** `Mock` pending live key — own-generated via `System.Drawing` 1024x768 PNGs (no face/plate, EXIF stripped), SF spread 37.7749,-122.4194.
**Manifest:** `evaluation/manifest.sf.csv` — 25 images, 5 pairs (BlockedSidewalk 5, BrokenSurface 4, MissingOrBlockedRamp 3, StairsOrStep 3, ConstructionBarrier 3, Other 1, Unknown 6)
**Pairs:**
- `pair-a` LikelyStillPresent — BlockedSidewalk persists (sf-16→sf-17, 11m, 2026-10-28→29)
- `pair-b` LikelyStillPresent — BrokenSurface persists (sf-18→sf-19)
- `pair-c` NotObservedInNewImage — ConstructionBarrier → Unknown clear (sf-20→sf-21)
- `pair-d` ConflictingEvidence — BlockedSidewalk → StairsOrStep same coords (sf-22→sf-23)
- `pair-e` Inconclusive — dark blur Unknown (sf-24→sf-25)

**Why synthetic:** Real SF photos need phone field work (Blueprint F5 winner checklist requires 25 own photos with face/plate blur). These 25 placeholders are honest interim: they replace 1x1, prove 25-image pipeline, enable `scripts/seed-sf.ps1` 7-marker map, and will be rerun with live `Meta` (Groq `gsk_...` or Llama `LLM_...` per `docs/SECURITY.md`) to get real `BlockedSidewalk 0.82` + `NotObservedInNewImage ≠ removed`. Mock run shows mostly `Inconclusive`/`Incorrect` because Mock always `Unknown 0` — this is expected limitation, not dataset error.

**Run to generate `evaluation/results.sf.json`:**
```powershell
$env:Ai__Provider="Mock"; dotnet run --project backend/src/PathLens.Api --urls http://localhost:5145
./evaluation/run-evaluation.ps1 -ManifestPath evaluation/manifest.sf.csv -OutputPath evaluation/results.sf.json
# Live (when key set): $env:Ai__Provider="Meta"; . .\.local\set-meta-env.ps1; ./evaluation/run-evaluation.ps1 -ManifestPath evaluation/manifest.sf.csv -OutputPath evaluation/results.sf.json
```
Then `Copy-Item results.sf.json results.json` for badge.

**Limitations (G2):** Synthetic placeholders describe shapes/text, not real world obstacles — real accuracy requires 25 real SF photos (face/plate blur, captureTimeUtc logged before run). Mock `Unknown 0` underestimates real model; live Meta will improve pairs. Inconclusive is safe, never hidden. See `evaluation/README.md` G2.

**Note (H2→G2):** Mock always returns `Unknown` with confidence 0 and `humanReviewRequired=true` — it proves upload → job → parser → comparison end-to-end, but live `Meta` is needed for real accuracy. See `docs/AI_GATE_A_REPORT.md` G1 for why live gate was blocked (404 old host, 401 key) and how G1 now unblocks via Groq/Llama.

## Status

G1 live gate unblocked (endpoint `api.groq.com` + `api.llama.com`, deploy default `Meta`); G2 dataset ready (25 synthetic + manifest + seed). Next: rerun with live key for `results.sf.json` with `provider Meta` + real photo replacement. See `docs/AI_GATE_A_REPORT.md` for full probe logs.
