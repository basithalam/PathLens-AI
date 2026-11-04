# Third-Party Materials & Licences

Every asset in this repository that is not written by us, what it is used for, the licence it carries, and
**which obligations are still open**. The project's own code and docs are MIT (`LICENSE`); nothing here
relicences a third-party asset.

Verified 2026-11-03 by reading the installed package manifests, the package licences on disk, and the
source call sites — not from memory.

## 1. Summary of open obligations

| # | Obligation | Status |
|---|---|---|
| 1 | Per-file attribution for the borrowed street photos in `evaluation/sf-real-eval/` | **DONE** — nine shipped images each credited with author, licence and source page, recovered by byte-identical SHA-256 match (§6, and the `sourcePage`/`author`/`license` columns in `manifest.sf-real-eval.csv`). The tenth file could not be traced and was **deleted on 2026-11-04** rather than shipped uncredited |
| 2 | Reproduce the Leaflet BSD-2-Clause copyright notice in documentation | **DONE** — §5 |
| 3 | Keep the OpenStreetMap attribution control visible on every map | **DONE** — present on all three maps (§4) |
| 4 | Honour the Nominatim usage policy (rate + identification) | **PARTIAL — documented, not enforced in code** (§4) |
| 5 | Confirm the terms of the two camera-feed providers we hotlink | **OPEN** — unverified (§7) |
| 6 | Disclose that `SixLabors.ImageSharp` carries known advisories suppressed by `NoWarn` | **DONE** — `docs/SECURITY.md` |

## 2. Code dependencies

Versions read from `apps/web/package.json`, the `backend/src/*/*.csproj` files and the installed
`node_modules/*/package.json`.

**Frontend (`apps/web`)**

| Package | Version | Licence | Used for |
|---|---|---|---|
| `next` | 15.5.8 | MIT | App Router, SSR/PPR |
| `react` / `react-dom` | 19.1.0 | MIT | UI |
| `leaflet` | 1.9.4 | BSD-2-Clause (notice in §5) | Maps, markers, tile layer, attribution control |
| `typescript`, `eslint`, `eslint-config-next`, `@types/*` | dev only | MIT | Typecheck and lint |

There is **no CSS framework and no analytics package** in this project — the dependency list above is the
complete `dependencies` block, and all styling is hand-written in `apps/web/src/app/globals.css`.

**Backend**

