# Camera Frame Quality Gate — Thresholds & Rationale (H3.4)

**Module:** `PathLens.Infrastructure.Cameras.CameraFrameAnalyzer` + `CameraOptions` (Phase F3 quality gate, H3.4 calibration)  
**Goal:** Spend AI budget only on frames where a barrier could plausibly be seen; otherwise stay `Inconclusive`.

## Pipeline
1. `Analyze(bytes)` → decode → reject frames <16×16 as `Decoded=false` → unknown quality (never suitable/unsuitable).
2. Resize to 96×96 grayscale → mean **brightness** (0-255) + Laplacian-variance **sharpness** proxy + 64-bit **difference hash** (9×8 → 64 bits, Hamming distance).
3. `CameraIngestionService.AssessFrameAsync` applies thresholds from `CameraOptions`:

| Threshold | Config key | Default | Meaning |
|---|---|---|---|
| `MinBrightness` | `Cameras:MinBrightness` | **10** | Mean 0-255 below this = too dark to judge → `TooDark` (stored, no AI). Night, underpass, lens cap. |
| `MinSharpness` | `Cameras:MinSharpness` | **2** | Laplacian variance below this = too blurred/flat → `TooBlurred` (stored, no AI). Fog, defocus, wall fill. |
| `ViewChangeHashDistance` | `Cameras:ViewChangeHashDistance` | **30** | Hamming distance to previous `PerceptualHash` > this = view changed → `ViewChanged` (analysed, but comparison forced `Inconclusive` with repositioning note). PTZ or truck blocking. |
| `QualityGateEnabled` | `Cameras:QualityGateEnabled` | **true** | `false` = gate off, all frames go to AI (for debugging). |

If `Decoded=false` → `Unknown` (too small/decode fail) → frame goes to AI as-is (unknown ≠ suitable).

## Why these defaults
- **Brightness 10:** Empirically separates true night (mean 3-8 on test feed samples) from dim daylight (15-40) without rejecting cloudy daytime. 20 samples would tighten per city; until then conservative low value avoids false `TooDark` on overcast.
- **Sharpness 2:** Laplacian variance <2 is flat (wall/sky/fog) on 96×96 downscale; normal outdoor frames score 8-40. Very conservative to avoid rejecting genuinely obstructed but textured scenes (construction debris still textured).
- **Hamming 30:** 64-bit hash: distance 0-5 = identical (deduped earlier by SHA-256), 6-20 = minor lighting shift, 22-35 = noticeable crop/PTZ, >40 = distinct scene. 30 balances reposition detection vs sunset lighting. Calibrate with 20 labelled pairs (same view / repositioned) when a live SF feed is enabled.

## Calibration plan (20 samples, pending live feed)
Until a live SF feed is authorised (see `docs/camera-live-gate.md` — still blocked 2026-10-30), thresholds are defaults. When enabled:
1. Collect 20 frames per class: **Normal daylight**, **Night/TooDark**, **Blurred/Fog**, **Repositioned** (manual PTZ).
2. Run `CameraFrameAnalyzer.Analyze` offline, record `Brightness`, `Sharpness`, `PerceptualHash` + human label.
3. Plot ROC: sweep `MinBrightness` 5-20, `MinSharpness` 1-5, pick knee where `TooDark/TooBlurred` precision >90% and recall >80%.
4. For view change, compute Hamming distances within-class vs across-class; set `ViewChangeHashDistance` at 95th percentile of same-view distances.
5. Document in this file with 3 example images per class (cropped, no faces/plates) and the chosen thresholds.

## Evidence & API
- Every `EvidenceItem` now carries `QualityFlag` (`Suitable|TooDark|TooBlurred|ViewChanged|Unknown`), `QualityNote`, `PerceptualHash`, `ImageBrightness`, `ImageSharpness`.
- `TooDark/TooBlurred` → stored, **never analysed** (job records why, comparison stays `Inconclusive`).
- `ViewChanged` → analysed, but comparison forced `Inconclusive` with repositioning reason.
- `Unknown` (too small to judge) → never `Suitable`.
- Frontend `EvidenceSection` shows `qualityFlag` + note + metrics.

## Limitations
- No face/plate blur (stated in `docs/SECURITY.md`).
- Laplacian variance is proxy, not true MTF; conservative defaults until live calibration.
- Hamming 30 is not proven on this city's cameras — treat as initial, not validated.

## Config
```json
"Cameras": {
  "PollingEnabled": false,
  "PollCheckSeconds": 15,
  "DailyAnalysisBudgetPerCamera": 240,
  "QualityGateEnabled": true,
  "MinBrightness": 10,
  "MinSharpness": 2,
  "ViewChangeHashDistance": 30,
  "DatumfeedApiKey": ""
}
```
