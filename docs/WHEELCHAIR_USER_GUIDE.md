# PathLens AI — চাকা-চলাচলকারী ব্যবহারকারীর ফিডব্যাক ম্যানুয়াল

**কীসের গাইড:** একজন wheelchair user বা চলাচলে অসুবিধা থাকা মানুষ **UI দিয়ে কীভাবে ফিডব্যাক দেবেন** — প্রতিটা ক্লিক, প্রতিটা লেবেল, আর যেখানে UI সাহায্য করে না সেখানে কী করতে হবে।
**যে ভাষায় অ্যাপ চলছে:** English (বাংলা UI এই বিল্ডে নেই — `lib/i18n.tsx` stub-টা ২০২৬-১১-০৩ মুছে ফেলা হয়েছে)। তাই নিচে UI-র ইংরেজি লেবেল হুবহু উদ্ধৃত, বাকিটা বাংলায়।
**Live:** https://pathlens.cognitivebd.com · **Local:** http://localhost:3000

---

## ০. এক নজরে — ফিডব্যাক দেওয়ার ৫টা রাস্তা, কোনো লগইন ছাড়াই

| # | ব্যবহারকারী কী করবে | কোথায় (URL) | পিছনে কোন API |
|---|---|---|---|
| ১ | নতুন বাধার **report** তৈরি | `/report/new` | `POST /api/reports` |
| ২ | report-এ **ছবি যোগ** + AI analysis | report page → *Photo evidence* | `POST /api/reports/{id}/evidence`, `POST /api/evidence/{id}/analyze` |
| ৩ | পুরোনো/নতুন ছবি **তুলনা** | report page → *Evidence explorer* | `POST /api/comparisons` |
| ৪ | **Quick feedback** — ৪টা বাটন + নোট | report page → *Quick feedback* panel | `POST /api/reports/{id}/feedback` |
| ৫ | নিজের claim **সংশোধন** | `/reports/{id}/edit` | `PATCH /api/reports/{id}` |

> **গুরুত্বপূর্ণ:** ১–৫ কোনোটিতেই account বা password লাগে না। এটা কোডে নিশ্চিত করা (`ReportEndpoints.cs`, `EvidenceEndpoints.cs`, `ComparisonEndpoints.cs` — এই endpoint গুলায় `RequireAuthorization` নেই)। মানে participant-কে "আগে login করো" বলা যাবে না, বললে ভুল বলা হবে।

---

## ১. প্রথম জিনিস: নতুন report দিয়ে ফিডব্যাক (সবচেয়ে কাজের পথ)

**ঠিকানা:** `/report/new` — বা যেকোনো পেজের উপর-ডানে **+ Report** বাটন।

ফর্মে যা যা আছে (উপরে থেকে নিচে, হুবহু লেবেল):

1. **Title** — জায়গাটা চেনার মতো নাম লিখুন। যেমন `Zindabazar crossing — rickshaw stand on the ramp`
2. **Description (optional)** — কী বাধা, কত চওড়া জায়গা বাকি, কখন দেখলেন
3. **Obstacle type** — dropdown, ৭টা মান:
   `BlockedSidewalk` · `BrokenSurface` · `MissingOrBlockedRamp` · `StairsOrStep` · `ConstructionBarrier` · `Other` · `Unknown`
4. **Severity** — `Unknown` · `Low` · `Medium` · `High`
   > চাকা-চলাচলের জন্য practical ধরন: গাড়ি/রিকশা দখল = `BlockedSidewalk`; ফাটল/গর্ত = `BrokenSurface`; ramp নেই বা বন্ধ = `MissingOrBlockedRamp`; সিঁড়ি = `StairsOrStep`
5. **Latitude / Longitude** — তিনটা উপায়:
   - **Use my location** বাটন → browser GPS থেকে ভরে যায়; নিচে `accuracy ~Xm` লেখা আসে
   - **Pick on map** বাটন → মানচিত্র খোলে; ট্যাপ করলে point বসে, marker টেনে ঠিক করা যায়
   - সরাসরি টাইপ
6. **What did you actually see? (optional — Unknown is honest)** fieldset — এটাই চাকা-ব্যবহারকারীর সবচেয়ে দামি অংশ:
   - **Stairs or a step here?** → `Unknown` / `Yes` / `No`
   - **Ramp here?** → `Unknown` / `Yes` / `No`
   - **Surface** → `Unknown` / `Smooth` / `Uneven` / `Broken`
   > না জেনে গেলে **Unknown রাখুন**। সিস্টেমে estimate করা হয় না, আর route card-এ `Stairs/ramp/surface not measured here (unknown, not estimated)` দেখায়। ভুল `No` দেওয়াই বেশি ক্ষতিকর।
