# PathLens AI — Wheelchair User Feedback Manual

**What this is:** a click-by-click guide for how a wheelchair user, or a person with mobility difficulties,
gives feedback through the PathLens UI — every screen, every button label, every field, in order. Written for
judges and facilitators, so it names the exact on-screen text rather than describing it loosely.

**Live demo:** https://pathlens.cognitivebd.com · **Local:** http://localhost:3000
**UI language:** English only in this build (the Bengali i18n stub was removed on 2026-11-03 — see §7).

---

## 0. The five feedback paths — none of them require an account

| # | What the user does | Where | Endpoint behind it | Login? |
|---|---|---|---|---|
| 1 | Report a new obstacle | `/report/new` | `POST /api/reports` | **No** |
| 2 | Add a photo, run AI analysis | report page → *Photo evidence* | `POST /api/reports/{id}/evidence`, `POST /api/evidence/{id}/analyze` | **No** |
| 3 | Compare an old and a new photo | report page → *Evidence explorer* | `POST /api/comparisons` | **No** |
| 4 | Tap one of four Quick feedback buttons | report page → *Quick feedback* | `POST /api/reports/{id}/feedback` | **No** |
| 5 | Correct their own claim | `/reports/{id}/edit` | `PATCH /api/reports/{id}` | **No** |

Verified in code, not assumed: those routes carry no `RequireAuthorization`. The review endpoints are the
opposite — `POST /api/reports/{id}/review`, `/duplicate/confirm`, `/duplicate/reject` and
`GET /api/reviewer/*` all require the Reviewer policy. **So a participant never has to sign in to give
feedback; only a reviewer signs in to act on it.**

---

## 1. Report an obstacle — the most useful form of feedback

**Open:** `/report/new`, or the **+ Report** button at the top right of every page.

Fields, top to bottom, exactly as labelled:

1. **Title** — name the place so a stranger can find it, e.g. `Zindabazar crossing — rickshaw stand on the ramp`
2. **Description (optional)** — what blocks it, how much width is left, when you saw it
3. **Obstacle type** — dropdown with seven values:
   `BlockedSidewalk` · `BrokenSurface` · `MissingOrBlockedRamp` · `StairsOrStep` · `ConstructionBarrier` · `Other` · `Unknown`
4. **Severity** — `Unknown` · `Low` · `Medium` · `High`
5. **Latitude / Longitude** — three ways to fill them:
   - **Use my location** — browser GPS fills both, and a note appears with the accuracy, e.g.
     `Location filled from your browser (accuracy about 12 m). Check it before submitting.`
     If accuracy is over 50 m the note adds `⚠️ Accuracy is low (>50m) — drag the map pin to fine-tune.`
   - **Pick on map** — opens an embedded map; tap to drop the pin, drag it to adjust
   - type the numbers directly
6. **What did you actually see? (optional — Unknown is honest)** — this is the field that matters most for
   rolling users:
   - **Stairs or a step here?** → `Unknown` / `Yes` / `No`
   - **Ramp here?** → `Unknown` / `Yes` / `No`
   - **Surface** → `Unknown` / `Smooth` / `Uneven` / `Broken`

   > Leave any of them on **Unknown** if you did not check. The system never estimates them, and the route
   > card then reads `Stairs/ramp/surface not measured here (unknown, not estimated)`. A wrong `No` is worse
   > for the next user than an honest `Unknown`.
7. **Consent checkbox** — required; the form will not submit without it
8. **Create report** — takes you straight to the new report page

A new report is marked **Unverified** on arrival. That is correct and is shown openly — it becomes a verified
fact only after a reviewer acts on it (§6).

---

## 2. Add photo evidence

**Open:** any report page (`/reports/{id}`) → section **Photo evidence**.

1. **Choose a photo or drop it here** — JPEG, PNG or WebP, up to 10 MB. Drag and drop works too
2. **When was this photo taken? (optional)** — date and time picker. The panel states the rule in plain words:
   *"Capture time is your claim, not a verified fact. Without it, comparisons cannot make a time-based claim."*
3. **Upload photo** — the image is stored privately and served back only through the API
4. On the new photo's card, press **Run analysis** (or **Retry analysis** if an earlier attempt failed)

What comes back, and what to read first:

