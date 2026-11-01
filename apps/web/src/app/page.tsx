import Link from "next/link";
import { getHealth } from "@/lib/api-client";
import { StatusBadge } from "@/components/StatusBadge";

export const dynamic = "force-dynamic";

export default async function HomePage() {
  const [live, ready] = await Promise.all([getHealth("live"), getHealth("ready")]);
  const database = ready?.checks.find((check) => check.name === "database");
  const aiProvider = ready?.checks.find((check) => check.name === "ai-provider");

  return (
    <>
      <section className="hero">
        <div className="hero__badge">
          <span className="hero__dot" aria-hidden="true" /> Meta Global AI Hackathon 2026 • San Francisco Pilot • Live AI
        </div>
        <h1 className="hero__title">
          Know before you go — <span>is this path usable?</span>
        </h1>
        <p className="hero__lede">
          PathLens AI turns pedestrian photos into evidence. It compares what was seen yesterday with what is seen today, explains confidence honestly, and never pretends unknown means clear.
        </p>
        <div className="hero__actions">
          <Link href="/map" className="btn btn--primary">Explore Live Map →</Link>
          <Link href="/report/new" className="btn btn--ghost">Report Obstacle</Link>
          <Link href="/routes" className="btn btn--ghost">Plan Route</Link>
        </div>
        <div style={{ display: "flex", gap: "1.5rem", marginTop: "1.5rem", flexWrap: "wrap", fontSize: ".88rem", color: "var(--muted)" }}>
          <span>✓ <strong>25</strong> SF evidence</span>
          <span>✓ <strong>92</strong> tests pass</span>
          <span>✓ <strong>Live</strong> Meta AI</span>
          <span>✓ <strong>Honest</strong> unknown</span>
        </div>
        <div className="hero__trust">
          <div className="hero__trust-card">
            <div style={{ fontSize: "1.5rem", marginBottom: ".5rem" }}>🔍</div>
            <strong>Evidence-aware</strong>
            <span>Every claim shows age, source, verification. Unknown stays unknown.</span>
          </div>
          <div className="hero__trust-card">
            <div style={{ fontSize: "1.5rem", marginBottom: ".5rem" }}>👥</div>
            <strong>Human in loop</strong>
            <span>Reviewers verify with audit trail. AI never auto-verifies.</span>
          </div>
          <div className="hero__trust-card">
            <div style={{ fontSize: "1.5rem", marginBottom: ".5rem" }}>🛡️</div>
            <strong>Honest design</strong>
            <span>Not visible ≠ removed. No reports ≠ clear.</span>
          </div>
        </div>
      </section>

      <section style={{ margin: "2.5rem 0", textAlign: "center" }}>
        <h2 style={{ fontSize: "1.5rem", fontWeight: 800, marginBottom: ".5rem" }}>How it works — 3 steps</h2>
        <p style={{ color: "var(--muted)", maxWidth: "60ch", margin: "0 auto 1.5rem" }}>From photo to trusted route — in 2 minutes.</p>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(250px, 1fr))", gap: "1rem", textAlign: "left" }}>
          <div style={{ background: "var(--surface)", border: "1px solid var(--line)", borderRadius: "var(--radius)", padding: "1.25rem", textAlign: "center" }}>
            <div style={{ width: "48px", height: "48px", borderRadius: "50%", background: "var(--brand-ghost)", display: "grid", placeItems: "center", margin: "0 auto .75rem", fontWeight: 800, color: "var(--brand-dark)" }}>1</div>
            <h3 style={{ margin: "0 0 .5rem", fontSize: "1rem", fontWeight: 800 }}>Capture Evidence</h3>
            <p style={{ margin: 0, fontSize: ".9rem", color: "var(--muted)" }}>Take a photo. AI describes what is visible — with confidence and limitations.</p>
          </div>
          <div style={{ background: "var(--surface)", border: "1px solid var(--line)", borderRadius: "var(--radius)", padding: "1.25rem", textAlign: "center" }}>
            <div style={{ width: "48px", height: "48px", borderRadius: "50%", background: "var(--accent-light)", display: "grid", placeItems: "center", margin: "0 auto .75rem", fontWeight: 800, color: "var(--accent)" }}>2</div>
            <h3 style={{ margin: "0 0 .5rem", fontSize: "1rem", fontWeight: 800 }}>Compare Over Time</h3>
            <p style={{ margin: 0, fontSize: ".9rem", color: "var(--muted)" }}>Old vs new from same place → Likely still there, Not observed, Conflicting.</p>
          </div>
          <div style={{ background: "var(--surface)", border: "1px solid var(--line)", borderRadius: "var(--radius)", padding: "1.25rem", textAlign: "center" }}>
            <div style={{ width: "48px", height: "48px", borderRadius: "50%", background: "var(--warn-ghost)", display: "grid", placeItems: "center", margin: "0 auto .75rem", fontWeight: 800, color: "var(--warn)" }}>3</div>
            <h3 style={{ margin: "0 0 .5rem", fontSize: "1rem", fontWeight: 800 }}>Route with Truth</h3>
            <p style={{ margin: 0, fontSize: ".9rem", color: "var(--muted)" }}>See 3 routes ranked by obstacles and unknown share.</p>
          </div>
        </div>
      </section>

      <section className="feature-grid" aria-label="Key capabilities">
        <Link href="/map" className="feature-card">
          <div className="feature-card__icon" aria-hidden="true">🗺️</div>
          <h3>Live Accessibility Map</h3>
          <p>Explore SF with real reports. See coverage gaps and tap markers for details.</p>
          <span className="feature-card__link">Open Live Map →</span>
        </Link>
        <Link href="/routes" className="feature-card">
          <div className="feature-card__icon" aria-hidden="true">🧭</div>
          <h3>Smart Route Planner</h3>
          <p>Get 3 routes compared by evidence. Avoid stairs, prefer fresh.</p>
          <span className="feature-card__link">Plan Smart Route →</span>
        </Link>
        <Link href="/reports" className="feature-card">
          <div className="feature-card__icon" aria-hidden="true">📸</div>
          <h3>Evidence Reports</h3>
          <p>Browse real photos, AI analysis, and human reviews.</p>
          <span className="feature-card__link">Browse Evidence →</span>
        </Link>
      </section>

      <section aria-labelledby="system-status-heading" className="status-panel">
        <h2 id="system-status-heading">Live System Status</h2>
        <p className="status-source">Real-time from PathLens API — not cached, not mocked.</p>
        {live === null ? (
          <div style={{ textAlign: "center", padding: "2rem", background: "var(--bad-ghost)", borderRadius: "var(--radius-sm)", border: "1px solid #fecaca" }}>
            <div style={{ fontSize: "2rem", marginBottom: ".5rem" }}>🔴</div>
            <p role="alert" style={{ margin: 0, fontWeight: 700, color: "var(--bad)" }}>API offline — start backend at localhost:5145</p>
          </div>
        ) : (
          <dl className="status-list">
            <div style={{ background: live.status === "Healthy" ? "var(--success-ghost)" : "var(--warn-ghost)", borderColor: live.status === "Healthy" ? "#a7f3d0" : "#fde68a" }}>
              <dt>✅ API Liveness</dt>
              <dd><StatusBadge status={live.status} /></dd>
            </div>
            <div style={{ background: ready?.status === "Healthy" ? "var(--success-ghost)" : "var(--warn-ghost)", borderColor: ready?.status === "Healthy" ? "#a7f3d0" : "#fde68a" }}>
              <dt>⚡ API Readiness</dt>
              <dd>{ready ? <StatusBadge status={ready.status} /> : <StatusBadge status="Unknown" />}</dd>
            </div>
            <div style={{ background: database?.status === "Healthy" ? "var(--success-ghost)" : "var(--bad-ghost)", borderColor: database?.status === "Healthy" ? "#a7f3d0" : "#fecaca" }}>
              <dt>🗄️ Database</dt>
              <dd>{database ? <><StatusBadge status={database.status} /> <span style={{ fontSize: ".85rem", color: "var(--muted)" }}>{database.description}</span></> : <span>Not reported yet</span>}</dd>
            </div>
            {aiProvider && (
              <div style={{ background: aiProvider.status === "Healthy" ? "var(--success-ghost)" : "var(--warn-ghost)", borderColor: aiProvider.status === "Healthy" ? "#a7f3d0" : "#fde68a" }}>
                <dt>🤖 AI Vision (Meta)</dt>
                <dd><StatusBadge status={aiProvider.status} /> <span style={{ fontSize: ".85rem", color: "var(--muted)" }}>{aiProvider.description}</span></dd>
              </div>
            )}
          </dl>
        )}
      </section>
    </>
  );
}
