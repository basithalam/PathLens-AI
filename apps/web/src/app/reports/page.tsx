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

const PAGE_SIZE = 10;

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

function pageWindow(current: number, total: number): number[] {
  const start = Math.max(1, Math.min(current - 2, total - 4));
  const end = Math.min(total, Math.max(current + 2, start + 4));
  return Array.from({ length: end - start + 1 }, (_, i) => start + i);
}

export default async function ReportsPage({ searchParams }: ReportsPageProps) {
  const params = await searchParams;
  const page = Math.max(1, Number(params.page ?? 1) || 1);
  const result = await getReports({
    search: params.search,
    obstacleType: params.obstacleType,
    page,
    pageSize: PAGE_SIZE,
  });

  const totalCount = result?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const filtering = Boolean(params.search || params.obstacleType);

  return (
    <>
      <div className="page-header">
        <div className="page-header__eyebrow">Community reports</div>
        <h1>Accessibility reports</h1>
        <p>
          Every row is a claim about a place, not a verified fact. A report only becomes trustworthy after
          evidence and a human review — and a place with no report here is unknown, not clear.
        </p>
        <div className="page-header__actions">
          <Link href="/report/new" className="btn btn--primary btn--sm">
            Report an obstacle
          </Link>
          {filtering ? (
            <Link href="/reports" className="btn btn--ghost btn--sm">
              Clear filters
            </Link>
          ) : null}
        </div>
      </div>

      <form method="get" className="filter-bar" aria-label="Filter reports">
        <label>
          Search
          <input
            name="search"
            defaultValue={params.search ?? ""}
            placeholder="Title or description"
            aria-label="Search reports by title or description"
          />
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
        <button type="submit" className="btn btn--primary">
          Apply filters
        </button>
      </form>

      {result === null ? (
        <p role="alert" className="status-error">
          The PathLens API is not reachable, so reports cannot be loaded. Start the backend and refresh.
        </p>
      ) : result.items.length === 0 ? (
        <p className="empty-state">
          <strong>No report matches this filter.</strong>
          <span>
            That is a gap in the data, not proof that the area is accessible.{" "}
            <Link href="/report/new">Be the first to report</Link> what you see.
          </span>
        </p>
      ) : (
        <section className="panel" aria-labelledby="reports-list-heading">
          <div className="panel__head">
            <h2 id="reports-list-heading">Reports</h2>
            <span className="panel__count">
              page {result.page} / {totalPages}
            </span>
          </div>
          <div className="panel__body">
            <p className="hint">
              {result.totalCount} report{result.totalCount === 1 ? "" : "s"} found · {PAGE_SIZE} per page —
              a live camera adds about one report a minute, so the list stays paginated instead of loading
              everything.
            </p>
            <ul className="report-list">
              {result.items.map((report) => (
                <li key={report.id}>
                  <div className="list-row__head">
                    <Link href={`/reports/${report.id}`} className="list-row__title">
                      {report.title}
                    </Link>
                    <StatusBadge status={report.verificationStatus} />
                  </div>
                  <div className="list-row__meta">
                    <span className="meta-chip">
                      Type <strong>{report.obstacleType}</strong>
                    </span>
                    <span className="meta-chip">
                      Severity <strong>{report.severity}</strong>
                    </span>
                    <span className="meta-chip">
                      {new Date(report.createdAt).toLocaleString()}
                    </span>
                    <span className="meta-chip mono">{report.id.slice(0, 8)}</span>
                  </div>
                </li>
              ))}
            </ul>
          </div>
          <div className="panel__foot">
            {result.items.length} shown on this page. Verification happens in the{" "}
            <Link href="/reviewer">reviewer console</Link>, never from this list.
          </div>
        </section>
      )}

      {result && result.totalCount > PAGE_SIZE ? (
        <nav aria-label="Pagination" className="pager">
          {page > 1 ? (
            <Link href={buildPageHref(params, page - 1)} className="btn btn--ghost btn--sm">
              ← Previous
            </Link>
          ) : null}
          {pageWindow(page, totalPages).map((p) => (
            <Link
              key={p}
              href={buildPageHref(params, p)}
              className={p === page ? "btn btn--primary btn--sm" : "btn btn--ghost btn--sm"}
              aria-current={p === page ? "page" : undefined}
            >
              {p}
            </Link>
          ))}
          {page < totalPages ? (
            <Link href={buildPageHref(params, page + 1)} className="btn btn--ghost btn--sm">
              Next →
            </Link>
          ) : null}
          <span className="pager__total">{result.totalCount} total</span>
        </nav>
      ) : null}
    </>
  );
}
