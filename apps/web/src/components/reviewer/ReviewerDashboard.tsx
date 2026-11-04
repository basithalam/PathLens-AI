"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { authFetch, clearSession, getSessionUser, type SessionUser } from "@/lib/auth";
import { SignInPanel, SessionBar } from "@/components/auth/SignInPanel";
import { StatusBadge } from "@/components/StatusBadge";

interface QueueReport {
  id: string;
  title: string;
  obstacleType: string;
  verificationStatus: string;
  createdAt: string;
  isCameraCandidate: boolean;
  possibleDuplicateOfReportId: string | null;
}

interface StaleCamera {
  id: string;
  label: string;
  city: string;
  provider: string;
  lastSuccessAt: string | null;
  lastError: string | null;
}

interface QueueComparison {
  id: string;
  reportId: string;
  effectiveOutcome: string;
  createdAt: string;
}

interface Queue {
  reports: QueueReport[];
  comparisons: QueueComparison[];
  staleCameras: StaleCamera[];
}

interface AuditEvent {
  id: string;
  entityType: string;
  entityId: string;
  action: string;
  previousValue: string | null;
  newValue: string | null;
  note: string | null;
  createdAt: string;
}

const REVIEW_ACTIONS = ["HumanVerified", "Disputed", "Rejected"] as const;

const ACTION_CLASS: Record<string, string> = {
  HumanVerified: "btn btn--brand btn--sm",
  Disputed: "btn btn--ghost btn--sm",
  Rejected: "btn btn--danger btn--sm",
};

function formatStamp(value: string): string {
  return new Date(value).toLocaleString();
}