7. **Consent checkbox** (আনচেক করলে submit হয় না) — ছবি নিজের, মুখ/নম্বর প্লেট নেই, retention policy-তে সম্মতি
8. **Create report** বাটন → তৈরি হলে সরাসরি report page-এ নিয়ে যায়

**তারপর report page-এ:** ছবি যোগ করুন (ধাপ ২)। report তৈরির মুহূর্তে সেটা `Unverified` — এটা ঠিক আছে, human review না হওয়া পর্যন্ত তাকে claim ধরা হয়।

---

## ২. ছবি দিয়ে প্রমাণ যোগ করা

**ঠিকানা:** `/reports/{id}` → **Photo evidence** section।

1. **Choose a photo or drop it here** — JPEG / PNG / WebP, ১০ MB পর্যন্ত। টেনে এনে ফেলেও দিতে পারেন
2. **When was this photo taken? (optional)** — তারিখ+সময়। দিলে comparison time-based দাবি করতে পারে; না দিলে upload time ধরা হয়, আর ফর্ম নিজেই বলে: *"Capture time is your claim, not a verified fact."*
3. **Upload photo** → ছবি card-এ আসে
4. card-এর **Run analysis** বাটন → Meta AI (`✓ Live Meta AI`) এগুলো দেয়:
   - **Possible obstacle** type
   - **AI confidence** bar (৪০% / ৭০% এ dash — Low / Medium / High)
   - **Visible evidence** — মডেল কী কী দেখে বলেছে
   - **Limitations** — কী জানা যায় না
   - **Human review required** callout (হলুদ)
5. **Retry analysis** — ব্যর্থ হলে (server exact কারণ রাখে, বানিয়ে বলে না)

> **সীমাবদ্ধতা (participant-কে আগেই বলুন):** upload **ঘণ্টায় ১০টা** (H5 rate limit)। ১০টা ছবির বেশি পরীক্ষা করলে ৪২৯ আসে — সেটা মডেলের ভুল না, infrastructure-র throttle।

---

## ৩. একই জায়গার দুই ছবি তুলনা (সময়ের সাথে কী বদলালো)

**ঠিকানা:** report page → **Evidence explorer — compare over time**।

1. **Earlier photo** select → পুরোনো ছবি
2. **Later photo** select → নতুন ছবি
3. **Compare photos** বাটন
4. ফলাফল ৪টার একটা, explanation সহ:

| Outcome | মানে | যা **বোঝায় না** |
|---|---|---|
| `LikelyStillPresent` | বাধা সম্ভবত এখনো আছে | নিশ্চিত যে আছে |
| `NotObservedInNewImage` | নতুন ছবিতে দেখা যায়নি | **সরানো হয়নি** — angle/coverage-ও হতে পারে |
| `ConflictingEvidence` | দুই ছবি বিरोধিতা করে | কারো একটা ভুল — human দরকার |
| `Inconclusive` | যথেষ্ট প্রমাণ নেই | পথ পরিষ্কার |

> দুটো ছবি একই report-এর হতে হবে, আর দুটোরই analysis `Completed` দরকার। না হলে panel বলে: *"Two analysed photos are needed before a comparison can run."*

---

## ৪. Quick feedback — সবচেয়ে সহজ পথ (১ ট্যাপ)

**ঠিকানা:** যেকোনো report page → **Quick feedback — help keep this honest** panel।

৪টা বাটন, হুবহু এই লেখা:

| বাটনে লেখা | কখন ট্যাপবেন |
|---|---|
| **Still visible — obstacle is still there** | গিয়ে দেখেছেন বাধাটা এখনো আছে |
| **Not in new photo — I checked, it's not visible now** | গিয়ে দেখেছেন এখন নেই (তবুও "সরানো হয়েছে" দাবি করে না) |
| **Information wrong or old** | তথ্য ভুল, নাগাল নাই, বা অনেক পুরোনো |
| **Not sure** | নিশ্চিত নন — এটাও দামি তথ্য |

- চাইলে **Optional note (what you saw, when)** লিখুন — যেমন `Checked today 3pm, still blocked by scaffolding`
- ট্যাপ করলে message আসে: *"Thanks — your "<kind>" feedback was recorded. It does not auto-change verification; it will be reviewed."*

### এটা যে কাজ করছে — কোথায় দেখা যায়
| কোথায় | কী দেখা যায় |
|---|---|
| `/reviewer` → audit trail | `FeedbackStillVisible` / `FeedbackNotInNewPhoto` / `FeedbackWrongOrStale` entries, সময় + actor সহ |
| `/admin` → Pilot analytics | **Feedback: wrong/stale** stat tile |
| API | `GET /api/reports/feedback/summary` (reviewer) — সপ্তাহভিত্তিক কারণ অনুযায়ী grouped |

