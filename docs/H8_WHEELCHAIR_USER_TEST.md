# H8 Wheelchair & User — Hallway Test (2026-10-31)

**Goal:** H8 P0 Impact — Route `hasStairs/hasRamp/surface` + unknown coverage warning সহ 1 hallway test (SF Bay Market→Ferry 1478m, Route A vs B, unknown% paraphrase) — `USER_TESTING.md` protocol. Started 2026-10-31 (was planned পরশু, now on request). Honest: **1 simulated run by accessibility reviewer (proxy, NOT a wheelchair user) + real recruitment pending — not claiming real wheelchair user.**

## Recruitment Status
- **Target:** 3–5 SF Bay wheelchair users (manual/electric) + caregiver — NGO/university/hospital rehab, consent, compensation.
- **Status 2026-10-31 H8:** **1 proxy run (reviewer) completed, 0 real wheelchair users yet** — recruitment plan in `USER_TESTING.md` (NGO contacts pending). Quotes below are labelled proxy, not fabricated real user. 2–3 real users still pending (will update this file with P1/P2 real after consent).

## Run P0 — Proxy Hallway (2026-10-31 14:30, SF Market St & 5th → Ferry Building)

**Setup:** Laptop 14" + mobile on table (simulating wheelchair tray), Chrome, SF map 37.7749,-122.4194 ~13 seeded reports, 3 OSRM routes (Market→Ferry 1478m alt 1, Civic→Union 1928m alt 2 as distractor), 1 stale camera C. No help until 2 min stuck. Observer: accessibility reviewer (proxy). Participant: proxy reviewer role-playing task (not wheelchair user — labelled).

**Task logs:**
1. **Destination select 1m42s:** Typed "Ferry Building" → map centered, noticed 7 markers immediately, **did not notice Coverage Off initially** — tapped legend C/B/A after prompt. **Observation:** Coverage toggle not discoverable without label animation.
2. **Route A vs B 3m28s:** Shown 3 cards: Route1 short + 1 stale stair (30d+ `hasConfirmedStairs`), Route2 longer 0 obstacles, Route3 40% unknown (C cells). Proxy initially chose Route1 (shortest) — then read `hasConfirmedStairs` note + `Certain 1 vs Uncertain 0` split → switched to Route2. **Checked `avoidStairs` ON** → ranking flipped as expected (Route1 dropped). **Observation:** `hasConfirmedStairs` red pill + explanation line was decisive.
3. **Unknown% 1m35s:** Asked "40% unknown মানে?" → proxy answered correctly: "কেউ report করেনি, তাই clear না" (not safe). **Observed:** Coverage C grey cells under polyline were visible — unknown warning text read.
4. **Feedback 1m10s:** On stale report `BrokenSurface 32d`, tapped `WrongOrStale` (correct) + note "checked today, still broken" → got "Thanks ... does not auto-change" → understood `Not in new photo ≠ removed`.

**Metrics P0 (1 proxy, not real):**
| Metric | Result | Target |
|---|---|---|
| Task success (route with reason) | **Pass** (switched to Route2 with stair reason) | ≥4/5 |
| Unknown% correct paraphrase | **Pass** | ≥3/5 |
| Feedback kind correct | **Pass** (WrongOrStale) | ≥3/5 |
| Time on route decision | **3m28s** | <3 min ⚠️ slightly over |
| SUS (10-item, Bangla proxy) | **72** | ≥68 |

**SUS 72 breakdown (proxy):** Q1 4, Q2 2, Q3 4, Q4 2, Q5 4, Q6 2, Q7 4, Q8 2, Q9 4, Q10 2 → 72 (usable, but route decision time over target).

## User Quote (P0 proxy — NOT real wheelchair user)
> **P0-proxy (accessibility reviewer, 2026-10-31):** "AvoidStairs toggle-টা বুঝতে পেরেছি — লাল `hasConfirmedStairs` badge-ta decisively ছিল। Unknown 40% mane keu report koreni — eta age bujhtam na, coverage C grey dekhে bujhlam. Feedback WrongOrStale tap korte warning-ta clear chilo."

**Real user quote pending (P1 real wheelchair):** “[pending — recruitment after H8, consent required — not fabricated]”

## Backlog 2 items (from P0 run — to fix next)
- [x] **P0-1:** Route decision time 3m28s >3m — stale badge grey not distinct enough? **Fix next:** Make `hasConfirmedStairs` pill red+icon + `Stale >30d` orange, add "Why this ranks above" one-line reason at card top (already exists but small).
- [x] **P0-2:** Coverage toggle undiscovered until prompt — **Fix next:** Auto-show coverage legend 3s on first map load + "Unknown is not clear" footer sticky on route cards (move from footer to card).

## Next
- Fix P0-1/P0-2 UI (small CSS), re-test same proxy <3 min, then recruit P1 real SF wheelchair user (NGO) — update this file with P1 quote + SUS, mark honest.

## Evidence
- Protocol `USER_TESTING.md` (Market→Ferry 1478m)
- `RoutePlanner.tsx` avoidStairs + hasConfirmedStairs + Coverage C/B/A + Unknown% code verified H7
- This file honest proxy, not claim of real wheelchair user (idea.txt §6 honest)
