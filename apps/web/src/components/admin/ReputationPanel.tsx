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
      if (!r.ok) {
        setMsg(`Reputation ${r.status}`);
        return;
      }
      setReps((await r.json()) as Rep[]);
    })();
  }, []);

  if (msg) {
    return (
      <section className="panel" aria-labelledby="reputation-heading">
        <div className="panel__head">
          <h2 id="reputation-heading">Reporter reputation</h2>
        </div>
        <div className="panel__body">
          <p role="alert" className="status-error">
            {msg}
          </p>
        </div>
      </section>
    );
  }

  if (!reps) {
    return (
      <div className="skeleton" aria-live="polite">
        <div className="sk-line sk-line--title" />
        <div className="sk-card" />
      </div>
    );
  }

  return (
    <section className="panel" aria-labelledby="reputation-heading">
      <div className="panel__head">
        <h2 id="reputation-heading">Reporter reputation</h2>
        <span className="panel__count">{reps.length} reporters</span>
      </div>
      <div className="panel__body">
        {reps.length === 0 ? (
          <p className="empty-state">
            <strong>No verified reporters yet.</strong>
            <span>Reputation starts at the first human-verified report — Bronze 1, Silver 3, Gold 5.</span>
          </p>
        ) : (
          <ul className="report-list">
            {reps.map((r) => (
              <li key={r.userId}>
                <div className="list-row__head">
                  <span className="list-row__title">{r.displayName}</span>
                  <span className={`pill pill--${r.badge.toLowerCase()}`}>{r.badge}</span>
                </div>
                <span className="list-row__meta">
                  {r.verifiedCount} verified · {r.note}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>
      <div className="panel__foot">
        Badges count verified reports only (Bronze 1+, Silver 3+, Gold 5+). A reviewer confirming a duplicate
        adds the incentive note — it never changes a report&rsquo;s evidence.
      </div>
    </section>
  );
}
