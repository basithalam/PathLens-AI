import Link from "next/link";
import { getHealth } from "@/lib/api-client";
import { StatusBadge } from "@/components/StatusBadge";

export const dynamic = "force-dynamic";

export default async function HomePage() {
  const [live, ready] = await Promise.all([getHealth("live"), getHealth("ready")]);
  const database = ready?.checks.find((check) => check.name === "database");

  return (
    <>
      <section className="hero">
        <div className="hero__badge">
          <span className="hero__dot" aria-hidden="true" /> Meta Global AI Hackathon 2026 · San Francisco pilot
        </div>
        <h1 className="hero__title">
          Can this path be used <span>right now</span> — and how sure are we?
        </h1>
        <p className="hero__lede">
          PathLens AI collects photo evidence of pedestrian obstacles, compares older and newer evidence from the same
          place, and explains how fresh and trustworthy that information is. No false sense of safety.
        </p>
        <div className="hero__actions">
          <Link href="/map" className="btn btn--primary">
            Explore the map <span aria-hidden="true">→</span>
          </Link>
          <Link href="/report/new" className="btn btn--ghost">
            Report an obstacle
          </Link>
          <Link href="/routes" className="btn btn--ghost">
            Plan a route
          </Link>
        </div>
        <div className="hero__trust">
          <div className="hero__trust-card">
            <strong>Evidence-aware</strong>
            <span>Every claim shows age, source, and verification. Unknown stays unknown.</span>
          </div>
          <div className="hero__trust-card">
            <strong>Human in the loop</strong>
            <span>Reviewers verify, dispute, or reject — with a full audit trail.</span>
          </div>
          <div className="hero__trust-card">
            <strong>Honest by design</strong>
            <span>Not visible ≠ removed. No reports ≠ clear.</span>
          </div>
        </div>
      </section>

      <section className="feature-grid" aria-label="Key capabilities">
        <Link href="/map" className="feature-card">
          <div className="feature-card__icon" aria-hidden="true">
            ◉
          </div>
          <h3>Accessibility Map</h3>
          <p>OSM + Leaflet with coverage grid (C/B/A). Click a cell to zoom and filter reports.</p>
          <span className="feature-card__link">Open map →</span>
        </Link>
        <Link href="/routes" className="feature-card">
          <div className="feature-card__icon" aria-hidden="true">
            ↝
          </div>
          <h3>Route Planner</h3>
          <p>OSRM foot routing ranked by known obstacles, verification, and unknown share.</p>
          <span className="feature-card__link">Plan a route →</span>
        </Link>
        <Link href="/reports" className="feature-card">
          <div className="feature-card__icon" aria-hidden="true">
            ◎
          </div>
          <h3>Reports & Evidence</h3>
          <p>Photo timeline, AI comparison, and feedback that never auto-verifies.</p>
          <span className="feature-card__link">Browse reports →</span>
        </Link>
      </section>

      <section aria-labelledby="system-status-heading" className="status-panel">
        <h2 id="system-status-heading">System status</h2>
        <p className="status-source">
          Live from the PathLens API. If the API is stopped, this panel says so
          instead of guessing.
        </p>

        {live === null ? (
          <p role="alert" className="status-error">
            The PathLens API is not reachable right now. Start the backend and
            refresh this page.
          </p>
        ) : (
          <dl className="status-list">
            <div>
              <dt>API liveness</dt>
              <dd>
                <StatusBadge status={live.status} />
              </dd>
            </div>
            <div>
              <dt>API readiness</dt>
              <dd>
                {ready ? (
                  <StatusBadge status={ready.status} />
                ) : (
                  <StatusBadge status="Unknown" />
                )}
              </dd>
            </div>
            <div>
              <dt>PostgreSQL database</dt>
              <dd>
                {database ? (
                  <>
                    <StatusBadge status={database.status} />{" "}
                    <span>{database.description}</span>
                  </>
                ) : (
                  <span>Not reported by the API yet.</span>
                )}
              </dd>
            </div>
          </dl>
        )}
      </section>
    </>
  );
}
