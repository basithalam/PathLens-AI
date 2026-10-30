# Product Requirements — PathLens AI

Source: Blueprint §1 + F5/F6 updates (2026-10-29).

## Problem
Wheelchair users need to know before travel: is a ramp blocked, is an old obstacle still there, how old/who provided the info, which alternative has better evidence, and where there is no info at all. Normal maps hide the gap.

## MVP scope (F0–F6 done)
- **Reports:** create/edit, search, filter, pagination, nearby, duplicate suggestion (25 m, same/Unknown type, reviewer confirms/rejects, confirmed excluded from lists/map/routes).
- **Evidence:** photo upload JPEG/PNG/WebP ≤10 MB (magic bytes), private storage, background AI job with bounded retry, schema-1.0 validation, EXIF stripped.
- **Comparison:** deterministic rules (location, timestamp trust, newer-photo strength) → LikelyStillPresent / NotObservedInNewImage (never means removed) / ConflictingEvidence / Inconclusive; human review appends, never overwrites.
- **Map:** OSM+Leaflet, viewport reload, summary, list sync, verification letters V/?/X/! + square C for cameras.
- **Coverage (F5):** `GET /api/map/coverage` grid (50 m default, auto-scales to ≤1000 cells, 30 s cache), status C/B/A with letter+colour+text, always visible legend, click to zoom/filter, route integration under polyline.
- **Routes:** OSRM foot geometry, 50 m obstacle matching, unknown-length %, explainable ranking (fewest obstacles → shortest), wheelchair v1 attributes `hasStairs/hasRamp/surface` + preferences `avoidStairs` hard rule, `preferVerified/avoidStale/preferShortest`.
- **Review:** JWT (reviewer@pathlens.local / Reviewer@123, admin@pathlens.local / Admin@123), queue (unverified + conflicting + stale cameras + duplicate suggestions), audit trail, camera candidates.
- **Feedback (F6):** `POST /api/reports/{id}/feedback` 4 kinds (StillVisible/NotInNewPhoto/WrongOrStale/NotSure), audit, anonymous→admin fallback, never auto-verifies; frontend quick capture map picker + QuickFeedback buttons.

## Demo city
San Francisco, CA (37.7749,-122.4194) — map default + camera candidates (511SF Bay / SFMTA / Caltrans / GenericPublicFeed). NYC leg blocked honestly (camera-live-gate.md); Dhaka is Phase-2 expansion.

## Non-goals (F7+)
Live city-wide coverage, guaranteed wheelchair-safe routing, native app, real-time moderation at scale, custom model training, Mapillary live — per Blueprint §1.3.

## Acceptance (Definition of Done)
- Coordinated API + frontend contracts, validation `400`, auth `401/403`, `404` where needed.
- Tests: 92 passing (39 unit + 53 integration) + frontend lint/typecheck/build green.
- Limitations documented honestly (PostGIS deferred, ImageSharp license, mock vs real AI, unknown ≠ clear).
