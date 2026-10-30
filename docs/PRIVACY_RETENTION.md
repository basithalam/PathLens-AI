# Privacy & Retention — PathLens AI (H5.2)

**Principle:** Evidence-aware means privacy-aware. No faces/plates are sent to AI under free credits.

## EXIF & Blur
- Backend `ImageDownscaler` strips EXIF (including GPS) before AI; `MetaImageAnalysisService` sends stripped JPEG.
- **Blur PoC (next):** Client `face-api.js` or backend `YoloDotNet` placeholder — not yet active. Current PoC: `ContentModeration.FlagNsfwPlaceholder` returns false; blur will run before storage when integrated. Until then, UI consent requires "no identifiable faces/plates" and free-tier data is not real-user faces.

## Consent
- `ReportForm.tsx` checkbox required: "I confirm this photo is my own, contains no identifiable faces/plates, and I consent to storage under retention policy."
- Without consent, browser blocks submit (HTML required).

## Retention
| Data | Default | Deletion | Note |
|---|---|---|---|
| Camera frames (raw) | 30d | Auto purge after 30d (future job) | Stored privately, not served as `GET /evidence/file` beyond report link |
| Evidence photos | 1y | `DELETE /api/reports/{id}` soft-deletes report + its evidence (hidden, not hard-deleted) | `IsDeleted` + `DeletedAt` on `accessibility_reports`, query filter excludes |
| Reports + audit | Until deleted | `DELETE /api/reports/{id}` (auth, soft) | GDPR: user can request via reviewer; hard delete after 1y via admin script (future) |
| Camera snapshots budget | 240/day | Not stored beyond 30d | `DailyAnalysisBudgetPerCamera` |

## GDPR
- **Right to erasure:** `DELETE /api/reports/{id}` (owner or reviewer) soft-deletes — `HasQueryFilter(r => !r.IsDeleted)` hides from lists/map/routes, but row + evidence files remain for audit until purge (1y). Hard delete requires DB admin (logged).
- **Access:** `GET /api/reports/{id}` + `/evidence` + `/feedback/summary` show stored data. User can export via those endpoints.
- **Object storage:** `IObjectStorage` local `.local/storage` (dev) or `/app/storage` (docker) — not public. `GET /api/evidence/{id}/file` streams only, never lists.

## EXIF Verify
- `ImageDownscaler` test: upload PNG with GPS EXIF → AI payload has no EXIF. Verify via `evaluation` image check (future).

## Next
- Integrate `face-api.js` client blur before `compressImageFile` (H5.2 PoC) or `YoloDotNet` backend.
- Nightly purge job for camera 30d + evidence 1y (cron in Worker).
