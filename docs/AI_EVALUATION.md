# AI Evaluation — PathLens AI (Blueprint §19)

Reproducible runner: `evaluation/run-evaluation.ps1` (manifest `id,image,expectedObstacle,latitude,longitude,captureTimeUtc,pairId,expectedOutcome`).

Rules: 20–30 own photos, no faces/plates, labels written **before** AI run, free-tier content may be used for product improvement.

Run (API with `MetaAi__ApiKey`):
```powershell
./evaluation/run-evaluation.ps1 -ManifestPath evaluation/manifest.example.csv -OutputPath evaluation/results.json
```
Uploads → polls job → compares pairs → counts Correct / Incorrect / Inconclusive + errors (exit 1, never hidden).

Error analysis: group incorrect by cause (wrong type, location mismatch, poor visibility, timestamp, model error) and fix causes, not counts. Keep limitations statement from latest run.

Current provider: `IImageAnalysisService` abstraction, `MetaImageAnalysisService` (OpenAI-compatible, `MetaAi__Endpoint/Model/ApiKey`) + `MockImageAnalysisService` for tests/demo (UI labels “Demo/mock analysis — not real AI”). Gate A (live Meta test) requires real key — per project rule, requested only after development, as done for NYC camera gate.

Latest status: mock provider green in tests, live gate blocked on missing key — honest, per Blueprint §22.
