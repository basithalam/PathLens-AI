# Final Verification — Phases H1–H6

**Original run:** 2026-10-30 | **Re-verified:** 2026-11-03 | **Verifier:** AI Auditor + builds

> **Read this first.** The tables below are the record as of 2026-10-30, kept intact rather than rewritten.
> Several claims in them were later shown to be wrong or stale — every one of those is listed in
> **"Corrections as of 2026-11-03"** at the bottom, and that section outranks the table.

**City on 2026-10-30:** San Francisco, CA 37.7749,-122.4194 — sole pilot; Dhaka deferred, no camera source.
**2026-11-03:** Sylhet became the demo-seed city (map default, route endpoints, judge demo flow). San
Francisco remains only where it is factually true — the public traffic-camera feeds and the synthetic
25-image evaluation set. See `docs/JUDGE_GUIDE.md`.
**Update 2026-11-04:** PathLens is **open globally — works in any city** (Sylhet stays only as the default map seed).

## Build proof (2026-10-30)
```
dotnet build backend/PathLens.slnx -c Release → Build succeeded. 0W 0E
dotnet test PathLens.UnitTests -c Release → Passed 39
npm run typecheck → pass
npm run lint → 0 errors, 7 warnings (pre-existing exhaustive-deps)
npm run build → 8 routes, First Load 102kB
```
No secrets committed at that date: `appsettings.Development.json` is trust-only, `.env` ignored,
`.local` ignored.

## Phase table — requested vs delivered (as recorded 2026-10-30)

| Phase | Name | Goal | Delivered | Gap fixed that day |
|---|---|---|---|---|
| **H1** Hotfix & Demo Unblocked | crash / 404 / filter | ✅ leaflet 404 fix + `public/marker-*.png` + `fixLeafletIcons`, pagination preserves search and type, full docker-compose (api/web/db/worker profile) + standalone + `.env.example`, ImageSharp 3.1.8 0W0E | none — `ROUTING_STALE_DAYS` was missing from `.env.example`, added as `ROUTING_STALE_DAYS=30` |
| **H2** Honest Demo Complete | prove real, not mock | ✅ `AI_GATE_A_REPORT` 404/401 recorded honestly, `camera-live-gate` 525/timeout, `evaluation/results.json` 3/3 Mock, `AI_EVALUATION` + `DEMO_SCRIPT` 2-min | none |
| **H3** Scale Foundation | city-scale PostGIS + routing | ✅ `PostGisHelper` dual mode ST_DWithin/ST_Within with fallback, SKIP LOCKED, `StaleEvidenceDays` configurable, `camera-quality-thresholds.md` | RoutePlanner hard-coded 30 → now notes "backend configurable" |
| **H4** Real User & Trust | wheelchair tester + a11y + PWA | ✅ `USER_TESTING.md` protocol, a11y marker keyboard alt + sr-only, TEST_PLAN a11y, PWA manifest/sw/offline-queue + compress + GPS >50/100 warning, `feedback/summary` endpoint | none |
| **H5** Production Hardened | security + CI/CD | ✅ JWT fail-closed in Production, CORS multi-origin, rate limits (login 5/min, upload 10/hour → 429), moderation blocklist, `IsDeleted` soft-delete + `DELETE /api/reports/{id}`, `PRIVACY_RETENTION` 30d/1y, `ci.yml`, osrm + ai health checks, docs dedup | none |
| **H6** Pilot Ready | research + i18n + analytics | ✅ `SF_RESEARCH.md` with 5 OSRM samples 1478–2756 m, CCTV provider terms, `lib/i18n.tsx` EN/Bengali LocaleProvider, `admin/stats` + reputation + `AdminStatsPanel`/`ReputationPanel` | README H6 title clarified "(was Dhaka Pilot Ready — now SF sole pilot)" |

## H1–H6 file audit (26 checked, 2026-10-30)
All present then: `public/marker-*.png`, `manifest.json`, `sw.js`, `leaflet-icons.ts`, `next.config.ts`
standalone, ReportForm offline queue + GPS warning, EvidenceSection compress, feedback summary,
`PostGisHelper`, `ReportRepository`, `AnalysisJobProcessor`, `CameraPollingWorker`, `RoutingOptions`,
`SF_RESEARCH`, `i18n.tsx`, `AdminEndpoints` (stats + reputation), `AdminStatsPanel`, `ReputationPanel`, the
H1–H6 fix reports, `camera-live-gate`, `AI_GATE`.