- **Possible obstacle** — the type the model proposes
- **AI confidence** — a bar with tick marks at 40% and 70%, labelled
  `Low — treat as weak evidence` / `Medium — uncertain, verify` / `High — still needs human check`
- **Visible evidence** — what the model says it can see in the frame
- **Limitations** — what it cannot tell from this photo
- **`✓ Live Meta AI`** or **`Demo/mock analysis — not real AI`** — which provider actually ran
- **Human review required** — an amber callout while no reviewer has acted

> **Limits to warn participants about before they start:** uploads are capped at **10 per hour** per client
> (H5 rate limit). If a session pushes more than ten photos, the eleventh returns HTTP 429 — that is
> infrastructure throttling, not a model failure.

---

## 3. Compare two photos of the same place

**Open:** report page → section **Evidence explorer — compare over time**.

1. **Earlier photo** — pick from the dropdown (only analysed photos appear)
2. **Later photo** — pick the newer one
3. **Compare photos**

The outcome is one of four, each with a written explanation:

| Outcome | What it means | What it does **not** mean |
|---|---|---|
| `LikelyStillPresent` | the obstacle is probably still there | proof it is there |
| `NotObservedInNewImage` | the newer photo did not show it | **it was not removed** — angle or coverage may explain it |
| `ConflictingEvidence` | the two photos disagree | either one is wrong — a human must look |
| `Inconclusive` | not enough to decide | the path is clear |

If fewer than two photos are analysed, the panel says so directly:
*"Two analysed photos are needed before a comparison can run."* Both photos must belong to the same report.

---

## 4. Quick feedback — the one-tap path

**Open:** any report page → panel **Quick feedback — help keep this honest**.

Four buttons, verbatim labels:

| Button reads | Use it when |
|---|---|
| **Still visible — obstacle is still there** | you went and it is still blocking |
| **Not in new photo — I checked, it's not visible now** | you went and it is gone from view |
| **Information wrong or old** | the report is wrong, out of date, or not what you saw |
| **Not sure** | you cannot tell — this is genuinely useful data |

- Optional **note** box: **Optional note (what you saw, when)**, e.g. `Checked today 3pm, still blocked by scaffolding`
- After a tap, the confirmation reads: *"Thanks — your "<kind>" feedback was recorded. It does not auto-change
  verification; it will be reviewed."*

**Where that feedback then appears** — show this to a participant, it is what closes the loop:

| Where | What they see |
|---|---|
| `/reviewer` → audit trail | `FeedbackStillVisible`, `FeedbackNotInNewPhoto`, `FeedbackWrongOrStale` entries with time and actor |
| `/admin` → Pilot analytics | the **Feedback: wrong/stale** stat tile |
| API | `GET /api/reports/feedback/summary` — weekly, grouped by cause |

> **Explain this before they tap:** feedback never changes a report's verification status by itself. A
> reviewer decides. Without that sentence, a participant concludes "my tap did nothing".
> Tapping while signed out leaves the actor empty; if you want their name in the trail, use the one-tap
> **Reviewer** sign-in on `/reviewer` first (all three demo credentials are printed on the panel).

---

## 5. Correct an existing claim

**Open:** report page → **Edit report** (top right) → `/reports/{id}/edit`.

Editable: **Title**, **Description (optional)**, **Obstacle type**, **Severity**, then **Save changes**.

Not editable here, and the form says why:
*"Location cannot be edited here; coordinates are part of the original claim. Verification status and
evidence history are also outside this form — only a reviewer changes those, in the review console."*

---

## 6. How a report becomes trustworthy (so users know what their tap does *not* do)

```
someone reports a place        →  claim, marked Unverified
photo evidence added           →  person or camera, stored privately
Meta AI reads the photo        →  type + visible evidence + confidence + limitations
evidence compared over time    →  LikelyStillPresent / NotObserved / Conflicting / Inconclusive
a human reviews it             →  Verify · Dispute · Reject, with a note, in an audit trail
routes use what is known       →  ranking weighs obstacles, freshness, verification; the rest stays unknown
```

Reviewer decisions are stored **beside** the AI output, never instead of it. A reviewer signs in at
`/reviewer` with the one-tap card; the queue lists unverified reports, camera candidates, possible duplicates
and conflicting comparisons.

---

## 7. What this build cannot take as feedback yet

Say these plainly. Do not let a participant believe a screen collects something it does not.

