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
            <strong>Earlier</strong>
            {earlier && (
              <>
                <br />
                {new Date(earlier.captureTimeUtc ?? earlier.uploadedAt).toLocaleString()}
                <br />
                {parseAnalysisResult(earlier)?.obstacleType ?? earlier.analysisStatus}
              </>
            )}
          </figcaption>
        </figure>
        <span className="comparison-arrow" aria-hidden="true">
          →
        </span>
        <figure>
          {later && (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={evidenceFileUrl(later)} alt="Later evidence" className="evidence-photo" />
          )}
          <figcaption>
            <strong>Later</strong>
            {later && (
              <>
                <br />
                {new Date(later.captureTimeUtc ?? later.uploadedAt).toLocaleString()}
                <br />
                {parseAnalysisResult(later)?.obstacleType ?? later.analysisStatus}
              </>
            )}
          </figcaption>
        </figure>
      </div>

      <div className="list-row__meta">
        <span className="queue-actions__label">Outcome</span>
        <StatusBadge status={comparison.effectiveOutcome} />
        {comparison.reviewedOutcome ? (
          <span className="hint">Human review changed this from the original {comparison.outcome}</span>
        ) : null}
      </div>
      <p>{comparison.explanation}</p>

      {limitations.length > 0 && (
        <>
          <p>
            <strong>Why this may be wrong</strong>
          </p>
          <ul className="list-bullets">
            {limitations.map((text) => (
              <li key={text}>{text}</li>
            ))}
          </ul>
        </>
      )}

      {comparison.effectiveOutcome === "ConflictingEvidence" && (
        <p role="alert" className="callout callout--bad">
          <strong>Conflicting evidence.</strong> A human should look at both photos before anyone relies on
          this report.
        </p>
      )}

      {comparison.effectiveOutcome === "NotObservedInNewImage" && (
        <p role="note" className="callout callout--unknown">
          <strong>Not observed in the newer photo.</strong> That is not proof the obstacle was removed — the
          camera or angle may simply not have covered it.
        </p>
      )}

      <p className="status-source">
        Compared {new Date(comparison.createdAt).toLocaleString()}
        {comparison.modelVersion ? ` · model ${comparison.modelVersion}` : ""}
      </p>

      {comparison.reviewedOutcome ? (
        <p className="notice">
          <strong>Human review:</strong> {comparison.reviewedOutcome} — {comparison.reviewReason}
        </p>
      ) : (
        <div className="review-form">
          <p>
            <strong>Disagree? Record a human review.</strong> The original AI outcome is kept; your decision
            and reason are stored beside it.
          </p>
          <div className="decision-grid">
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
          </div>
          {reviewError && (
            <p role="alert" className="field-error">
              {reviewError}
            </p>
          )}
          <button type="button" onClick={submitReview} className="btn btn--primary btn--sm">
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
          <h3>Timeline — {timeline.length} photo{timeline.length === 1 ? "" : "s"} at this place</h3>
          <ol className="timeline">
            {timeline.map((item) => (
              <li key={item.id}>
                <time className="mono" dateTime={item.captureTimeUtc ?? item.uploadedAt}>
                  {new Date(item.captureTimeUtc ?? item.uploadedAt).toLocaleString()}
                </time>{" "}
                — {item.fileName} · <StatusBadge status={item.analysisStatus} />
                {item.captureTimeUtc ? "" : " (capture time not provided; upload time shown)"}
              </li>
            ))}
          </ol>
        </>
      )}

      {completed.length < 2 ? (
        <p className="callout callout--unknown">
          <strong>Two analysed photos are needed before a comparison can run.</strong> Upload a second photo
          above and run analysis on both, then reload the page so they appear here.
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
          <button type="button" onClick={compare} disabled={busy} className="btn btn--primary">
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
