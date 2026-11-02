import Link from "next/link";
import { getReports } from "@/lib/api-client";
import { StatusBadge } from "@/components/StatusBadge";

export const dynamic = "force-dynamic";

const OBSTACLE_TYPES = [
  "BlockedSidewalk",
  "BrokenSurface",
  "MissingOrBlockedRamp",
  "StairsOrStep",
  "ConstructionBarrier",
  "Other",
  "Unknown",
];

interface ReportsPageProps {
  searchParams: Promise<{ search?: string; obstacleType?: string; page?: string }>;
}

function buildPageHref(
  baseParams: { search?: string; obstacleType?: string },
  page: number,
): string {
  const sp = new URLSearchParams();
  if (baseParams.search) sp.set("search", baseParams.search);
  if (baseParams.obstacleType) sp.set("obstacleType", baseParams.obstacleType);
  sp.set("page", String(page));
  return `/reports?${sp.toString()}`;
}

export default async function ReportsPage({ searchParams }: ReportsPageProps) {
  const params = await searchParams;
  const page = Math.max(1, Number(params.page ?? 1) || 1);
  const result = await getReports({
    search: params.search,
    obstacleType: params.obstacleType,
    page,
    pageSize: 10,
  });

  return (
    <>
      <div className="page-header">
        <div className="page-header__eyebrow">Community reports</div>
        <h1>Accessibility reports</h1>
        <p>
          Reports are user claims about possible obstacles. A report is not a verified fact until a reviewer checks it.
          <Link href="/report/new"> Report an obstacle</Link>
        </p>
      </div>

      <form method="get" className="filter-bar" aria-label="Filter reports">
        <label>
          Search
          <input name="search" defaultValue={params.search ?? ""} placeholder="Title or description" />
        </label>
        <label>
          Obstacle type
          <select name="obstacleType" defaultValue={params.obstacleType ?? ""}>
            <option value="">All types</option>
            {OBSTACLE_TYPES.map((type) => (
              <option key={type} value={type}>
                {type}
              </option>
            ))}
          </select>
        </label>
        <button type="submit">Apply filters</button>
      </form>

      {result === null ? (
        <p role="alert" className="status-error">
          The PathLens API is not reachable, so reports cannot be loaded. Start the
          backend and refresh.
        </p>
      ) : result.items.length === 0 ? (
        <p>No reports match yet. Be the first to report an obstacle.</p>
      ) : (
        <>
          <p aria-live="polite">
            {result.totalCount} report{result.totalCount === 1 ? "" : "s"} found. Page {result.page} of {Math.ceil(result.totalCount / 10)} — live camera 1/min adds fast, pagination keeps load fast.
          </p>
          <ul className="report-list">
            {result.items.map((report) => (
              <li key={report.id}>
                <Link href={`/reports/${report.id}`}>{report.title}</Link>{" "}
                <StatusBadge status={report.verificationStatus} />
                <br />
                <span>
                  {report.obstacleType} · Severity {report.severity} ·{" "}
                  {new Date(report.createdAt).toLocaleString()}
                </span>
              </li>
            ))}
          </ul>
          <nav aria-label="Pagination" className="pagination" style={{ display: "flex", gap: 8, flexWrap: "wrap", alignItems: "center" }}>
            {page > 1 && <Link href={buildPageHref(params, page - 1)} className="btn btn--ghost">← Previous</Link>}
            {Array.from({ length: Math.ceil(result.totalCount / 10) }, (_, i) => i + 1)
              .slice(Math.max(0, page - 3), Math.min(Math.ceil(result.totalCount / 10), page + 2))
              .map((p) => (
                <Link key={p} href={buildPageHref(params, p)} className={p === page ? "btn btn--primary" : "btn btn--ghost"} style={{ minWidth: 36, justifyContent: "center" }}>
                  {p}
                </Link>
              ))}
            {page < Math.ceil(result.totalCount / 10) && <Link href={buildPageHref(params, page + 1)} className="btn btn--ghost">Next →</Link>}
            <span style={{ fontSize: "0.82rem", color: "var(--muted)" }}>{result.totalCount} total · 10/page</span>
          </nav>
        </>
      )}
    </>
  );
}
