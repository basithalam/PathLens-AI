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
      <h1>Accessibility reports</h1>
      <p>
        Reports are user claims about possible obstacles. A report is not a verified
        fact until a reviewer checks it. <Link href="/report/new">Report an obstacle</Link>
      </p>

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
            {result.totalCount} report{result.totalCount === 1 ? "" : "s"} found. Page {result.page}.
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
          <nav aria-label="Pagination" className="pagination">
            {page > 1 && <Link href={`/reports?page=${page - 1}`}>Previous</Link>}
            {result.items.length === 10 && <Link href={`/reports?page=${page + 1}`}>Next</Link>}
          </nav>
        </>
      )}
    </>
  );
}
