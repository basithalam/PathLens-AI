"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { authFetch, clearSession, getSessionUser, login, type SessionUser } from "@/lib/auth";

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

export function ReviewerDashboard() {
  const [user, setUser] = useState<SessionUser | null>(null);
  const [mounted, setMounted] = useState(false);
  const [email, setEmail] = useState("reviewer@pathlens.local");
  const [password, setPassword] = useState("");
  const [loginError, setLoginError] = useState<string | null>(null);
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
    setMounted(true);
    const existing = getSessionUser();
    if (existing) {
      setUser(existing);
      void loadQueue();
    }
  }, [loadQueue]);

  async function onLogin(event: React.FormEvent) {
    event.preventDefault();
    setLoginError(null);
    const result = await login(email, password);
    if (result.ok) {
      setUser(result.user);
      setPassword("");
      await loadQueue();
    } else {
      setLoginError(result.message);
    }
  }

  function onLogout() {
    clearSession();
    setUser(null);
    setQueue(null);
    setAudit([]);
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
      <form onSubmit={onLogin} className="report-form">
        <p>
          Reviewer and admin accounts are seeded for local development. Demo
          reviewer: <code>reviewer@pathlens.local</code> / <code>Reviewer@123</code>.
          Change these outside local development.
        </p>
        <label>
          Email
          <input value={email} onChange={(event) => setEmail(event.target.value)} type="email" required />
        </label>
        <label>
          Password
          <input
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            type="password"
            required
          />
        </label>
        {loginError && (
          <p role="alert" className="field-error">
            {loginError}
          </p>
        )}
        <button type="submit" className="primary-button">
          Log in
        </button>
      </form>
    );
  }

  return (
    <>
      <p>
        Signed in as {user.displayName} ({user.role}).{" "}
        <button type="button" onClick={onLogout}>
          Log out
        </button>
      </p>
      {message && (
        <p role="status" className="status-source">
          {message}
        </p>
      )}

      <h2>Reports waiting for review ({queue?.reports.length ?? 0})</h2>
      {queue === null ? (
        <p aria-live="polite">Loading the review queue…</p>
      ) : queue.reports.length === 0 ? (
        <p>No unverified reports right now.</p>
      ) : (
        <ul className="report-list">
          {queue.reports.map((report) => (
            <li key={report.id}>
              <Link href={`/reports/${report.id}`}>{report.title}</Link> — {report.obstacleType},{" "}
              reported {new Date(report.createdAt).toLocaleString()}
              {report.isCameraCandidate ? (
                <>
                  {" "}
                  <strong>CameraDetected candidate — unverified.</strong> A camera
                  suggested this; only your review can verify it.
                </>
              ) : null}
              {report.possibleDuplicateOfReportId ? (
                <>
                  <br />
                  <strong>Possible duplicate</strong> of{" "}
                  <Link href={`/reports/${report.possibleDuplicateOfReportId}`}>this nearby report</Link>.{" "}
                  <button type="button" onClick={() => decideDuplicate(report.id, "confirm")}>
                    Confirm merge
                  </button>{" "}
                  <button type="button" onClick={() => decideDuplicate(report.id, "reject")}>
                    Reject suggestion
                  </button>{" "}
                  Confirming deletes nothing: the report becomes a pointer and
                  keeps its evidence and audit history.
                </>
              ) : null}
              <br />
              <label>
                Reviewer note{" "}
                <input
                  value={notes[report.id] ?? ""}
                  onChange={(event) =>
                    setNotes((current) => ({ ...current, [report.id]: event.target.value }))
                  }
                  placeholder="What did you check?"
                />
              </label>{" "}
              {REVIEW_ACTIONS.map((action) => (
                <button key={action} type="button" onClick={() => reviewReport(report.id, action)}>
                  {action}
                </button>
              ))}
            </li>
          ))}
        </ul>
      )}

      <h2>Stale cameras ({queue?.staleCameras.length ?? 0})</h2>
      {queue === null ? (
        <p aria-live="polite">Loading camera health…</p>
      ) : queue.staleCameras.length === 0 ? (
        <p>No enabled camera is stale right now.</p>
      ) : (
        <ul className="report-list">
          {queue.staleCameras.map((camera) => (
            <li key={camera.id}>
              <strong>{camera.label}</strong> ({camera.provider}, {camera.city}) — stale.
              Last successful check:{" "}
              {camera.lastSuccessAt ? new Date(camera.lastSuccessAt).toLocaleString() : "never"}
              {camera.lastError ? <> · Last error: {camera.lastError}</> : null}. Do not
              treat this feed as current.
            </li>
          ))}
        </ul>
      )}

      <h2>Conflicting comparisons ({queue?.comparisons.length ?? 0})</h2>
      {queue && queue.comparisons.length === 0 ? (
        <p>No conflicting comparisons waiting.</p>
      ) : (
        <ul className="report-list">
          {queue?.comparisons.map((comparison) => (
            <li key={comparison.id}>
              Comparison from {new Date(comparison.createdAt).toLocaleString()} — open the{" "}
              <Link href={`/reports/${comparison.reportId}`}>report evidence explorer</Link> to
              review both photos and record a decision.
            </li>
          ))}
        </ul>
      )}

      <h2>Recent audit trail</h2>
      {audit.length === 0 ? (
        <p>No review actions recorded yet.</p>
      ) : (
        <ul className="report-list">
          {audit.map((event) => (
            <li key={event.id}>
              {new Date(event.createdAt).toLocaleString()} — {event.entityType}{" "}
              {event.action}: {event.previousValue ?? "—"} → {event.newValue ?? "—"}
              {event.note ? <> · {event.note}</> : null}
            </li>
          ))}
        </ul>
      )}
    </>
  );
}
