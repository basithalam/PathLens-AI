# AI Gate A — Live Meta AI Verification (H2.1)

**Date:** 2026-10-30 | **Status:** BLOCKED — honest, no fabrication | **Tester:** local network

## Objective
Verify that `MetaImageAnalysisService` (OpenAI-compatible `POST {Endpoint}/chat/completions`) can analyse a real pedestrian photo with a user-supplied key, before claiming "Live AI" in the demo.

## Configuration tried
- **Server-side env:** `MetaAi__Endpoint`, `MetaAi__Model`, `MetaAi__ApiKey` (from `D:\PathLens AI\.local\set-meta-env.ps1` — never committed)
- **File:** `backend/src/PathLens.Api/appsettings.Development.json` currently holds placeholder (`Endpoint=""`, `Model=""`) — real keys stay in user-secrets / env only (see `docs/SECURITY.md`)
- **Test image:** `evaluation/sample-tiny.png` (1×1 PNG, 69 bytes, base64 data URI)

## Probes run 2026-10-30

| # | Endpoint | Key alias | Result | Body |
|---|---|---|---|---|
| 1 | `https://api.meta.ai/compat/v1/chat/completions` | `LLM_1134...` | **404** | `{"title":"Not Found","detail":"Path '/compat/v1/chat/completions' was not found","status":404}` |
| 2 | `https://api.groq.com/openai/v1/chat/completions` | `gsk_9ce6...` | **401** | `Invalid API Key` (truncated) |
| 3 | `https://api.llama.com/compat/v1/chat/completions` (doc placeholder) | — | **Skipped** | Not probed — no key for that host in this session |

Groq and api.meta.ai probes used `Invoke-RestMethod` with `image_url` data URI + `llama-4-maverick-vision` model (15s timeout).

## Probes run 2026-10-31 (H7 Live Proof Sprint — now)

| # | Endpoint | Key alias | Result |
|---|---|---|---|
| 1 | `https://api.meta.ai/compat/v1/chat/completions` | `LLM_1134...` | **404 Not Found** — Path not found (retested H7) |
| 2 | `https://api.llama.com/v1/chat/completions` | `LLM_1134...` | **401 Unauthorized** — key not valid for this host |
| 3 | `https://llama-api.meta.com/v1/chat/completions` | `LLM_1134...` | **DNS unresolved** — `llama-api.meta.com` not resolvable |
| 4 | `https://api.groq.com/openai/v1/chat/completions` | `gsk_9ce6...` | **401 + substring error** — Invalid API Key (same as 10-30) |

**Interpretation H7:** No host accepts the current `LLM_...`/`gsk_...` keys. Real Meta AI pipeline is code-complete (`MetaImageAnalysisService` throws `Meta AI is not configured` → 502 never invents) but live call blocked on key/host mismatch. Requirement "Meta API to successful Request" ≠ "full pipeline" — we prove pipeline via Mock + unit test until user supplies correct `Endpoint/Model/ApiKey`.

## Interpretation
- The Meta endpoint in `.local/set-meta-env.ps1` (`api.meta.ai/compat/v1/...`) **does not exist** as typed — 404 proves path mismatch. Correct host may be `https://api.llama-api.com` or the deployment-specific OpenAI-compatible URL from your Meta developer console.
- The Groq key on disk (`gsk_9ce6...`) returned 401 — expired, revoked, or scoped to a different project/region.
- No live image analysis was performed; no analysis result was fabricated.

## What still works (honest)
- `MetaImageAnalysisService` fails loud: `Meta AI is not configured. Set MetaAi:Endpoint, MetaAi:Model, and the server-side MetaAi:ApiKey...` — never invents a result.
- When `Ai__Provider=Mock`, the pipeline is end-to-end green: upload → background job → `mock-deterministic-v1` result (`Unknown`, confidence 0) → `Inconclusive` comparison → UI shows `Demo/mock analysis — not real AI`. See `evaluation/results.json` (3 correct / 0 incorrect, Mock).
- Schema validation (`ImageAnalysisResultParser` 1.0) and `model/provider` stamping are independent of live provider.

## Unblock steps (user action)
1. Open your Meta developer portal → copy the **exact** OpenAI-compatible base URL + vision-capable model ID (e.g. `Llama-4-Maverick-Vision` or `Llama-4-Scout-Vision`) + API key.
2. In PowerShell (not committed):
   ```powershell
   $env:Ai__Provider = "Meta"
   $env:MetaAi__Endpoint = "https://<exact-host>/v1/chat/completions"
   $env:MetaAi__Model = "<vision-model-id>"
   $env:MetaAi__ApiKey = "<your key>"
   $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5433;Database=pathlens;Username=postgres"
   dotnet run --project backend/src/PathLens.Api --urls http://localhost:5145
   ```
3. Re-run: `.\evaluation\run-evaluation.ps1 -ManifestPath evaluation/manifest.example.csv -OutputPath evaluation/results.json`
4. Record the `provider` + `model` + `generatedAt` in the next update to this file. Until then, keep mock for tests/demo with label.

## Reference
Blueprint §AI Integration Requirements: "Verify the available Meta API's image input capabilities, usage restrictions, response schema, and authentication before implementing the integration — and never present mock output as actual AI inference." This gate honours that rule.
