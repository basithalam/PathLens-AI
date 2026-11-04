import Link from "next/link";

export default function ManualPage() {
  return (
    <>
      <div className="page-header">
        <div className="page-header__eyebrow">Professional user manual</div>
        <h1>How to use PathLens AI</h1>
        <p>
          One manual for every role — public user, reviewer and admin. Every claim shows its age,
          source and verification. Unknown stays unknown; no route is ever labelled safe.
        </p>
        <div className="page-header__actions">
          <a href="/docs/user-manual.html" className="btn btn--primary btn--sm" target="_blank" rel="noreferrer">
            Open full manual (A report · B route · C review · D admin)
          </a>
          <Link href="/map" className="btn btn--ghost btn--sm">
            Start on the map
          </Link>
        </div>
      </div>

      <section className="panel" aria-labelledby="manual-start">
        <div className="panel__head">
          <h2 id="manual-start">1. Start here — who needs an account?</h2>
        </div>
        <div className="panel__body">
          <div className="table-wrap">
            <table className="modern-table">
              <thead>
                <tr>
                  <th scope="col">What you do</th>
                  <th scope="col">Where</th>
                  <th scope="col">Login?</th>
                </tr>
              </thead>
              <tbody>
                <tr><td>Browse map, reports, routes</td><td><code>/map</code> · <code>/reports</code> · <code>/routes</code></td><td>No</td></tr>
                <tr><td>Report, photo, AI analysis, compare, quick feedback</td><td><code>/report/new</code> and any report page</td><td>No</td></tr>
                <tr><td>Verify / dispute / reject, audit trail</td><td><code>/reviewer</code></td><td>Yes — Reviewer card</td></tr>
                <tr><td>Camera registry, city analytics, reputation</td><td><code>/admin</code></td><td>Yes — Admin card</td></tr>
              </tbody>
            </table>
          </div>
          <p className="hint">
            Reviewer page shows Reviewer + User cards; admin page shows Admin only.
            Demo logins: <code>reviewer@pathlens.local / Reviewer@123</code> ·{" "}
            <code>admin@pathlens.local / Admin@123</code> · <code>user@pathlens.local / User@123</code>.
          </p>
        </div>
      </section>

      <section className="panel" aria-labelledby="manual-public">
        <div className="panel__head">
          <h2 id="manual-public">2. Public user — no account needed</h2>
        </div>
        <div className="panel__body">
          <h3>Accessibility map (<code>/map</code>)</h3>
          <ul className="list-bullets">
            <li>Pan or zoom — reports reload for the area you look at. No marker means unknown, not accessible.</li>
            <li>Letters: <strong>!</strong> Unverified · <strong>V</strong> HumanVerified · <strong>?</strong> Disputed · <strong>X</strong> Rejected · square <strong>C</strong> camera.</li>
            <li><strong>Coverage</strong> grid: grey C = insufficient, amber B = obstacle, green A = evidence.</li>
          </ul>
          <h3>Report (<code>/report/new</code> or + Report)</h3>
          <ul className="list-bullets">
            <li>Title, obstacle type, severity, location via GPS / map pin / typed coordinates.</li>
            <li>Stairs / ramp / surface: leave <strong>Unknown</strong> when not checked.</li>
            <li>Tick consent, <strong>Create report</strong>. Arrives Unverified, openly.</li>
          </ul>
          <h3>Photo + AI, compare, feedback</h3>
          <ul className="list-bullets">
            <li>Upload photo → <strong>Run analysis</strong>: <strong>Live Meta AI · muse-spark-1.3</strong>, confidence bar, visible evidence, limitations, share (sam-3.1, in-photo only).</li>
            <li>Two photos → <strong>Compare photos</strong>: <code>LikelyStillPresent · NotObservedInNewImage · ConflictingEvidence · Inconclusive</code>. Never means removed.</li>
            <li>Quick taps create a review note — never auto-verify. Edit at <code>/reports/id/edit</code>.</li>
          </ul>
          <h3>Routes (<code>/routes</code>)</h3>
          <ul className="list-bullets">
            <li>Start + destination → <strong>Plan routes</strong>. Ranked by fewest known obstacles: known obstacles, <strong>unknown %</strong>, Certain vs Uncertain, stale, hasConfirmedStairs.</li>
          </ul>
          <p role="note" className="callout callout--warn">
            <strong>OSRM foot routing is not wheelchair-validated. No route is ever labelled wheelchair-safe.</strong>
          </p>
        </div>
      </section>

      <section className="panel" aria-labelledby="manual-reviewer">
        <div className="panel__head">
          <h2 id="manual-reviewer">3. Reviewer console (<code>/reviewer</code>)</h2>
        </div>
        <div className="panel__body">
          <ul className="list-bullets">
            <li>One-tap <strong>Reviewer</strong> sign in. The <strong>User</strong> card proves a plain user gets 403.</li>
            <li>Decide with a note: <strong>HumanVerified · Disputed · Rejected</strong> — stored beside AI output, never overwriting it.</li>
            <li><strong>Recent audit trail</strong> shows actor, previous to new, note, time.</li>
          </ul>
        </div>
      </section>

      <section className="panel" aria-labelledby="manual-admin">
        <div className="panel__head">
          <h2 id="manual-admin">4. Admin console (<code>/admin</code>)</h2>
        </div>
        <div className="panel__body">
          <ul className="list-bullets">
            <li>Only the <strong>Admin</strong> card. Other roles see an Admin-only 403 notice.</li>
            <li><strong>Camera registry:</strong> permission note required to enable. <strong>Poll now</strong> — unchanged creates nothing; changed becomes evidence or auto <strong>CameraDetected candidate</strong> for the same Meta AI queue. Stale flagged.</li>
            <li><strong>City analytics</strong> and <strong>Reputation</strong>. An admin never edits a verdict here.</li>
          </ul>
        </div>
      </section>
    </>
  );
}
