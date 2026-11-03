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

      <section className="pipeline-wrap" aria-labelledby="pipeline-heading">
        <h2 id="pipeline-heading" className="section-title">
          How a claim becomes trustworthy
        </h2>
        <p className="section-sub">
          Each stage keeps its own record. Nothing later in the chain overwrites what came before it.
        </p>
        <ol className="pipeline">
          <li>
            <span className="pipeline__step">1</span>
            <strong>Someone reports a place</strong>
            <p>
              A claim with coordinates, obstacle type and severity — marked <em>Unverified</em> from the
              first second.
            </p>
          </li>
          <li>
            <span className="pipeline__step">2</span>
            <strong>Photo evidence is added</strong>
            <p>
              Uploaded by a person or captured by a camera feed, stored privately and served only through the
              API.
            </p>
          </li>
          <li>
            <span className="pipeline__step">3</span>
            <strong>Meta AI reads the photo</strong>
            <p>
              It returns possible obstacle types, the evidence it saw, its confidence and its limitations —
              never a verdict.
            </p>
          </li>
          <li>
            <span className="pipeline__step">4</span>
            <strong>Evidence is compared over time</strong>
            <p>
              Older against newer: likely still present, not observed again, conflicting, or inconclusive —
              always with an explanation.
            </p>
          </li>
          <li>
            <span className="pipeline__step">5</span>
            <strong>A human reviews it</strong>
            <p>
              Verify, dispute or reject with a note. The decision is stored beside the AI outcome, not instead
              of it.
            </p>
          </li>
          <li>
            <span className="pipeline__step">6</span>
            <strong>Routes use what is known</strong>
            <p>
              Ranking weighs obstacles, freshness and verification. The remaining share stays{" "}
              <em>unknown</em>, never “clear”.
            </p>
          </li>
        </ol>
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
        <Link href="/reviewer" className="feature-card">
          <div className="feature-card__icon" aria-hidden="true">
            ⚖
          </div>
          <h3>Review Console</h3>
          <p>
            Verify, dispute or reject with a note. Every decision keeps the previous value in the audit
            trail.
          </p>
          <span className="feature-card__link">Open the queue →</span>
        </Link>
        <Link href="/admin" className="feature-card">
          <div className="feature-card__icon" aria-hidden="true">
            ▣
          </div>
          <h3>Cameras &amp; Pilot Ops</h3>
          <p>Registered public feeds poll on a cadence; a stale feed is flagged instead of trusted.</p>
          <span className="feature-card__link">Operations →</span>
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
