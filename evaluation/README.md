# Evaluation

Reproducible AI evaluation for PathLens AI (Blueprint Section 19).

## Manifest

CSV columns: `id`, `image` (path relative to the manifest), `expectedObstacle`, `latitude`, `longitude`, `captureTimeUtc`, `pairId`, `expectedOutcome`.

Labels are written **before** any AI run. Rows sharing a `pairId` form an old/new pair; the row carrying `expectedOutcome` states what the comparison should produce. See [manifest.example.csv](manifest.example.csv).

Rules for the dataset:

- Use only your own photos, with no identifiable faces or number plates (free-tier API content may be used for product improvement).
- 20–30 images is a demo evaluation, never proof of general accuracy.

## Run

Start the API first (with the AI provider configured server-side), then:

```powershell
./evaluation/run-evaluation.ps1 -ManifestPath evaluation/manifest.example.csv -OutputPath evaluation/results.json
```

The runner uploads each image, polls the analysis job with a bounded wait, compares pairs, and writes expected-vs-actual counts. `Correct`, `Incorrect`, and `Inconclusive` are reported separately; errors fail the run (exit code 1) instead of being hidden.

## Error analysis

Group every incorrect result by cause — wrong obstacle type, location mismatch, poor visibility, timestamp problem, or model error — and fix causes, not just counts. Keep the short limitations statement from the latest run with the submission materials.
