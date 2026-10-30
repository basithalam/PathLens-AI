# Demo Script — PathLens AI (2 min, H2 Ready — honest demo)

**0–20s Problem:** wheelchair user stuck — "Map e road ache, kintu ramp blocked kina jani na." VO: ordinary map hides absence of data. PathLens AI makes the gap visible.

**20–50s Coverage shock (F5):** Map San Francisco (37.7749,-122.4194) → Coverage Off (7 markers on SF) → **On → ~60% grey C** (grid 50m, ≤1000 cells, cache 30s, click-to-zoom/filter). VO: "System jane je se jane na — Unknown ≠ Clear." Marker letters V/?/X/! + C for cameras (colour never alone). Legend always visible.

**50–90s Route + F2 wheelchair:** origin 37.7749,-122.4194 → dest 37.7790,-122.4100 → 3 OSRM foot routes (real geometry, 502 → no fake route). F1 shows per-obstacle age / 30-day stale flag / certain-vs-uncertain split + ranking reason. Toggle `avoidStairs` ON → ranking flips (hasConfirmedStairs hard rule, null=unknown never estimated). `preferVerified/preferShortest` noted under polyline.

**90–110s AI + audit (honest):** new photo upload (JPEG ≤10MB, magic-byte, EXIF stripped) → `POST /api/evidence/{id}/analyze` → job `202` → background Mock result `Unknown` (labelled **Demo/mock analysis — not real AI**) → reviewer Verified → audit trail (`/reviewer/audit` with actor/prev/new/note/time). Comparison on same report: `Inconclusive` default, `NotObservedInNewImage` shows warning "does not prove removed". H2 note: live Meta gate blocked (404 on `api.meta.ai/compat/v1/...` — see `docs/AI_GATE_A_REPORT.md`), mock proves pipeline; live run needs correct endpoint/key.

**110–120s Honest + impact (F6):** F5 summary + F6 QuickFeedback 4 buttons (StillVisible/NotInNewPhoto/WrongOrStale/NotSure — never auto-verifies) + camera freshness (`isStale` staleCameras) on `/map` list. VO: honest by design.

**Backup & H1/H2 polish:**
- H1: `marker-*.png` 404 fixed, pagination preserves `search+type`, `docker compose up --build` one-click (api+web+db+optional worker profile), Release `0 Warning 0 Error`, `.env.example` documented.
- H2: `evaluation/results.json` Mock 3/3 correct (pipeline proof), `docs/AI_GATE_A_REPORT.md` + `docs/camera-live-gate.md` both blocked honestly with probe logs, `AI_EVALUATION.md` updated, secrets never committed (see `docs/SECURITY.md`).
- Pre-recorded 1080p + localhost offline (pgdata seeded on 5433, `docker compose up` fallback) if live fails. Blueprint §20 checklist: rubric mapping, 3-min rehearse, real user quote (H4), deployment health/live+ready (`/health/live` always, `/health/ready` needs DB), no secrets committed.
