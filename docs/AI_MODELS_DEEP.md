# How Meta AI Works in PathLens — `muse-spark-1.3` + `sam-3.1` (code-verified 2026-11-04)

Two Meta models, two separate jobs, one evidence record. Classification and measurement fail independently — a failed measurement never discards a good classification.

## 1. `muse-spark-1.3` — what the photo shows

**Endpoint:** `POST {MetaAi:Endpoint}/v1/chat/completions` (OpenAI-compatible; live: `https://api.meta.ai/v1/chat/completions`). Code: `backend/src/PathLens.Infrastructure/Ai/MetaImageAnalysisService.cs`.

1. **Prepare** — `ImageDownscaler`: longest edge → 1568, JPEG q85, **EXIF/GPS stripped** before the image leaves the server. Unreadable image → original bytes used (preprocessing never fails the job).
2. **Ask** — one message: fixed schema-1.0 text instruction (seven obstacle types `BlockedSidewalk|BrokenSurface|MissingOrBlockedRamp|StairsOrStep|ConstructionBarrier|Other|Unknown`, `visibleEvidence`, `limitations`, `confidence` 0..1, `humanReviewRequired`) + base64 `image_url` data URI. Timeout 60 s, Bearer key server-side only (`MetaAi__ApiKey`, never frontend/committed).
3. **Validate** — `choices[0].message.content` (string or parts array) must parse via `ImageAnalysisResultParser`: schema 1.0, known `ObstacleType`, confidence 0..1. Else `Meta AI returned an unusable analysis` — never invented. Missing config → `missing: MetaAi:Endpoint, MetaAi:Model, MetaAi:ApiKey` (502).
4. **Stamp + show** — provider `Meta`, model `muse-spark-1.3`, timestamp stored; UI: `✓ Live Meta AI`, type, confidence bar (<40 red / 40–70 orange / >70 green), visible evidence, limitations, `Human review required`.

**Solves:** unlabelled photo → typed, scored, limitation-stated evidence that can be ranked, compared and audited. Measured: 3/3/2/2 on 10 real photos — a demo, not accuracy (`docs/AI_EVALUATION_SF_REAL.md`).

## 2. `sam-3.1` — how much of the path is blocked

**Endpoint:** `POST {Sam31:Endpoint}/v1/responses` (default `https://api.meta.ai/v1/responses`; chat endpoint 404s for this model). Code: `Sam31SceneMeasurementService.cs`, `Sam31Options.cs`, `ObstacleConcepts.cs`, `ObstructionGeometry.cs`. Opt-in: `Sam31:Enabled=true` (two billed calls/photo; live demo on).

1. **Two prompts** — walkway concept (`the paved pedestrian walkway`) + obstacle concept for the classified type. Full phrases only: `scooter` → zero boxes vs `scooters blocking the sidewalk` → two (measured 2026-11-03). `Unknown` has no concept.
2. **Boxes, no score** — SAM returns `<|box;x1=..;y1=..;x2=..;y2=..;w=..;h=..|>` tokens in `output[0].content[0].text`; masks ignored. Malformed tokens skipped, never guessed.
3. **Geometry** — clip obstacles to walkway, union overlaps (counted once), largest walkway wins → **1..100 share of visible walkway**. No walkway/obstacle box → **null = unknown, never 0%**. Measured-only: `BlockedSidewalk`, `ConstructionBarrier`, `Other` (surface defects excluded after a clear path scored 100% on damaged paving — `docs/ERROR_ANALYSIS.md` §9).

**Solves:** *how much* of this frame's path is blocked — shown in evidence cards (`N% of the visible walkway in this frame is occupied`) and route explanations (`covers about 23% … (SAM 3.1; share of photo, not metres)`), never in ranking. Limits stated in UI: in-photo share only, no confidence, analysis-time only.

## 3. Together

Muse classifies → SAM measures → comparison engine compares over time → reviewer verifies → routes rank by fewest known obstacles. Mock doubles (`MockImageAnalysisService`, `MockSceneMeasurementService`) exist for tests/demo and are always labelled `Demo/mock — not real AI`.
