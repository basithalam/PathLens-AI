"use client";

import { useState } from "react";
import { submitFeedback } from "@/lib/api-client";

const KINDS = [
  { value: "StillVisible", label: "Still visible — obstacle is still there" },
  { value: "NotInNewPhoto", label: "Not in new photo — I checked, it's not visible now" },
  { value: "WrongOrStale", label: "Information wrong or old" },
  { value: "NotSure", label: "Not sure" },
] as const;

export function QuickFeedback({ reportId }: { reportId: string }) {
  const [note, setNote] = useState("");
  const [busy, setBusy] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  async function send(kind: string) {
    setBusy(kind);
    setMessage(null);
    const result = await submitFeedback(reportId, { kind, note: note.trim() || null });
    setBusy(null);
    if (result.ok) {
      setMessage(`Thanks — your “${kind}” feedback was recorded. It does not auto-change verification; it will be reviewed.`);
      setNote("");
    } else setMessage(result.message);
  }

  return (
    <section aria-labelledby="quick-feedback-heading" style={{ border: "1px solid var(--line)", borderRadius: 10, padding: "1rem", background: "var(--wash)", marginTop: "1.5rem" }}>
      <h2 id="quick-feedback-heading">Quick feedback — help keep this honest</h2>
      <p style={{ fontSize: "0.92rem", color: "var(--muted)" }}>
        Your tap does not instantly verify or reject this report. It creates a review note with time and actor; a reviewer decides later. “Not in new photo” does not prove the obstacle was removed.
      </p>
      <div style={{ display: "grid", gap: "0.5rem", marginTop: "0.8rem" }}>
        {KINDS.map(k => (
          <button key={k.value} type="button" onClick={() => send(k.value)} disabled={!!busy} className="primary-button" style={{ textAlign: "left" }}>
            {busy === k.value ? "Sending…" : k.label}
          </button>
        ))}
      </div>
      <label style={{ display: "grid", gap: 4, marginTop: 12 }}>
        Optional note (what you saw, when)
        <textarea value={note} onChange={e => setNote(e.target.value)} maxLength={2000} rows={2} placeholder="e.g., Checked today 3pm, still blocked by scaffolding" />
      </label>
      {message && <p role="status" style={{ marginTop: 8, background: "var(--paper)", padding: "0.5rem 0.7rem", borderRadius: 6 }}>{message}</p>}
    </section>
  );
}
