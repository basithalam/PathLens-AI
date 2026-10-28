"use client";

import { useMemo, useState } from "react";
import {
  createComparison,
  evidenceFileUrl,
  parseAnalysisResult,
  reviewComparison,
  type EvidenceComparison,
  type EvidenceItem,
} from "@/lib/api-client";
import { StatusBadge } from "@/components/StatusBadge";

const OUTCOMES = [
  "LikelyStillPresent",
  "NotObservedInNewImage",
  "ConflictingEvidence",
  "Inconclusive",
];

function supportingLimitations(comparison: EvidenceComparison): string[] {
  try {
    const parsed = JSON.parse(comparison.supportingEvidenceJson) as { limitations?: string[] };
    return parsed.limitations ?? [];
  } catch {
    return [];
  }
}

function ComparisonCard({
  comparison,
  evidenceById,
  onReviewed,
}: {
  comparison: EvidenceComparison;
  evidenceById: Map<string, EvidenceItem>;
  onReviewed: (comparison: EvidenceComparison) => void;
}) {
  const earlier = evidenceById.get(comparison.earlierEvidenceId);
  const later = evidenceById.get(comparison.laterEvidenceId);
  const [reviewOutcome, setReviewOutcome] = useState("Inconclusive");
  const [reason, setReason] = useState("");
  const [reviewError, setReviewError] = useState<string | null>(null);
  const limitations = supportingLimitations(comparison);

  async function submitReview() {
    setReviewError(null);
    if (!reason.trim()) {
      setReviewError("Write a reason. An override without a reason is not allowed.");
      return;
    }

    const reviewed = await reviewComparison(comparison.id, reviewOutcome, reason);
    if (reviewed) {
      onReviewed(reviewed);
      setReason("");
    } else {
      setReviewError(
        "Review failed. You must be logged in as a reviewer (see the Reviewer page), and the backend must be running.",
      );
    }
  }

  return (
    <article className="comparison-card">
      <div className="comparison-images">
        <figure>
          {earlier && (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={evidenceFileUrl(earlier)} alt="Earlier evidence" className="evidence-photo" />
          )}
          <figcaption>
            Earlier
            {earlier && (
              <>
                {" "}· {new Date(earlier.captureTimeUtc ?? earlier.uploadedAt).toLocaleString()}
                <br />
                {parseAnalysisResult(earlier)?.obstacleType ?? earlier.analysisStatus}
              </>
            )}
          </figcaption>
        </figure>
        <figure>
          {later && (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={evidenceFileUrl(later)} alt="Later evidence" className="evidence-photo" />
          )}
          <figcaption>
            Later
            {later && (
              <>
                {" "}· {new Date(later.captureTimeUtc ?? later.uploadedAt).toLocaleString()}
                <br />
                {parseAnalysisResult(later)?.obstacleType ?? later.analysisStatus}
              </>
            )}
          </figcaption>
        </figure>
      </div>

      <p>
        Outcome: <StatusBadge status={comparison.effectiveOutcome} />
        {comparison.reviewedOutcome && (
          <>
            {" "}(human review changed this from the original {comparison.outcome})
          </>
        )}
      </p>
      <p>{comparison.explanation}</p>

      {limitations.length > 0 && (
        <>
          <p>
            <strong>Why this may be wrong</strong>
          </p>
          <ul>
            {limitations.map((text) => (
              <li key={text}>{text}</li>
            ))}
          </ul>
        </>
      )}

      {comparison.effectiveOutcome === "ConflictingEvidence" && (
        <p role="alert" className="status-error">
          Conflicting evidence: a human should look at both photos before anyone
          relies on this report.
        </p>
      )}

      <p className="status-source">
        Compared {new Date(comparison.createdAt).toLocaleString()}
        {comparison.modelVersion ? ` · Model: ${comparison.modelVersion}` : ""}
      </p>

      {comparison.reviewedOutcome ? (
        <p>
          <strong>Human review:</strong> {comparison.reviewedOutcome} — {comparison.reviewReason}
        </p>
      ) : (
        <div className="review-form">
          <p>
            <strong>Disagree? Record a human review.</strong> The original AI
            outcome is kept; your decision and reason are stored beside it.
          </p>
          <label>
            Reviewed outcome
            <select value={reviewOutcome} onChange={(event) => setReviewOutcome(event.target.value)}>
              {OUTCOMES.map((outcome) => (
                <option key={outcome} value={outcome}>
                  {outcome}
                </option>
              ))}
            </select>
          </label>
          <label>
            Reason
            <input
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Why is the AI outcome wrong or uncertain?"
            />
          </label>
          {reviewError && (
            <p role="alert" className="field-error">
              {reviewError}
            </p>
          )}
          <button type="button" onClick={submitReview} className="primary-button">
            Save human review
          </button>
        </div>
      )}
    </article>
  );
}