> **Design-সিদ্ধান্ত, bug না:** feedback ট্যাপ করলে report-এর verification status **বদলায় না**। একজন reviewer সিদ্ধান্ত নেন। এটা participant-কে বুঝিয়ে বলুন — না বুঝলে মনে করবেন "ট্যাপ কাজ করেনি"।
> লগইন ছাড়া ট্যাপ করলে actor খালি থাকে; চাইলে আগে `/reviewer`-এ এক ট্যাপে reviewer/admin login করে দিতে পারেন (panel-এ password ছাপা আছে) — তাহলে audit-এ নামও ওঠে।

---

## ৫. নিজের ভুল তথ্য সংশোধন

**ঠিকানা:** report page → উপরে-ডানে **Edit report** (`/reports/{id}/edit`)।

পারবেন: **Title**, **Description**, **Obstacle type**, **Severity**।
পারবেন না: **Location** (coordinate original claim-এর অংশ) — form নিজেই বলে: *"Location cannot be edited here; coordinates are part of the original claim."* আর verification status এখান থেকে বদলায় না, শুধু reviewer console থেকে।

---

## ৬. Map আর Route দিয়ে feedback-এর পরিপ্রেক্ষিত

- **`/map`** — `Obstacle type` + `Verification` filter, **Use my location**, দুটো switch: **Show cameras** আর **Coverage layer — where we have / do not have evidence**
  - Marker অক্ষর: `!` unverified · `V` human-verified · `?` disputed · `X` rejected · square `C` camera
  - Coverage cell: `A` evidence · `B` obstacle evidence · `C` **hatched = কেউ report করেনি** — "clear" না
  - ডান পাশের list আর map সবসময় একই report দেখায়; list-এর title-এ ক্লিক করলে selected report card খোলে
- **`/routes`** — From/To (টাইপ, address search, বা **Pick on map**), তারপর **Plan routes**
  - প্রতিটা route card-এ: দূরত্ব, সময়, **known obstacles**, **% unknown** bar, `Avoid stairs`/`Verified`/`Avoid stale`/`Shortest` preference chip
  - **Preferences** toggle করলে ranking বদলায়, কিন্তু কোনো known obstacle লুকোয় না

---

## ৭. যা এই UI-তে এখনো করা যায় না — honest list

Participant-কে ভুল দাবি করবেন না। এই বিল্ডে **নেই**:

| না-থাকার জিনিস | বর্তমান অবস্থা | তাই কী করবেন |
|---|---|---|
| **Reporter account / registration** | `AppUser` শুধু seeded staff account; public registration নেই | "My reports" list নেই — report page-এর URL (`/reports/<id>`) সেভ করে রাখুন |
| **Delete button** | `DELETE /api/reports/{id}` আছে কিন্তু শুধু signed-in, UI-তে কোনো বাটন নেই | মোছার অনুরোধ maintainer-কে জানান; consent text-এ এটাই লেখা আছে |
| **Route usability feedback** ("এই route-টা সত্যিই চলা যায়") | Quick feedback শুধু **report** নিয়ে, route নিয়ে না | paper/interview-এ নোট করুন (নিচের ফর্ম) |
| **In-UI contact channel** | নেই | session শেষে maintainer-এর email/ফোন paper-এ দিন |
| **বাংলা UI** | নেই (English only) | প্রতিটা screen-এ বাংলায় মানে বুঝিয়ে দিন; এই ফাইলটাই সেই স্ক্রিপ্ট |
| **Voice-first / screen-reader flow** | semantic HTML, `aria-live`, letter+colour dual encoding আছে; dedicated voice output নেই | screen reader দিয়ে test করে দেখুন, যা ভাঙে সেটা নোট করুন |

---

## ৮. Session চালানোর নিয়ম (observer-এর জন্য)

**Prep (৫ মিনিট)**
1. https://pathlens.cognitivebd.com/health/ready → তিনটা check `Healthy` কিনা দেখুন
2. participant-এর ফোনে/laptop-এ `/reports` খুলে একটা report সামনে রাখুন
3. consent form ছাপুন (নিচে) — **সম্মতি ছাড়া শুরু করবেন না**
4. সাহায্য করবেন না ২ মিনিট আটকে থাকলেও; ওরপর hint দিন, ক্লিক করে দেখিয়ে দিবেন না

