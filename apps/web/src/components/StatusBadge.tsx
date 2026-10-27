export function StatusBadge({ status }: { status: string }) {
  const normalized = status.toLowerCase();
  const className =
    normalized === "healthy"
      ? "badge badge-ok"
      : normalized === "unhealthy"
        ? "badge badge-bad"
        : "badge badge-warn";

  return <span className={className}>{status}</span>;
}
