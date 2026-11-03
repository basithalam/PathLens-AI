"use client";

import { useEffect, useState } from "react";
import { authFetch } from "@/lib/auth";

type Stats = {
  totalReports: number;
  staleReports: number;
  unverified: number;
  verified: number;
  stalePercent: number;
  verifiedPercent: number;
  reportsPerDayLast7: { date: string; count: number }[];
  unknownPercentMapHeuristic: number;
  feedbackTotal: number;
  feedbackWrongOrStale: number;
};

export function AdminStatsPanel() {
  const [stats, setStats] = useState<Stats | null>(null);
  const [msg, setMsg] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      const r = await authFetch("/api/admin/stats");
      if (!r.ok) {
        setMsg(`Stats ${r.status}: ${(await r.text()).slice(0, 300)}`);
        return;
      }
      setStats((await r.json()) as Stats);
    })();
  }, []);

  if (msg) {
    return (
      <section className="panel" aria-labelledby="stats-heading">
        <div className="panel__head">
          <h2 id="stats-heading">Pilot analytics</h2>
        </div>
        <div className="panel__body">
          <p role="alert" className="status-error">
            {msg}
          </p>
        </div>
      </section>
    );
  }

  if (!stats) {
    return (
      <div className="skeleton" aria-live="polite">
        <div className="sk-line sk-line--title" />
        <div className="sk-card" />
      </div>
    );
  }

  const max = Math.max(1, ...stats.reportsPerDayLast7.map((d) => d.count));

  return (
    <section className="panel" aria-labelledby="stats-heading">
      <div className="panel__head">
        <h2 id="stats-heading">Pilot analytics — San Francisco</h2>
        <span className="panel__count">live from <code>GET /api/admin/stats</code></span>
      </div>
      <div className="panel__body">
        <div className="stat-grid">
          <div className="stat">
            <span className="stat__label">Total reports</span>
            <span className="stat__value">{stats.totalReports}</span>
            <span className="stat__sub">all sources</span>
          </div>
          <div className="stat stat--ok">
            <span className="stat__label">Human-verified</span>
            <span className="stat__value">{stats.verifiedPercent}%</span>
            <span className="stat__sub">
              {stats.verified} of {stats.totalReports}
            </span>
          </div>
          <div className="stat stat--warn">
            <span className="stat__label">Stale &gt; 30 d</span>
            <span className="stat__value">{stats.stalePercent}%</span>
            <span className="stat__sub">{stats.staleReports} reports</span>
          </div>
          <div className="stat">
            <span className="stat__label">Unknown share (map)</span>
            <span className="stat__value">{stats.unknownPercentMapHeuristic}%</span>
            <span className="stat__sub">no nearby evidence — not “clear”</span>
          </div>
          <div className="stat stat--bad">
            <span className="stat__label">Feedback: wrong/stale</span>
            <span className="stat__value">{stats.feedbackWrongOrStale}</span>
            <span className="stat__sub">of {stats.feedbackTotal} taps</span>
          </div>
        </div>

        <div className="stack-sm">
          <h3>Reports per day (last 7)</h3>
          {stats.reportsPerDayLast7.length === 0 ? (
            <p className="empty-state">
              <strong>No reports in the last 7 days.</strong>
              <span>Seed the demo city with ./scripts/seed-sf.ps1 to populate this chart.</span>
            </p>
          ) : (
            <div className="chart" role="img" aria-label="Reports per day for the last 7 days">
              {stats.reportsPerDayLast7.map((d) => (
                <div key={d.date} className="chart__col">
                  <span className="chart__value">{d.count}</span>
                  <span
                    className="chart__bar"
                    style={{ height: `${Math.max(6, (d.count / max) * 72)}px` }}
                    title={`${d.date}: ${d.count}`}
                  />
                  <span className="chart__label">
                    {new Date(d.date).toLocaleDateString(undefined, { month: "short", day: "numeric" })}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
      <div className="panel__foot">
        Feedback is grouped weekly by cause — see <code>GET /api/reports/feedback/summary</code>. A feedback
        tap never changes verification on its own.
      </div>
    </section>
  );
}