**Task (১০ মিনিট)** — participant নিজের ভাষায় করবে, আপনি শুধু দেখবেন
1. "আপনার-route-এর একটা বাধা report করুন" → ধাপ ১
2. "সেটার একটা ছবি দিন আর analysis চালান" → ধাপ ২
3. "আজ গিয়ে দেখেছেন কী — নিচের ৪টা বাটনের একটা ট্যাপ করুন" → ধাপ ৪
4. "একটা route plan করুন; কোনটা বেছে নেবেন আর কেন?" → ধাপ ৬
5. "Route-এর 'unknown ৪০%' মানে কী?" → সঠিক উত্তর: *কেউ report করেনি, তাই পরিষ্কার প্রমাণিত না* — "নিরাপদ" হলে ভুল

**Interview (১০ মিনিট, consent থাকলে audio)**
- কোন তথ্যটা সিদ্ধান্ত নিতে সবচেয়ে কাজে লেগেছে?
- কোনটা বিভ্রান্ত করেছে?
- "unknown" দেখে আপনি কি route বদলাতেন?
- এটা কি আপনাকে **বেশি নির্ভর** করাবে? কী ভাবলে সেটা কমবে?
- সত্যিকারের যাত্রার আগে কী জানতে চান, যা এখানে পান না?

**Record** — প্রতিটা observation `docs/H8_WHEELCHAIR_USER_TEST.md`-এ যোগ করুন, **proxy না real participant — সেটা স্পষ্ট label করে**। আগের entry-তে ১টা proxy run আছে, real user এখনো নেই; সেটা যেন মিথ্যা না হয়।

---

## ৯. Route-usability-র জন্য paper ফর্ম (UI-তে জায়গা নেই, তাই কাগজে)

```
Participant (P1/P2 only, নাম প্রকাশ নয়): ____  Role: manual / electric / caregiver
Route দেখা:  card 1 / 2 / 3      বেছে নিলাম: ____
কেন নিলাম (নিজের কথায়): ______________________________________________
"unknown __%" বুঝলাম:  হ্যাঁ / না      যা বুঝলাম: ______________________
বাধা দেখা গেছে?  হ্যাঁ / না / জানি না    ধরন: BlockedSidewalk / BrokenSurface /
                                          MissingOrBlockedRamp / StairsOrStep / Other
ছবি আছে?  হ্যাঁ / না   (থাকলে app-এ upload করুন — ধাপ ২)
App যা বলেছিল আর বাস্তবে যা ছিল: ______________________________________
সবচেয়ে দরকারি তথ্য ছিল: __________   সবচেয়ে বিভ্রান্তিকর: __________
স্ক্রিন রিডার/বড় ফন্ট দিয়ে চালাতে সমস্যা:  নেই / আছে — কী: __________
আমি কি এটা সত্যিকারের যাত্রার আগে ব্যবহার করব?  হ্যাঁ / না — কেন: ________
```

---

## ১০. Consent template (কাগজে কপি করুন)

> আমি স্বেচ্ছায় PathLens AI-এর hallway test-এ অংশ নিচ্ছি। আমার স্ক্রিন/ইন্টারঅ্যাকশন demo-র জন্য রেকর্ড হতে পারে, কিন্তু লিখিত অনুমতি ছাড়া আমার নাম বা ছবি প্রকাশ করা হবে না। আমি যেকোনো সময় থামতে পারি। Contact: [organiser email]
> Participant signature: ______  Date: ______  Organiser: ______

---

## ১১. দ্রুত সমস্যা-নিরসরণ

| লক্ষণ | কারণ | কী করবেন |
|---|---|---|
| ছবি upload 429 দেয় | ঘণ্টায় ১০টা upload limit | ১ ঘণ্টা পর আবার, বা eval হলে `-UploadSpacingSeconds` |
| login 429 | মিনিটে ৫টা attempt | ১ মিনিট অপেক্ষা |
| analysis `Failed` | AI provider config নেই/timeout | `/health/ready`-তে `ai-provider` দেখুন; report page-এর **Retry analysis** |
| Compare বাটনে দুটো ছবি ধরা দেয় না | দুটোরই analysis `Completed` দরকার | আগে দুটোরই Run analysis |
| `Demo/mock analysis` লেখা | AI provider Mock mode | এটা real AI না — UI নিজেই স্পষ্ট label করে |
| map খালি | ওই viewport-এ report নেই | এটাই product-এর বার্তা: *unknown ≠ clear* |

---

**সংক্ষিপ্ত:** ব্যবহারকারীর ফিডব্যাকের আসল পথ = **report তৈরি → ছবি → Quick feedback-র ৪টা বাটন → প্রয়োজনে edit**। সব লগইন ছাড়া চলে, কোনোটাতেই verification সরাসরি বদলায় না (reviewer বদলায়), আর route-usability এখনো UI-তে যায় না — সেটা ধাপ ৯-এর কাগজে নোট করুন।
