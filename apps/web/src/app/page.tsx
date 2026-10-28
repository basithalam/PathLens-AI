import Link from "next/link";
import { getHealth } from "@/lib/api-client";
import { StatusBadge } from "@/components/StatusBadge";

export const dynamic = "force-dynamic";

export default async function HomePage() {
  const [live, ready] = await Promise.all([getHealth("live"), getHealth("ready")]);
  const database = ready?.checks.find((check) => check.name === "database");

  return (
    <>
      <section className="intro">
        <h1>Can this path be used right now — and how sure are we?</h1>
        <p className="lede">
          PathLens AI collects photo evidence of pedestrian obstacles, compares
          older and newer evidence from the same place, and explains how fresh
          and trustworthy that information is. It is being built for the Meta
          Global AI Developer Hackathon 2026.
        </p>
        <p className="home-actions">
          <Link href="/map" className="primary-button">
            Explore the map
          </Link>{" "}
          <Link href="/report/new">Report an obstacle</Link>{" "}
          <Link href="/reports">Browse reports</Link>{" "}
          <Link href="/routes">Plan a route</Link>
        </p>
        <p className="limitations">
          Limitations, stated up front: an obstacle that is not visible in a
          newer photo has not been proven removed, and a route with no reports
          is not a proven clear route.
        </p>
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