export function ReviewerDashboard() {
  const [user, setUser] = useState<SessionUser | null>(null);
  const [queue, setQueue] = useState<Queue | null>(null);
  const [audit, setAudit] = useState<AuditEvent[]>([]);
  const [message, setMessage] = useState<string | null>(null);
  const [notes, setNotes] = useState<Record<string, string>>({});

  const loadQueue = useCallback(async () => {
    const [queueResponse, auditResponse] = await Promise.all([
      authFetch("/api/reviewer/queue"),
      authFetch("/api/reviewer/audit?limit=30"),
    ]);

    if (queueResponse.status === 401 || queueResponse.status === 403) {
      setMessage("Your session is not a reviewer session. Log in with a reviewer account.");
      return;
    }

    if (queueResponse.ok) {
      setQueue((await queueResponse.json()) as Queue);
    }
    if (auditResponse.ok) {
      setAudit((await auditResponse.json()) as AuditEvent[]);
    }
  }, []);

  useEffect(() => {
    const existing = getSessionUser();
    if (existing) {
      setUser(existing);
      void loadQueue();
    }
  }, [loadQueue]);

  function onLogout() {
    clearSession();
    setUser(null);
    setQueue(null);
    setAudit([]);
    setMessage(null);
  }

  async function decideDuplicate(reportId: string, action: "confirm" | "reject") {
    const response = await authFetch(`/api/reports/${reportId}/duplicate/${action}`, {
      method: "POST",
      body: JSON.stringify({ note: notes[reportId] || null }),
    });

    if (response.ok) {
      setMessage(
        action === "confirm"
          ? "Duplicate confirmed. The report is now a pointer to the main report; nothing was deleted."
          : "Duplicate suggestion rejected. Both reports stay independent.",
      );
      await loadQueue();
    } else {
      setMessage(`Duplicate decision failed (${response.status}).`);
    }
  }

  async function reviewReport(reportId: string, status: string) {
    const note = notes[reportId] ?? "";
    const response = await authFetch(`/api/reports/${reportId}/review`, {
      method: "POST",
      body: JSON.stringify({ verificationStatus: status, note: note || null }),
    });

    if (response.ok) {
      setMessage(`Report marked ${status}. The change is in the audit trail below.`);
      await loadQueue();
    } else {
      setMessage(`Review failed (${response.status}).`);
    }
  }

  if (!user) {
    return (
      <SignInPanel
        defaultRole="Reviewer"
        visibleRoles="reviewer"
        heading="Reviewer access"
        onSignedIn={async (next) => { setUser(next); await loadQueue(); }}
      />
    );
  }

  const reports = queue?.reports ?? [];
  const comparisons = queue?.comparisons ?? [];
  const staleCameras = queue?.staleCameras ?? [];

  return (
    <>
      <SessionBar
        user={user}
        onLogout={onLogout}
        hint="Queue and audit trail reload after every decision."
      />

      {message && (
        <p role="status" className="notice">
          {message}
        </p>
      )}

      <section className="panel" aria-labelledby="queue-reports-heading">
        <div className="panel__head">
          <h2 id="queue-reports-heading">Reports waiting for review</h2>
          <span className="panel__count">{queue === null ? "…" : reports.length}</span>
        </div>
        <div className="panel__body">
          {queue === null ? (
            <div className="skeleton" aria-live="polite">
              <div className="sk-card" />
              <div className="sk-card" />
            </div>
          ) : reports.length === 0 ? (
            <p className="empty-state">
              <strong>No unverified reports in the queue.</strong>
              <span>New community reports appear here before they influence any route.</span>
            </p>
          ) : (
            <div className="stack">
              {reports.map((report) => (
                <article
                  key={report.id}
                  className={`queue-card${report.isCameraCandidate ? " queue-card--camera" : ""}${
                    report.possibleDuplicateOfReportId ? " queue-card--duplicate" : ""
                  }`}
                >
                  <div className="queue-card__head">
                    <div className="queue-card__title">
                      <Link href={`/reports/${report.id}`}>{report.title}</Link>
                      <span className="queue-card__sub">
                        <span className="mono">{report.id.slice(0, 8)}</span> · {report.obstacleType} ·
                        reported {formatStamp(report.createdAt)}
                      </span>
                    </div>
                    <StatusBadge status={report.verificationStatus} />
                  </div>

                  {report.isCameraCandidate && (
                    <p className="callout callout--info">
                      <strong>CameraDetected candidate — unverified.</strong> A camera suggested this;
                      only your review can verify it.
                    </p>
                  )}

                  {report.possibleDuplicateOfReportId && (
                    <div className="notice notice--warn">
                      <strong>Possible duplicate</strong> of{" "}
                      <Link href={`/reports/${report.possibleDuplicateOfReportId}`}>
                        this nearby report
                      </Link>
                      . Confirming deletes nothing: the report becomes a pointer and keeps its evidence
                      and audit history.
                      <span className="queue-actions" style={{ marginTop: ".5rem" }}>
                        <button
                          type="button"
                          className="btn btn--brand btn--sm"
                          onClick={() => decideDuplicate(report.id, "confirm")}
                        >
                          Confirm merge
                        </button>
                        <button
                          type="button"
                          className="btn btn--ghost btn--sm"
                          onClick={() => decideDuplicate(report.id, "reject")}
                        >
                          Reject suggestion
                        </button>
                      </span>
                    </div>
                  )}

                  <div className="queue-card__body">
                    <label className="queue-card__note">
                      Reviewer note
                      <input
                        value={notes[report.id] ?? ""}
                        onChange={(event) =>
                          setNotes((current) => ({ ...current, [report.id]: event.target.value }))
                        }
                        placeholder="What did you check, and where?"
                      />
                    </label>
                    <div className="queue-actions">
                      <span className="queue-actions__label">Record decision</span>
                      {REVIEW_ACTIONS.map((action) => (
                        <button
                          key={action}
                          type="button"
                          className={ACTION_CLASS[action]}
                          onClick={() => reviewReport(report.id, action)}
                        >
                          {action === "HumanVerified" ? "Verify" : action === "Disputed" ? "Dispute" : "Reject"}
                        </button>
                      ))}
                    </div>
                  </div>
                </article>
              ))}
            </div>
          )}
        </div>
        <div className="panel__foot">
          A decision writes an audit event and changes the effective status — the original report text and
          every earlier decision stay readable.
        </div>
      </section>

      <section className="panel" aria-labelledby="queue-comparisons-heading">
        <div className="panel__head">
          <h2 id="queue-comparisons-heading">Conflicting comparisons</h2>
          <span className="panel__count">{queue === null ? "…" : comparisons.length}</span>
        </div>
        <div className="panel__body">
          {queue === null ? (
            <p className="sk-line sk-line--sm" aria-live="polite" />
          ) : comparisons.length === 0 ? (
            <p className="empty-state">
              <strong>No conflicting comparisons waiting.</strong>
              <span>Contradicting evidence is escalated here, never silently averaged.</span>
            </p>
          ) : (
            <ul className="report-list">
              {comparisons.map((comparison) => (
                <li key={comparison.id}>
                  <div className="list-row__head">
                    <span className="list-row__title">
                      <Link href={`/reports/${comparison.reportId}`}>Report {comparison.reportId.slice(0, 8)}</Link>
                    </span>
                    <StatusBadge status={comparison.effectiveOutcome} />
                  </div>
                  <span className="list-row__meta">Compared {formatStamp(comparison.createdAt)}</span>
                  <span className="hint">
                    Open the evidence explorer to see both photos side by side and record a decision.
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>

      <section className="panel" aria-labelledby="queue-cameras-heading">
        <div className="panel__head">
          <h2 id="queue-cameras-heading">Stale cameras</h2>
          <span className="panel__count">{queue === null ? "…" : staleCameras.length}</span>
        </div>
        <div className="panel__body">
          {queue === null ? (
            <p className="sk-line sk-line--sm" aria-live="polite" />
          ) : staleCameras.length === 0 ? (
            <p className="empty-state">
              <strong>No enabled camera is stale right now.</strong>
              <span>A stale feed keeps its history but stops claiming to be current.</span>
            </p>
          ) : (
            <div className="stack-sm">
              {staleCameras.map((camera) => (
                <article key={camera.id} className="queue-card queue-card--stale">
                  <div className="queue-card__head">
                    <div className="queue-card__title">
                      <span className="list-row__title">{camera.label}</span>
                      <span className="queue-card__sub">
                        {camera.provider} · {camera.city} · last successful check{" "}
                        {camera.lastSuccessAt ? formatStamp(camera.lastSuccessAt) : "never"}
                      </span>
                    </div>
                    <span className="badge badge-warn">Stale</span>
                  </div>
                  {camera.lastError ? <p className="field-error">{camera.lastError}</p> : null}
                  <p className="hint">Do not treat this feed as current evidence.</p>
                </article>
              ))}
            </div>
          )}
        </div>
      </section>

      <section className="panel" aria-labelledby="queue-audit-heading">
        <div className="panel__head">
          <h2 id="queue-audit-heading">Recent audit trail</h2>
          <span className="panel__count">{audit.length}</span>
        </div>
        <div className="panel__body">
          {audit.length === 0 ? (
            <p className="empty-state">
              <strong>No review actions recorded yet.</strong>
              <span>The trail starts with the first decision you make.</span>
            </p>
          ) : (
            <ul className="audit-list">
              {audit.map((event) => (
                <li key={event.id}>
                  <time className="audit-time" dateTime={event.createdAt}>
                    {formatStamp(event.createdAt)}
                  </time>
                  <span className="stack-sm">
                    <span className="audit-entity">
                      {event.entityType} · {event.action}
                    </span>
                    <span className="audit-flow">
                      <span className="from">{event.previousValue ?? "—"}</span>
                      <span className="arrow" aria-hidden="true">
                        →
                      </span>
                      <span className="to">{event.newValue ?? "—"}</span>
                      <span className="mono muted">{event.entityId.slice(0, 8)}</span>
                    </span>
                  </span>
                  {event.note ? <span className="audit-note">{event.note}</span> : null}
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>
    </>
  );
}
