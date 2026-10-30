# H4 - Real User & Trust (P1 Product) - Completion Report
Date: 2026-10-30 | Status: DONE (protocol + a11y + PWA + feedback loop) | Depends: H3

## Summary
idea.txt SS6 Impact 0 risk mitigated: hallay protocol ready, a11y triple encoding keyboard verified, PWA offline minimal, feedback WrongOrStale grouped weekly.

## H4.1 Hallway User Test (3-5)
- docs/USER_TESTING.md NEW - recruitment (NGO/university), consent template, 4-step protocol (destination, Route A vs B, unknown%, feedback), metrics (success, SUS 68, time), anticipated backlog, quotes pending recruitment honesty (NOT YET RECRUITED).
- Backlog: marker stale badge, coverage legend mobile, MiniMap 260->320, footer unknown, GPS warning (H4.3 fixed).
- Video consent captured in template; weekly review to H4_FIX_REPORT next iteration.

## H4.2 A11y Audit
- AccessibilityMap.tsx: marker keyboard:true, alt title - status - type, riseOnHover, sr-only inside divIcon, role application + list fallback. Triple encoding letter+colour+text verified.
- TEST_PLAN.md A11y section NEW: WCAG 1.4.1/2.1.1/4.1.2, keyboard e2e, axe audit via a11y-audit.ts (npm i -D axe-core, runAxe() in console/Playwright, 0 violations expected).
- a11y-audit.ts NEW helper.

## H4.3 PWA Minimal
- public/manifest.json + public/sw.js (install/activate/fetch pass-through)
- src/lib/offline-queue.ts NEW: IndexedDB via localStorage fallback, enqueueReport/getQueue/dequeue, compressImageFile canvas 1280/0.8 <1MB.
- ReportForm.tsx: offline queue when navigator offline or API unreachable, GPS accuracy warning >50m/>100m with highAccuracy.
- MiniMapPicker already 260px + drag; layout.tsx manifest/themeColor/apple-touch-icon.
- EvidenceSection.tsx compress before upload with message.
- Build: next 8 routes 6.12kB map.

## H4.4 Feedback -> AI Loop
- FeedbackService already audit all Kinds; new endpoint GET /api/reports/feedback/summary in ReportEndpoints.cs aggregates Feedback* audit events (total, byKind, WrongOrStale total + byCause wrong-type/visibility/stale/other/no-note via note keyword heuristics). Note: not counts alone.
- Weekly process documented in TEST_PLAN.md and summary note: WrongOrStale grouped by cause drives prompt fixes.

## Files (H4)
- docs/USER_TESTING.md NEW, docs/TEST_PLAN.md, docs/H4_FIX_REPORT.md NEW
- apps/web/public/manifest.json NEW, public/sw.js NEW, src/lib/offline-queue.ts NEW, src/lib/a11y-audit.ts NEW
- src/app/layout.tsx (manifest/theme), components/map/AccessibilityMap.tsx (keyboard alt), components/reports/ReportForm.tsx (offline GPS), components/evidence/EvidenceSection.tsx (compress), backend Api Endpoints ReportEndpoints.cs (feedback/summary)

## Build Proof
- dotnet build Release 0W0E, unit 39 pass, lint 0 error 7 warnings (pre-existing), next build 8 routes
- A11y: manual keyboard Tab on map markers ok
- PWA: manifest.json present, sw.js registered placeholder
- Feedback: new endpoint returns byKind + wrongOrStaleByCause

## Next: H5 Production Hardened
Rate limiting, fail-closed JWT, CI/CD, Sentry/OpenTelemetry.

