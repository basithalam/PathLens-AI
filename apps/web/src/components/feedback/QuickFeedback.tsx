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
    <section className="panel" id="quick-feedback-heading" aria-labelledby="quick-feedback-title">
      <div className="panel__head">
        <h2 id="quick-feedback-title">Quick feedback — help keep this honest</h2>
      </div>
      <div className="panel__body">
        <p className="section__note">
          Your tap does not instantly verify or reject this report. It creates a review note with time and
          actor; a reviewer decides later. “Not in new photo” does not prove the obstacle was removed.
        </p>
        <div className="queue-actions">
          {KINDS.map((k) => (
            <button
              key={k.value}
              type="button"
              onClick={() => send(k.value)}
              disabled={!!busy}
              className={`btn btn--ghost btn--sm${k.value === "WrongOrStale" ? " btn--danger" : ""}`}
            >
              {busy === k.value ? "Sending…" : k.label}
            </button>
          ))}
        </div>
        <label className="field">
          Optional note (what you saw, when)
          <textarea
            value={note}
            onChange={(e) => setNote(e.target.value)}
            maxLength={2000}
            rows={2}
            placeholder="e.g., Checked today 3pm, still blocked by scaffolding"
          />
        </label>
        {message && (
          <p role="status" className="notice">
            {message}
          </p>
        )}
      </div>
    </section>
  );
}
