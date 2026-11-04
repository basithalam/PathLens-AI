# Hallway User Testing — PathLens AI (H4.1)

**Goal:** Without testing with real wheelchair users, impact is 0 (idea.txt §6). This protocol is for a 20-minute hallway test with 3–5 people with mobility limitations.

## Recruitment (honest)
- 3–5 participants: wheelchair users (manual/electric) + 1 caregiver/assistant mix, **any city (demo seed: Sylhet, Bangladesh 24.91,91.84; camera demo: SF Datumfeed 37 cameras)**. Recruit via local NGO, hospital rehab, community (consent, no coercion).
- Compensation: local rate + transport + snack. No real name published without written consent — quotes use participant P1/P2 with role only.
- Status **2026-11-01: PENDING — User will provide (per chat 2026-11-01)** — protocol ready, recruitment pending user-supplied participants. Until then, this file records plan + anticipated backlog, not fabricated results.
- Update **2026-11-01:** Map/Route demo seed is Sylhet 24.91,91.84 (works in any city — pan anywhere) — `a871b26` — coverage/route now opens on Sylhet. H8 proxy (reviewer, NOT wheelchair user) completed — see `H8_WHEELCHAIR_USER_TEST.md`; real wheelchair P1/P2 pending user.
- Update **2026-10-31 H8:** 1 proxy run completed (reviewer, NOT wheelchair user) — see `H8_WHEELCHAIR_USER_TEST.md` for P0 metrics + 2 backlog fixes; real wheelchair recruitment still pending (P1/P2 after consent).

## Consent template (copy to paper form)
> I voluntarily take part in the PathLens AI hallway test. My screen/interaction may be recorded for the demo, but my name/photo will not be public without written permission. I can stop at any time. Contact: [organiser email].  
> Participant signature: ______ Date: ______ Organiser: ______

## Protocol — Same task for all (10 min task + 10 min interview)

**Setup:** Laptop/tablet on desk, mobile on wheelchair tray, SF map centered 37.7749,-122.4194 with ~13 seeded reports, 3 routes, 1 stale camera. No help until stuck 2 min.

1. **Destination select (2 min):** "You want to go from Market St & 5th to Ferry Building — type the destination." Observe: can they type/select? Do they notice coverage grid?
2. **Route A vs B (4 min):** Show 3 route cards: Route 1 short + 1 stale stair, Route 2 longer 0 obstacles, Route 3 40% unknown. Ask: "Which would you take? Why?" Observe: do they read explanation lines, certain/uncertain split, `avoidStairs` toggle, hasConfirmedStairs note?
3. **Unknown% (2 min):** "What does 40% unknown on this route mean?" Expected: "Nobody reported here, so not proven clear" — not "safe". Observe confusion.
4. **Feedback (2 min):** On a stale report, "Tell us if this is still there — tap one of the 4 Quick Feedback buttons." Observe: do they understand StillVisible vs NotInNewPhoto vs WrongOrStale vs NotSure? Note "Not in new photo ≠ removed" warning.

**Post-task interview (10 min, record audio with consent):**
- Was it easy to find the destination? Did you understand Route differences? What did Unknown mean to you? What info was most useful? Does the app make you over-reliant? What would stop you from using this before a real trip?

## Metrics (to fill after runs)
| Metric | How | Target |
|---|---|---|
| Task success (route chosen with reason) | Observer + recording | ≥4/5 |
| Unknown% correct paraphrase | Interview quote | ≥3/5 |
| Feedback kind correct (given scenario) | Tap accuracy | ≥3/5 |
| SUS (10-item, English) | Paper | ≥68 |
| Time on route decision | Stopwatch | <3 min |

## Anticipated backlog (from design review, to validate with users)
- [ ] Route card stale badge not distinct enough (ageDays>30 grey vs red?) — needs test
- [ ] Coverage legend C/B/A + letter+colour+text triple encoding may still be too subtle on mobile — add haptics? Test
- [ ] MiniMapPicker 260px height too small for wheelchair tray — increase to 320px? Test
- [ ] "Unknown ≠ clear" footer not read — move to route explanation top? Test
- [ ] GPS accuracy warning missing in MiniMapPicker (H4.3)

## Quotes (pending real runs)
> P1 (manual wheelchair, SF): "[pending — after run]"  
> P2 (caregiver, SF Bay): "[pending — after run, 2nd group]"

## Update 2026-10-31 H8 P0 proxy
> P0-proxy (accessibility reviewer, 2026-10-31): "I understood the AvoidStairs toggle — the red hasConfirmedStairs badge was decisive. Unknown 40% means nobody reported here — I didn't get that before, I got it seeing the grey C cells on coverage." — NOT a wheelchair user, honest proxy. Real P1/P2 pending.

> Quote as taken during the run (Bengali, romanised): "AvoidStairs toggle-ta bujhte perechi — laal
> `hasConfirmedStairs` badge-ta decisive chilo. Unknown 40% mane keu report koreni — eta age bujhtam na,
> coverage C grey dekhe bujhlam." (English version above; the original script is kept here for the local
> record only.)

## Weekly review
After every 3 runs, triage backlog, fix top 2 UI items, re-test 2 participants. Record in `docs/H4_FIX_REPORT.md` (next file).

## Reference
idea.txt §6, Blueprint §UX + A11y, `docs/DEMO_SCRIPT.md` H2 backup note.
**Click-by-click manual for participants (what to tap, in what order, and what the UI cannot do yet):**
`docs/WHEELCHAIR_USER_GUIDE.md`.