| Package | Version | Licence | Used for |
|---|---|---|---|
| .NET 10 (`sdk:10.0` → `aspnet:10.0`, `backend/Dockerfile:4,20`) | 10 | MIT | Runtime |
| `Microsoft.EntityFrameworkCore` (+ `Design`, `Relational`) | 10.0.14 | MIT | Persistence |
| `Npgsql.EntityFrameworkCore.PostgreSQL` (+ `.NetTopologySuite`) | 10.0.3 | PostgreSQL License | Npgsql provider |
| `NetTopologySuite` (transitive via the provider above) | resolved by the provider | BSD-3-Clause | Geometry for the lat/lon fallback path |
| `SixLabors.ImageSharp` | 3.1.8 | **Six Labors Split License v1.0** — used under its **Apache-2.0 branch** | EXIF stripping + downscale |
| `System.IdentityModel.Tokens.Jwt`, `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.23.0 / 10.0.14 | MIT | Reviewer/Admin auth |
| `xunit`, `xunit.runner.visualstudio` | 2.9.3 / 3.1.4 | Apache-2.0 | Test framework |
| `coverlet.collector`, `Microsoft.NET.Test.Sdk`, `Microsoft.AspNetCore.Mvc.Testing` | 6.0.4 / 17.14.1 / 10.0.14 | MIT | Coverage and test host |

**ImageSharp licence, stated precisely.** The package ships `Six Labors Split License v1.0`, which grants
Apache-2.0 when the consumer "is consuming the Work in for use in software licensed under an Open Source or
Source Available license". PathLens is distributed under MIT, so the Apache-2.0 branch applies and no
commercial licence is required. `requireLicenseAcceptance` is `true` on this package, so the licence text
should be kept with any redistributed build.

**Why the two advisories are still suppressed — measured 2026-11-04, not assumed.** "Just upgrade" does not
work here, so the build was run against each candidate:

| Version | Result |
|---|---|
| 3.1.8 (in use) | `NU1902` + `NU1903` suppressed by `<NoWarn>` in `PathLens.Infrastructure.csproj`; build reports 0 warnings / 0 errors |
| 3.1.12 | **Worse** — six advisories instead of two (`GHSA-gwg2-r3hj-4w44`, `j3p4-wp97-rph4`, `j9gm-c75j-xc9q`, `jjfr-hcj7-qf5w`, `v76p-62qx-wwq2`, `wmxv-xphr-5c9g`), 60 build warnings |
| 4.1.3 | No advisories at all, but **the build fails**: `No Six Labors license found. Set $(SixLaborsLicenseKey)…` — v4 demands a licence file, which for this project means the commercial branch of the same split licence |

So the honest state is a pinned version with two disclosed advisories, not a clean build. The suppression is
recorded in `docs/SECURITY.md`, `docs/TEST_PLAN.md` and here, and the reason now sits as a comment inside the
`.csproj` so the next reader does not repeat the experiment.

## 3. Data and imagery we wrote ourselves

| Asset | Origin |
|---|---|
| `apps/web/src/**`, `backend/src/**`, `scripts/**`, `docs/**` | This project (MIT) |
| `evaluation/sf-dataset/` (25 images) | Generated placeholders, not photographs. Their run used the Mock provider — pipeline proof only, never accuracy |
| Map marker artwork for reports/cameras | Drawn in this project as CSS/inline SVG (`.map-marker-pin`, `.map-camera-pin`, `.coverage-letter-inner`) — no icon set imported |

## 4. Hosted services

| Service | Where | Terms we must honour | State |
|---|---|---|---|
| **OpenStreetMap raster tiles** `tile.openstreetmap.org/{z}/{x}/{y}.png` | `AccessibilityMap.tsx:263`, `RoutePlanner.tsx:107`, `MiniMapPicker.tsx:24` | Data © OpenStreetMap contributors, **ODbL 1.0**; public tile server is for light use | Attribution control set on all three maps. Demo traffic is human-scale |
| **Nominatim** `nominatim.openstreetmap.org/search` | `RoutePlanner.tsx:323` and `:360` — called **from the browser** on Enter | Usage policy: ≤1 request/s, a valid identifying User-Agent, attribution, no bulk geocoding | **Not enforced in code.** One request per keypress, and a browser cannot set the UA header, so compliance rests on human-scale use rather than a control. Self-hosting or a proxy is the fix if this ever leaves demo scale |
| **OSRM demo router** `router.project-osrm.org` | `OsrmRoutingService.cs`, probed by `OsrmHealthCheck.cs` | Free demo instance, no SLA; routed data is ODbL | We send `PathLensAI/0.1 (Meta Global AI Developer Hackathon 2026 prototype)` as User-Agent and the repo URL as Referer on both the router and the health probe |
| **Meta Model API** `api.meta.ai/v1` | `MetaAiSceneUnderstandingService` (`/v1/chat/completions`, `muse-spark-1.3`) and `Sam31SceneMeasurementService` (`/v1/responses`, `sam-3.1`) | Meta developer terms; billed per call | Key lives in GitHub Secrets only. Model output is shown as model output, with provider and model named in the UI and in the API payload |

SAM 3.1 is **off by default** (`Sam31:Enabled=false`); turning it on adds two billed calls per analysed
photo. On the live demo it is enabled through the `SAM31_ENABLED` deployment secret.

## 5. Leaflet default images — BSD-2-Clause notice

`apps/web/public/` ships `marker-icon.png`, `marker-icon-2x.png`, `marker-shadow.png`, `layers.png`,
`layers-2x.png`. These are the default assets from the Leaflet package, and BSD-2-Clause requires the
copyright notice to be reproduced in the distribution's documentation, so it is reproduced here:

> BSD 2-Clause License
>
> Copyright (c) 2010-2023, Volodymyr Agafonkin
> Copyright (c) 2010-2011, CloudMade
> All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided the
two conditions of the BSD-2-Clause licence (retain the notice in source; reproduce it in binary
documentation) are met. The full text is in `node_modules/leaflet/LICENSE`.

## 6. The borrowed street photographs — attribution ledger

`evaluation/sf-real-eval/` holds **nine** San Francisco street photographs downloaded from **Wikimedia
Commons**. They are *not* covered by this project's MIT licence, and each keeps its own terms. A tenth file,
`sf-eval-09.jpg`, was deleted on 2026-11-04 because its source could not be recovered — see below.

**Correction to what this file said before:** it described all ten as CC BY-SA 4.0. That was wrong — the
licences differ per file (CC0, CC BY 2.0, CC BY 4.0 and one CC BY-SA 4.0), and only the CC BY-SA item
carries a share-alike obligation. The per-file table below is now the authority.

**How the sources were recovered.** The credit is not stored inside the files — every `Copyright` string in
them is colour-profile boilerplate (`Copyright 1999 Adobe Systems Incorporated`, `Copyright Apple Inc.,
2017`), with no `Artist`, source URL or licence field. What *is* recoverable is geometry: all ten are
exactly **960 px wide**, which is the standard Commons thumbnail width, so each file is a 960px render of a
Commons original. Searching Commons by scene, pre-filtering candidates to the same aspect ratio, downloading
each candidate's 960px thumbnail and comparing **SHA-256** identified **9 of the 10 byte-for-byte**. Those
nine rows below are therefore not guesses — the bytes are identical to the file served by that Commons page.

| File | Commons file | Author as credited | Licence | Attribution required |
|---|---|---|---|---|
| `sf-eval-01.jpg` | [File:Sidewalk in San Francisco.jpg](https://commons.wikimedia.org/wiki/File:Sidewalk_in_San_Francisco.jpg) | Alex Sirac | **CC BY 4.0** | yes |
| `sf-eval-02.jpg` | [File:Stockton street sidewalk.jpg](https://commons.wikimedia.org/wiki/File:Stockton_street_sidewalk.jpg) | Aatu Dorochenko | **CC BY-SA 4.0** | yes, and share-alike applies to this image |
| `sf-eval-03.jpg` | [File:Sidewalk plantings - San Francisco, CA.jpg](https://commons.wikimedia.org/wiki/File:Sidewalk_plantings_-_San_Francisco%2C_CA.jpg) | Daderot | **CC0** | no — public domain dedication |
| `sf-eval-04.jpg` | [File:Uneven, two lines - Mission St San Francisco.jpg](https://commons.wikimedia.org/wiki/File:Uneven%2C_two_lines_-_Mission_St_San_Francisco.jpg) | Marcin Wichary | **CC BY 2.0** | yes |
| `sf-eval-05.jpg` | [File:LimeBike scooters - August 2025 - Sarah Stierch.jpg](https://commons.wikimedia.org/wiki/File:LimeBike_scooters_-_August_2025_-_Sarah_Stierch.jpg) | Missvain (Sarah Stierch) | **CC0** | no — public domain dedication |
| `sf-eval-06.jpg` | [File:Castro Street, San Francisco, March 17, 2020.jpg](https://commons.wikimedia.org/wiki/File:Castro_Street%2C_San_Francisco%2C_March_17%2C_2020.jpg) | Christopher Michel | **CC BY 2.0** | yes |
| `sf-eval-07.jpg` | [File:Market Street, San Francisco, California April 2022 - Sidewalk homelessness and drugs.jpg](https://commons.wikimedia.org/wiki/File:Market_Street%2C_San_Francisco%2C_California_April_2022_-_Sidewalk_homelessness_and_drugs.jpg) | Sharon Hahn Darlin | **CC BY 2.0** | yes — and see the privacy note below |
| `sf-eval-08.jpg` | [File:North Beach, San Francisco (14731005196).jpg](https://commons.wikimedia.org/wiki/File:North_Beach%2C_San_Francisco_(14731005196).jpg) | Christopher Michel | **CC BY 2.0** | yes |
| `sf-eval-09.jpg` | **deleted 2026-11-04** | — | — | no source found; file removed from the repository |
| `sf-eval-10.jpg` | [File:San Francisco (17073829244).jpg](https://commons.wikimedia.org/wiki/File:San_Francisco_(17073829244).jpg) | Rodrigo Paredes (Ciudad Autónoma de Buenos Aires, Argentina) | **CC BY 2.0** | yes |

The same data now lives in `evaluation/manifest.sf-real-eval.csv` as `sourcePage` / `author` / `license`
columns, so the dataset and this ledger cannot drift. Verified parsing with `Import-Csv`: 9 rows, 11
columns, every row populated.

**Correct attribution form.** Reuse of these images must name the author, link the source page, state the
licence, and note that the file was modified — ours was resized to 960 px and had EXIF stripped before
storage. `sf-eval-03` and `sf-eval-05` are CC0 and need nothing, but crediting them anyway is harmless and
is what the table above supports.

**`sf-eval-09.jpg` was deleted on 2026-11-04, not quietly kept.** Nine of the ten files matched a Commons
thumbnail byte-for-byte; this one did not match anything in the pool searched (18 queries, 99 aspect-ratio
candidates). An uncredited CC-licensed file is a licence breach, so the file is gone from the repository and
from the manifest. What is deliberately **not** rewritten is `evaluation/results.sf-real-eval.json` — that is
the machine record of the 2026-11-01 run, which covered ten images of which two failed at upload. The honest
sentence is: *the run covered ten borrowed photos; the set now ships nine, all nine credited.*

The live demo still shows the report `Demo evidence 09 - no photo uploaded (rejected)`. That is correct and
is kept on purpose: it is the rejection path on screen, its description records that the source could not be
recovered and the file was removed, and no image is served for it.

**Privacy note.** `sf-eval-07` contains identifiable faces. It is CC BY 2.0, which permits reuse, but the
report built on it was deliberately **never marked verified** — see `docs/AI_EVALUATION_SF_REAL.md`.

## 7. Camera feeds

Four camera rows exist on the live demo. Their imagery is fetched server-side and stored as evidence, so
the source terms matter:

| Row | Feed | Assessment |
|---|---|---|
| `SF US-101 At Octavia St - Datumfeed Live` | `datumfeed.com/api/cameras/cal-4-tv301us101atoctaviast/frame` | Publicly listed Caltrans corridor camera reached through a third-party aggregator. **Aggregator terms unverified.** Polling is disabled on live (`CAMERAS_POLLING_ENABLED=false`), so no frames are being pulled |
| `SF Market St - Demo` | `cwwp2.dot.ca.gov/data/d4/cctv/image/132.jpg` | Caltrans District 4 CCTV, a public agency feed. **Terms not read.** This is the camera behind the honest "the frame is a night highway, not a footpath" comparison in `docs/JUDGE_GUIDE.md` |
| `Market St & 5th — SF Demo` | `datumfeed.com/api/cameras/SF_001/image` | Example row created through the admin UI; the URL is not a confirmed live endpoint |
| `SF Market St - Demo` | `via.placeholder.com/640x480.jpg` | **A placeholder image service, not traffic imagery.** Kept to show the admin form's failure path; it is labelled as such wherever it appears |

No camera frame is presented as a community submission, and camera-sourced reports are marked uncertain in
the queue. Before any real deployment, each feed's terms must be read and recorded here.

## 8. Fonts

`globals.css:93-94` names `"Inter"` and `"JetBrains Mono"` at the head of two font stacks. **Neither font is
bundled, embedded, or fetched** — there is no `next/font` import, no `@font-face` rule and no webfont
request anywhere in `apps/web/src`. They take effect only on a machine that already has them installed, and
everyone else gets the system stack. There is no font licence obligation in this repository.

## 9. Not used

No Bootstrap/Tailwind/MUI or any other component library, no CDN scripts, no analytics or tracking, no
Google Fonts, no Playwright or browser automation, no third-party icon set. The `react-leaflet` wrapper is
**not** used — Leaflet is called directly through `L.tileLayer`/`L.map` in the three map components.