## Dhaka mentions — intentional at the time
Every Dhaka string carried "deferred — no camera source" (camera-live-gate, H3_POSTGIS,
PRODUCT_REQUIREMENTS, ROUTING_WHEELCHAIR, USER_TESTING) — a stated decision, not a hidden gap. Superseded on
2026-11-01 by Sylhet, and on 2026-11-03 by the pilot-city copy pass.

## Gaps fixed during that verification (2026-10-30)
1. `RoutePlanner.tsx` — stale badge text gained "(backend configurable)" to align with H3.3
2. `.env.example` — added `ROUTING_STALE_DAYS=30` (missing for H3.3)
3. `README.md` H6 line — clarified the pilot-city wording

---

## Corrections as of 2026-11-03 — this section outranks everything above

Measured, not assumed. Each item names how it was checked.

| Claim in the 2026-10-30 record | What is actually true on 2026-11-03 | How it was checked |
|---|---|---|
| "ci.yml" delivered, 92 tests running in CI | **`ci.yml` had never executed a single job.** Line 44 held `- run: echo "health: …"` — an unquoted plain scalar containing `health: `, which YAML reads as a mapping key, so the workflow failed to parse on every push. Run #59 (2026-11-03) is the first green CI: backend 39 unit + 53 integration, frontend lint/typecheck/build | GitHub run page annotation `Invalid workflow file: .github/workflows/ci.yml#L44`; run showed the file path as its name and **zero jobs**. Fixed in `7f28758` |
| H5 delivered `IsDeleted` soft-delete | the entity and `HasQueryFilter(report => !report.IsDeleted)` shipped **with no migration**, so any database built from the repo returned `42703: column a.IsDeleted does not exist` → HTTP 500 on every report read. The deployed DB had been patched by hand, which is why live looked fine | Reproduced locally, fixed by `20261103090726_AddReportSoftDelete`, then proven on a brand-new empty database (all 11 migrations → 9 tables incl. both columns) and on the live API (`/api/reports` 200) |
| H6 delivered `lib/i18n.tsx` EN/Bengali | the file was a **dead no-op stub** — nothing imported it — while this doc still claimed Bengali support. It has been deleted; the UI is English only | `grep` for `LocaleProvider|useLocale|LocaleSwitch` found only the file's own definitions |
| "SF sole pilot" | **Sylhet is the pilot city**; SF stays only as the camera-feed source and the synthetic dataset | copy pass `1ba6a5e`, live home page verified |
| "No further gaps — all green" | several gaps were still open and are now written down in `docs/JUDGE_GUIDE.md` ("What is NOT proven"): real-image accuracy is **3 correct / 3 incorrect / 2 abstained / 2 upload errors / 0 comparison pairs** on 10 borrowed photos; **three of that set's own ground-truth labels do not survive re-reading the images**; the 25-image SF set is synthetic and was run under Mock; reporter reputation is empty; there is no delete control in the UI; Wikimedia CC BY-SA attribution is still owed | measured against the live API and by reading each photo |
| "npm run build → 8 routes, 102 kB" | 10 routes (custom `not-found`, admin console), First Load JS 102–114 kB | `npm run build` output |
| "health osrm" | `/health/ready` reported `osrm: Degraded — 403` while real routing returned 201: the probe sent no User-Agent, the router did. Now both send the same identity and the check reads **Healthy** | live `/health/ready` before and after `f65c69c` |
| H4 "wheelchair tester" | **one proxy run by an accessibility reviewer, zero real wheelchair users.** Never present the proxy as user evidence | `docs/H8_WHEELCHAIR_USER_TEST.md`, `docs/USER_TESTING.md` |
| "No secrets committed" | true for the working tree, but a full Llama API key was printed **inside `docs/SECURITY.md`** and is reachable in tracked commits `771a485` and `2386b7f`. Masked in the tree; history deliberately not rewritten; the key must be treated as revoked | `git log -S` and `git grep` |

**Also added since:** the frontend design system (every component class that had no CSS rule is now styled,
light + dark), one-tap reviewer/admin sign-in with all three demo credentials printed on the panel, the
`evaluation/run-evaluation.ps1` 429-aware upload handling, the `Auth:Seed*` password-rotation control
(kept **off** here on purpose — the demo is meant to be open to judges), and
`docs/WHEELCHAIR_USER_GUIDE.md` — the click-by-click manual for how a wheelchair user gives feedback.

**Licence:** MIT (`LICENSE`), code and docs only; the borrowed Wikimedia Commons photos stay CC BY-SA and their
per-file attribution is still owed.