| Not available | Current state | What to do instead |
|---|---|---|
| **Reporter accounts / registration** | only seeded staff accounts exist; there is no sign-up | bookmark or note the report URL `/reports/<id>` — there is no "my reports" list |
| **Delete button** | `DELETE /api/reports/{id}` exists but needs a signed-in account, and no UI control calls it | ask the maintainer to remove it; the consent text says this |
| **Route usability feedback** | the four Quick feedback kinds describe a **report**, never a route | use the paper form in §9 |
| **Contact channel in the UI** | none | give participants the maintainer's email on paper |
| **Bengali interface** | removed; English only | explain each screen in the participant's language — this file is the script |
| **Voice-first output** | semantic HTML, `aria-live` regions and letter+colour marker encoding exist; there is no spoken output | run a screen reader over `/map` and `/report/new` and record what breaks |

---

## 8. Running a session (facilitator notes)

**Setup — 5 minutes**
1. Open `https://pathlens.cognitivebd.com/health/ready` and confirm all three checks are `Healthy`
2. Have one report page already open at `/reports` so you are not hunting during the session
3. Print the consent form (§10). **Do not start without it**
4. Put the laptop on the desk and a phone on the tray, at the height they will really use

**Task — 10 minutes.** The participant does the tapping; you watch and stay quiet.
1. "Report one obstacle on a route you use." → §1
2. "Add a photo of it and run the analysis." → §2
3. "You went back today — tap whichever of the four buttons fits." → §4
4. "Plan a route and tell me which one you would take, and why." → §6 of the app, `/routes`
5. "What does '40% unknown' on that route mean?"
   Correct answer: *nobody has reported there, so it is not proven clear.* "It means it is safe" is the failure.

**Interview — 10 minutes** (record audio only with written consent)
- Which piece of information actually changed your decision?
- Which screen confused you?
- Would you rely on this before a real trip? What would make you trust it, or stop trusting it?
- Does this make you over-reliant on the app? What is missing that you would still want to check?

**Recording results:** add each run to `docs/H8_WHEELCHAIR_USER_TEST.md` and **label it clearly as a real
participant or a proxy**. That file currently holds one proxy run by an accessibility reviewer and zero real
wheelchair users; keep it honest rather than filling the gap with guesses.

---

## 9. Route usability paper form (the UI does not collect this)

```
Participant (P1/P2 only, no published names): ____   Role: manual / electric / caregiver
Route shown:  card 1 / 2 / 3        Chose: ____
Why I chose it (own words): ______________________________________________
"unknown __%" understood?  yes / no     What I took it to mean: ____________
Obstacle actually present?  yes / no / unsure    Type: BlockedSidewalk / BrokenSurface /
                                                   MissingOrBlockedRamp / StairsOrStep / Other
Photo available?  yes / no   (if yes, upload it in the app — §2)
What the app said vs what I found: ________________________________________
Most useful information: ____________   Most confusing: ____________________
Problems using a screen reader / large text / one hand:  none / yes — what: ________
Would I use this before a real trip?  yes / no — why: ______________________
```

---

## 10. Consent template

> I take part in the PathLens AI hallway test voluntarily. My screen and interaction may be recorded for the
> demo, but my name or photo will not be published without my written permission. I can stop at any time.
> Contact: [organiser email]
> Participant signature: ______  Date: ______  Organiser: ______

---

## 11. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Photo upload returns 429 | 10 uploads per hour limit | wait an hour, or pace an eval run with `-UploadSpacingSeconds` |
| Sign-in returns 429 | 5 login attempts per minute | wait one minute |
| Analysis shows **Failed** | AI provider not configured, or the call errored | check `/health/ready` → `ai-provider`, then **Retry analysis** on the photo card |
| **Compare photos** has no options | both photos need `Completed` analysis first | run analysis on both |
| Badge reads **Demo/mock analysis** | provider is in Mock mode | labelled as not real AI by design; set the Meta provider for live results |
| Map looks empty in an area | nobody has reported there | that is the product's message: unknown is not clear |

---

**In one line:** a wheelchair user gives feedback by **reporting a place → adding a photo → tapping one of the
four Quick feedback buttons → editing if needed**. All of it works with no account and no password, none of it
silently changes a verification status, and route usability — the thing the UI cannot yet ask — goes on the
paper in §9.
