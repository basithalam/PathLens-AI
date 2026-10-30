# Hallway User Testing — PathLens AI (H4.1)

**Goal:** idea.txt §6 — বাস্তব wheelchair user দিয়ে পরীক্ষা না করলে Impact 0। এই প্রোটোকল 3–5 জন mobility limitation থাকা ব্যক্তির সাথে 20-মিনিট hallway test এর জন্য।

## Recruitment (honest)
- 3–5 participants: wheelchair users (manual/electric) + 1 caregiver/assistant mix, SF Bay local (Dhaka deferred — no camera source). Recruit via SF Bay accessibility NGO, university disability support, hospital rehab (consent + no coercion).
- Compensation: local rate + transport + snack। No real name published without written consent — quotes use participant P1/P2 with role only.
- Status **2026-10-30: NOT YET RECRUITED** — protocol ready, recruitment pending H4 approval + funds/time। Until then, this file records plan + anticipated backlog, not fabricated results.

## Consent template (copy to paper form)
> আমি PathLens AI hallway test এ স্বেচ্ছায় অংশ নিচ্ছি। আমার screen/interaction record করা হতে পারে demo-র জন্য, কিন্তু আমার নাম/ছবি public হবে না লিখিত অনুমতি ছাড়া। আমি যেকোনো সময় থামতে পারি। যোগাযোগ: [organiser email]।  
> Participant signature: ______ Date: ______ Organiser: ______

## Protocol — Same task for all (10 min task + 10 min interview)

**Setup:** Laptop/tablet on desk, mobile on wheelchair tray, SF map centered 37.7749,-122.4194 with ~13 seeded reports, 3 routes, 1 stale camera. No help until stuck 2 min.

1. **Destination select (2 min):** "তুমি Market St & 5th থেকে Ferry Building যেতে চাও — destination টাইপ করো।" Observe: can they type/select? Do they notice coverage grid?
2. **Route A vs B (4 min):** Show 3 routes cards: Route 1 short + 1 stale stair, Route 2 longer 0 obstacles, Route 3 40% unknown. Ask: "কোনটা নেবে? কেন?" Observe: do they read explanation lines, certain/uncertain split, `avoidStairs` toggle, hasConfirmedStairs note?
3. **Unknown% (2 min):** "এই route-এ 40% unknown মানে কী?" Expected: "কেউ report করেনি, তাই clear না" — not "safe". Observe confusion.
4. **Feedback (2 min):** On a stale report, "এটা এখনো আছে কি না জানাবে — Quick Feedback 4 button থেকে একটা ট্যাপ করো।" Observe: do they understand StillVisible vs NotInNewPhoto vs WrongOrStale vs NotSure? Note "Not in new photo ≠ removed" warning.

**Post-task interview (10 min, record audio with consent):**
- গন্তব্য পাওয়া সহজ ছিল? Route পার্থক্য বুঝেছ? Unknown কী মনে হলো? কোন তথ্য সবচেয়ে দরকারী? App কি অতিরিক্ত নির্ভরশীল বানাচ্ছে? What would stop you from using this before a real trip?

## Metrics (to fill after runs)
| Metric | How | Target |
|---|---|---|
| Task success (route chosen with reason) | Observer + recording | ≥4/5 |
| Unknown% correct paraphrase | Interview quote | ≥3/5 |
| Feedback kind correct (given scenario) | Tap accuracy | ≥3/5 |
| SUS (10-item, Bangla) | Paper | ≥68 |
| Time on route decision | Stopwatch | <3 min |

## Anticipated backlog (from design review, to validate with users)
- [ ] Route card stale badge not distinct enough (ageDays>30 grey vs red?) — needs test
- [ ] Coverage legend C/B/A + letter+colour+text triple encoding may still be too subtle on mobile — add haptics? Test
- [ ] MiniMapPicker 260px height too small for wheelchair tray — increase to 320px? Test
- [ ] "Unknown ≠ clear" footer not read — move to route explanation top? Test
- [ ] GPS accuracy warning missing in MiniMapPicker (H4.3)

## Quotes (pending real runs)
> P1 (manual wheelchair, SF): “[pending — after run]”  
> P2 (caregiver, SF Bay): “[pending — after run, 2nd group]”

## Weekly review
After every 3 runs, triage backlog, fix top 2 UI items, re-test 2 participants. Record in `docs/H4_FIX_REPORT.md` (next file).

## Reference
idea.txt §6, Blueprint §UX + A11y, `docs/DEMO_SCRIPT.md` H2 backup note.
