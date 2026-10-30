"use client";

import { useEffect, useState } from "react";
import { authFetch } from "@/lib/auth";

type Stats = {
  totalReports: number;
  staleReports: number;
  stalePercent: number;
  unverified: number;
  verified: number;
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

  if (msg) return <p className="status-error">{msg}</p>;
  if (!stats) return <p className="status-source">Loading stats…</p>;

  const max = Math.max(1, ...stats.reportsPerDayLast7.map(d => d.count));
  return (
    <div className="card" style={{ marginTop: "1rem" }}>
      <h3>Analytics Dashboard (SF pilot)</h3>
      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(140px, 1fr))", gap: 12 }}>
        <div className="hero__trust-card"><strong>Total reports</strong><span>{stats.totalReports}</span></div>
        <div className="hero__trust-card"><strong>Stale &gt;30d</strong><span>{stats.staleReports} ({stats.stalePercent}%)</span></div>
        <div className="hero__trust-card"><strong>Verified</strong><span>{stats.verified} ({stats.verifiedPercent}%)</span></div>
        <div className="hero__trust-card"><strong>Unknown% map heuristic</strong><span>{stats.unknownPercentMapHeuristic}%</span></div>
        <div className="hero__trust-card"><strong>Feedback total</strong><span>{stats.feedbackTotal} (Wrong/Stale {stats.feedbackWrongOrStale})</span></div>
      </div>
      <h4 style={{ marginTop: 16 }}>Reports per day (last 7)</h4>
      <div style={{ display: "flex", alignItems: "end", gap: 6, height: 80 }}>
        {stats.reportsPerDayLast7.length === 0 ? (
          <span className="status-source">No reports last 7 days</span>
        ) : (
          stats.reportsPerDayLast7.map(d => (
            <div key={d.date} style={{ flex: 1, textAlign: "center" }}>
              <div style={{ height: `${(d.count / max) * 60 + 8}px`, background: "var(--accent)", borderRadius: 4 }} title={`${d.date}: ${d.count}`} />
              <div style={{ fontSize: "0.7rem", marginTop: 4 }}>{new Date(d.date).toLocaleDateString(undefined, { month: "short", day: "numeric" })}</div>
              <div style={{ fontSize: "0.75rem" }}>{d.count}</div>
            </div>
          ))
        )}
      </div>
      <p className="status-source">Feedback grouped weekly by cause — see GET /api/reports/feedback/summary</p>
    </div>
  );
}