export function ComparisonSection({
  evidence,
  initialComparisons,
}: {
  evidence: EvidenceItem[];
  initialComparisons: EvidenceComparison[];
}) {
  const completed = useMemo(
    () => evidence.filter((item) => item.analysisStatus === "Completed"),
    [evidence],
  );
  const [comparisons, setComparisons] = useState(initialComparisons);
  const [earlierId, setEarlierId] = useState(completed[0]?.id ?? "");
  const [laterId, setLaterId] = useState(completed[1]?.id ?? "");
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const evidenceById = useMemo(
    () => new Map(evidence.map((item) => [item.id, item])),
    [evidence],
  );

  const timeline = useMemo(
    () =>
      [...evidence].sort((a, b) =>
        (a.captureTimeUtc ?? a.uploadedAt).localeCompare(b.captureTimeUtc ?? b.uploadedAt),
      ),
    [evidence],
  );

  async function compare() {
    setMessage(null);
    if (!earlierId || !laterId) {
      setMessage("Choose both an earlier and a later photo.");
      return;
    }

    setBusy(true);
    const result = await createComparison(earlierId, laterId);
    setBusy(false);

    if (result.ok) {
      setComparisons((current) => [result.comparison, ...current]);
    } else {
      setMessage(result.message);
    }
  }

  return (
    <section aria-labelledby="comparison-heading" className="comparison-section">
      <h2 id="comparison-heading">Evidence explorer — compare over time</h2>
      <p>
        Pick an earlier and a later photo of this place. The comparison states
        what changed, what did not, and what cannot be known. “Not observed” is
        never shown as “removed”.
      </p>

      {timeline.length > 0 && (
        <>
          <h3>Timeline</h3>
          <ol className="timeline">
            {timeline.map((item) => (
              <li key={item.id}>
                {new Date(item.captureTimeUtc ?? item.uploadedAt).toLocaleString()} —{" "}
                {item.fileName} · {item.analysisStatus}
                {item.captureTimeUtc ? "" : " (capture time not provided; upload time shown)"}
              </li>
            ))}
          </ol>
        </>
      )}

      {completed.length < 2 ? (
        <p>
          Two analysed photos are needed before a comparison can run. Upload a
          second photo above and run analysis on both. (Reload the page after
          new uploads so they appear here.)
        </p>
      ) : (
        <div className="compare-controls">
          <label>
            Earlier photo
            <select value={earlierId} onChange={(event) => setEarlierId(event.target.value)}>
              {completed.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.fileName} — {new Date(item.captureTimeUtc ?? item.uploadedAt).toLocaleString()}
                </option>
              ))}
            </select>
          </label>
          <label>
            Later photo
            <select value={laterId} onChange={(event) => setLaterId(event.target.value)}>
              {completed.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.fileName} — {new Date(item.captureTimeUtc ?? item.uploadedAt).toLocaleString()}
                </option>
              ))}
            </select>
          </label>
          <button type="button" onClick={compare} disabled={busy} className="primary-button">
            {busy ? "Comparing…" : "Compare photos"}
          </button>
          {message && (
            <p role="alert" className="field-error">
              {message}
            </p>
          )}
        </div>
      )}

      {comparisons.length > 0 && (
        <div className="comparison-list">
          {comparisons.map((comparison) => (
            <ComparisonCard
              key={comparison.id}
              comparison={comparison}
              evidenceById={evidenceById}
              onReviewed={(reviewed) =>
                setComparisons((current) =>
                  current.map((c) => (c.id === reviewed.id ? reviewed : c)),
                )
              }
            />
          ))}
        </div>
      )}
    </section>
  );
}
