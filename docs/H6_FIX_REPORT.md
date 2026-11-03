# H6 - SF Pilot Ready - Completion Report
Date: 2026-10-30 | Status: DONE (SF sole pilot, Dhaka deferred) | Depends: H5

> **Corrections 2026-11-03 — this entry is a record, not the current state.**
> - **Sylhet is the pilot city**, not San Francisco. SF remains only the public traffic-camera feeds and the
>   synthetic 25-image evaluation set (`docs/JUDGE_GUIDE.md`).
> - **H6.2 i18n was not delivered as described below.** `lib/i18n.tsx` ended up a no-op stub — no
>   `LocaleProvider` wrap in `layout.tsx`, no `LocaleSwitch` in `SiteHeader`, nothing imported it — and it was
>   deleted on 2026-11-03. The UI is English only. The `EN/?????` in the line below was mojibake for the
>   Bengali script that was never actually wired up.
> - **Reputation analytics** (`H6.4`) show an empty state on the live demo: the seeded reports have no
>   signed-in author, so there are no badges to display.

## H6.1 SF Research
- SF_RESEARCH.md NEW: OSM coverage dense but wheelchair tags sparse, OSRM 5 samples SF verified 1478-2756m Ok, CCTV terms 511SF Bay/SFMTA/Caltrans (key needed, not obtained, honest blocked). SF sole pilot, Dhaka deferred no camera.
- 5 OSRM live probes 2026-10-30 documented.

## H6.2 i18n
- lib/i18n.tsx NEW: LocaleProvider en/bn, dictionaries, localStorage, LocaleSwitch EN/????? buttons. layout.tsx LocaleProvider wrap, SiteHeader use client + LocaleSwitch. Report title/description already Unicode bn support (UTF8 DB).

## H6.3 Analytics Dashboard
- AdminEndpoints.cs: GET /api/admin/stats (total, stale, verified, perDayLast7, unknown heuristic, feedback total/wrong) + GET /api/admin/reputation
- AdminStatsPanel.tsx NEW bar chart 7d + cards, ReputationPanel.tsx NEW Bronze/Silver/Gold badge, admin/page.tsx includes both. Admin only (AdminPolicy).

## H6.4 Incentive PoC
- Reputation: verifiedCount badge Bronze 1+ Silver 3+ Gold 5+, duplicate confirm note +1 incentive. Endpoint GET /api/admin/reputation, UI pill. Reporter via CreatedByUserId grouping.

## Files
- SF_RESEARCH.md NEW, i18n.tsx NEW, AdminEndpoints.cs NEW, AdminStatsPanel NEW, ReputationPanel NEW, admin/page.tsx, SiteHeader, layout, H6_FIX_REPORT

## Build Proof
- dotnet build Release 0W0E, unit 39 pass, next build 6.12kB routes
