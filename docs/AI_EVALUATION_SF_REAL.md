# AI Evaluation — Real SF Sidewalks (Muse Spark) — 2026-11-01

**Status:** Running — 10 Real SF Wikimedia Commons (CC BY-SA, 800px thumb, 174-639KB) + Ground-Truth + Muse Spark `muse-spark-1.3 @ https://api.meta.ai/v1/chat/completions`

**Dataset:** `evaluation/sf-real-eval/` — 10 Real SF sidewalks (not synthetic) — lat/lon SF 37.7749,-122.4194 spread — `manifest.sf-real-eval.csv`

**Ground Truth (Human Before AI):**

| id | image | expectedObstacle | lat,lon | why |
|---|---|---|---|---|
| sf-real-eval-01 | sf-eval-01.jpg Sidewalk in SF | BlockedSidewalk | 37.7749,-122.4194 | Planters + narrow strip |
| sf-real-eval-02 | sf-eval-02.jpg Stockton st | BlockedSidewalk | 37.7755,-122.4180 | Scooters/bikes on sidewalk |
| sf-real-eval-03 | sf-eval-03.jpg Sidewalk plantings | Other | 37.776,-122.417 | Plantings, not block |
| sf-real-eval-04 | sf-eval-04.jpg Uneven Mission | BrokenSurface | 37.7765,-122.4165 | Cracks, two lines |
| sf-real-eval-05 | sf-eval-05.jpg LimeBike scooters | BlockedSidewalk | 37.774,-122.42 | Scooters blocking |
| sf-real-eval-06 | sf-eval-06.jpg Castro St | Unknown | 37.7735,-122.421 | Clear, no obstacle |
| sf-real-eval-07 | sf-eval-07.jpg Market homelessness | BlockedSidewalk | 37.7729,-122.4205 | Encampment blocking |
| sf-real-eval-08 | sf-eval-08.jpg North Beach | Unknown | 37.778,-122.415 | Clear |
| sf-real-eval-09 | sf-eval-09.jpg Tank Hill | Unknown | 37.7785,-122.414 | Clear hillside |
| sf-real-eval-10 | sf-eval-10.jpg SF 17073 | Unknown | 37.779,-122.4135 | Clear |

**Muse Spark Results (Hetzner Live — `results.sf-real-eval.json`):**
- Running via `run-evaluation.ps1 -ApiBaseUrl https://pathlens.cognitivebd.com` — 10 images, 90s poll, `provider: Meta, model: muse-spark-1.3`
- Current: 7 Reports on Hetzner (`totalCount 7`), `evaluation/results.sf-real-eval.json` pending — will show `correct/incorrect/inconclusive` per groundTruth vs Muse `obstacleType, confidence, visibleEvidence, limitations`
- Synthetic G2 (25 placeholders, 6 correct Mock) = workflow proof; **Real SF 10 = accuracy proof** — 10 is demo, not general accuracy

**Limitations:** 10 is small demo, not general accuracy; Wikimedia CC BY-SA attribution required; Inconclusive safe, never hidden; Traffic cameras (Datumfeed 37 SF) show highway not footpath — manual close-up primary

**Next:** `results.sf-real-eval.json` complete → README G2 update: Synthetic workflow + Real SF accuracy

**Live Evidence:** `GET /health/ready` → `ai-provider Healthy Meta muse-spark-1.3` + `POST /api/reports/{id}/evidence` Sylhet `BlockedSidewalk 90%` (Muse) — not Mock

