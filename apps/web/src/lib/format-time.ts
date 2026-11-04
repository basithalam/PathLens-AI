/**
 * All timestamps are stored as UTC on the server. Judging happens on US Eastern
 * (deadline Nov 4, 9PM ET), so every user-visible time renders in America/New_York
 * with an explicit ET label — never the viewer's local zone, never a bare UTC string.
 */
export const DISPLAY_TIME_ZONE = "America/New_York";
export const DISPLAY_TIME_ZONE_LABEL = "ET";

export function formatDateTimeET(value: string | null | undefined): string {
  if (!value) return "—";
  return new Date(value).toLocaleString("en-US", {
    timeZone: DISPLAY_TIME_ZONE,
    year: "numeric",
    month: "numeric",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
    second: "2-digit",
    hour12: true,
  }) + ` ${DISPLAY_TIME_ZONE_LABEL}`;
}

export function formatDateET(value: string | null | undefined): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString("en-US", { timeZone: DISPLAY_TIME_ZONE }) +
    ` ${DISPLAY_TIME_ZONE_LABEL}`;
}
