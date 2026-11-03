type Tone = "ok" | "warn" | "bad" | "info" | "unknown" | "neutral";

const TONE: Record<string, Tone> = {
  // system health
  healthy: "ok",
  degraded: "warn",
  unhealthy: "bad",
  // report verification — the four outcomes a reviewer can record
  humanverified: "ok",
  disputed: "warn",
  rejected: "bad",
  unverified: "unknown",
  // analysis jobs
  completed: "ok",
  failed: "bad",
  processing: "info",
  pending: "unknown",
  // evidence comparison outcomes
  likelystillpresent: "warn",
  conflictingevidence: "bad",
  // "not seen again" is deliberately not green: absence of sighting is not removal
  notobservedinnewimage: "unknown",
  inconclusive: "unknown",
  unknown: "unknown",
};

function label(status: string): string {
  return status
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/_/g, " ")
    .replace(/^./, (c) => c.toUpperCase());
}

/** Colour plus shape: an unknown state is hatched with a hollow marker, never a solid badge. */
export function StatusBadge({ status }: { status: string }) {
  const tone = TONE[status.toLowerCase()] ?? "neutral";

  return (
    <span
      className={tone === "neutral" ? "badge badge-neutral" : `badge badge-${tone}`}
      title={status}
    >
      {label(status)}
    </span>
  );
}
