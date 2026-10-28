# Camera Live-Source Gate — Evidence Record

Status: **BLOCKED** (checked 2026-10-28, twice). No real camera source is
enabled, and no permission note has been fabricated. Everything else in
camera Phase A–E (registry, ingestion, dedupe, auto-comparison,
candidates, map/report/route/reviewer UX, daily AI budget, privacy
handling, tests) is implemented and green.

## What was verified

| Check | Result | Source |
|---|---|---|
| NYC DOT official camera page | HTTP 200 | `nyc.gov/html/dot/html/motorist/atis.shtml` |
| Feed format (list) | `GET webcams.nyctmc.org/api/cameras` → JSON `[{id,name,latitude,longitude,area,isOnline,imageUrl}]` | Multiple independent integrators + NYC DOT public viewer backend |
| Feed format (still) | `GET webcams.nyctmc.org/api/cameras/{id}/image` → JPEG, no auth, refresh guidance minimum 15s (our cadence is 60s) | Same |
| Camera host reachability from this network | **Timeout** (both attempts, direct and via fetch tool) | Live probes 2026-10-28 |
| 511NY developer programme | HTTP 200 at `511ny.org/developers/help`; access under the *511NY Developer's Access Agreement* with a self-obtained API key, "powered by 511NY" attribution, data must not be altered | NYSDOT 511NY developer pages and agreement summaries |
| NYC DOT image-reuse licence for automated reuse | **Not clearly licensed**; formal DOT data-sharing agreement route exists (`tmcdot@dot.nyc.gov`); image-reuse objections reported | Public sources reviewed 2026-10-28 |

## Why the gate stays blocked

1. The still-image host cannot be reached from this network (reported
   US-only serving), so three real one-minute polls cannot be observed
   here.
2. Neither provider route is licensed-and-keyed for this project yet:
   NYC DOT image reuse needs terms confirmation, and 511NY needs a
   developer agreement plus API key — a user-side credential, like the
   Meta Model API key. Per project rules, credentials are requested
   from the user, never invented.

## Unblock steps (in order)

1. User obtains a 511NY developer API key (accepting the Developer
   Access Agreement) **or** confirms NYC DOT reuse terms/agreement.
2. Run the Worker from a network that reaches `webcams.nyctmc.org`.
3. Register exactly one camera, recording the agreement/key basis
   verbatim as its permission note (the registry refuses to enable a
   source without one).
4. Observe three one-minute polls; record outcomes and image hashes.
   Only then enable further sources.

A local fake feed must never be presented as this gate: the local
Changed → Unchanged → Changed behaviour is already covered by
integration tests and proves the pipeline, not provider access.
