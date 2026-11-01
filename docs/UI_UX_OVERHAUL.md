# UI/UX Overhaul — PathLens AI (Judge-Impressive, Real-Project Quality)
**Date:** 2026-11-01 | **Goal:** Full redesign to best-practice, cross-platform responsive, smart modern data input

## Gap Analysis — Old UI Failed Judge
Homepage had no step flow; SiteHeader no mobile menu; filter bars plain; ReportForm 12 fields no grouping; Evidence drop zone no polish; Reports list ul with <br>; Map no onboarding.

## Design Principles
1. Clarity: eyebrow+ h1 + purpose + CTA above fold
2. Progressive disclosure: ReportForm 3 steps, not 12 fields
3. Mobile-first: 320→768→1024→1280, hamburger <768px, 44px taps
4. Visual hierarchy: 900 h1, brand teal #0f766e, 16px radius, shadows
5. Smart input: chips, pills, GPS warnings, always-visible map
6. Empty/loading/error states everywhere
7. A11y: focus-visible, landmarks, keyboard, color never alone

## Plan U1-U6
| Phase | Scope | Status |
| U1 Design System | globals.css tokens, responsive, components | ✅ Done |
| U2 Nav + Home | SiteHeader hamburger+active, hero metrics+3steps, status | ✅ Done |
| U3 Data Input | ReportForm chips, map, Evidence modern | 🔄 In Progress |
| U4 Data Display | Reports cards, Map legend, Routes, Detail | ⏳ Next |
| U5 Reviewer/Admin | kanban, timeline, cameras | ⏳ Next |
| U6 Responsive & A11y | 320 test, axe 0, lighthouse 95+ | ⏳ Next |

## U1 Done — globals.css
Tokens slate/teal #0f766e, Inter 400-900, 80rem max, mobile breakpoints, hero pulse, btn lift, feature-card hover, filter-bar icon, report-form fieldset, map-layout 1.8fr→1fr, drop-zone ghost, confidence bar G3, pill--on/off.

## U2 Done — SiteHeader + Home
SiteHeader use client + usePathname active + hamburger span→X <768px, + Report CTA. Home hero SF Pilot•Live AI + Know before you go + 3 CTAs + metrics + 3 trust cards + How It Works 3 steps + 3 feature cards + Live Status with icons.
