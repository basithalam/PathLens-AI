# AI Evaluation — PathLens AI (Blueprint §19)

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
# Live Meta AI (requires real key, server-side only — see docs/AI_GATE_A_REPORT.md)
$env:Ai__Provider = "Meta"
$env:MetaAi__Endpoint = "https://<exact-host>/v1/chat/completions"
$env:MetaAi__Model = "<vision-model-id>"
$env:MetaAi__ApiKey = "<your key>"
./evaluation/run-evaluation.ps1 -ManifestPath evaluation/manifest.example.csv -OutputPath evaluation/results.json
```

Runner uploads → polls job → compares pairs → counts Correct / Incorrect / Inconclusive + errors (exit 1, never hidden).

Error analysis: group incorrect by cause (wrong type, location mismatch, poor visibility, timestamp, model error) and fix causes, not counts. Keep limitations statement from latest run.

Current provider: `IImageAnalysisService` abstraction, `MetaImageAnalysisService` (OpenAI-compatible, `MetaAi__Endpoint/Model/ApiKey`) + `MockImageAnalysisService` for tests/demo (UI labels “Demo/mock analysis — not real AI”). Gate A (live Meta test) requires real key — per project rule, requested only after development, as done for NYC camera gate.

## Latest run — H2 Honest Demo (2026-10-30)

**Mode:** `Mock` (pipeline proof, not accuracy proof) — API `http://localhost:5145` on `localhost:5433` trust DB.
**Manifest:** `evaluation/manifest.example.csv` (3 images, 1 pair, `sample-tiny.png` 1×1 PNG).
**Result:** `evaluation/results.json`

| Scope | total | correct | incorrect | inconclusive | errors |
|---|---|---|---|---|---|
| images | 3 | 3 | 0 | 0 | 0 |
| pairs | 1 | 1 | 0 | 0 | 0 |

| id | expectedObstacle | actualObstacle | provider | model | verdict |
|---|---|---|---|---|---|
| sample-1 | Unknown | Unknown | Mock | mock-deterministic-v1 | Correct |
| sample-2 | Unknown | Unknown | Mock | mock-deterministic-v1 | Correct |
| sample-3 | Unknown | Unknown | Mock | mock-deterministic-v1 | Correct |

Pair `pair-1` (sample-2 older vs sample-3 newer): expected `Inconclusive` → actual `Inconclusive` → Correct.

**Note:** Mock always returns `Unknown` with confidence 0 and `humanReviewRequired=true` — it proves upload → job → parser → comparison end-to-end, but a 20–30 own-photo evaluation with live `Meta` is still needed for real accuracy. See `docs/AI_GATE_A_REPORT.md` for why live gate is blocked (404 on `api.meta.ai/compat/v1/...`, 401 on Groq key).

**Limitations statement from this run:** "A 20-30 image set is a demo evaluation, never proof of general accuracy. Inconclusive is reported separately from incorrect. Mock provider results are not real AI inference."

## Latest status (pre-H2)

Mock provider green in tests, live gate blocked on missing/incorrect endpoint — honest, per Blueprint §22. See `docs/AI_GATE_A_REPORT.md` for full probe logs.
