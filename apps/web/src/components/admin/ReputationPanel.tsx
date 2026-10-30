"use client";

import { useEffect, useState } from "react";
import { authFetch } from "@/lib/auth";

type Rep = { userId: string; displayName: string; verifiedCount: number; badge: string; note: string };

export function ReputationPanel() {
  const [reps, setReps] = useState<Rep[] | null>(null);
  const [msg, setMsg] = useState<string | null>(null);
  useEffect(() => {
    (async () => {
      const r = await authFetch("/api/admin/reputation");
      if (!r.ok) { setMsg(`Reputation ${r.status}`); return; }
      setReps((await r.json()) as Rep[]);
    })();
  }, []);
  if (msg) return <p className="status-error">{msg}</p>;
  if (!reps) return <p className="status-source">Loading reputation…</p>;
  if (reps.length === 0) return <p className="status-source">No verified reporters yet — be first (Bronze at 1 verified, Silver 3, Gold 5). Duplicate confirm adds note for reputation.</p>;
  return (
    <div className="card" style={{ marginTop: "1rem" }}>
      <h3>Reporter Reputation (H6.4 PoC)</h3>
      <ul className="report-list">
        {reps.map(r => (
          <li key={r.userId}><strong>{r.displayName}</strong> — {r.verifiedCount} verified <span className={`pill pill--${r.badge.toLowerCase()}`}>{r.badge}</span></li>
        ))}
      </ul>
      <p className="status-source">Badge: Bronze 1+, Silver 3+, Gold 5+ verified reports. Reviewer duplicate confirm note includes +1 incentive.</p>
    </div>
  );
}
