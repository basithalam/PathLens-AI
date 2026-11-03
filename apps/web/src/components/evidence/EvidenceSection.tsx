"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  evidenceFileUrl,
  getAnalysisJob,
  getEvidence,
  parseAnalysisResult,
  parseObstruction,
  startAnalysis,
  uploadEvidence,
  type EvidenceItem,
} from "@/lib/api-client";
import { StatusBadge } from "@/components/StatusBadge";
import { compressImageFile } from "@/lib/offline-queue";

const ACCEPTED = "image/jpeg,image/png,image/webp";
const MAX_BYTES = 10 * 1024 * 1024;

function AnalysisFindings({ item }: { item: EvidenceItem }) {
  const result = parseAnalysisResult(item);

  if (item.analysisStatus === "Failed") {
    return (
      <div role="alert" className="callout callout--bad">
        <p>
          <strong>Analysis failed.</strong> Use “Retry analysis” below — the server stores the exact reason
          and never invents a result.
        </p>
        <p className="hint">
          G1 hint: if <code>/health/ready</code> shows <code>ai-provider Degraded — missing
          MetaAi:Endpoint/Model/ApiKey</code>, set <code>.local/set-meta-env.ps1</code> (Groq{" "}
          <code>gsk_...</code> or Llama API <code>LLM_...</code>) then restart the API. Check API logs for the
          exact 401/404 body. See <code>docs/AI_GATE_A_REPORT.md</code> G1 for the correct endpoint (the old{" "}
          <code>api.meta.ai/compat/v1</code> was 404).
        </p>
      </div>
    );
  }

  if (!result) {
    return null;
  }

  const isMock = result.provider.toLowerCase() === "mock" || item.analysisProvider?.toLowerCase() === "mock";

  return (
    <div className="findings">
      {isMock ? (
        <p className="mock-label" role="note">
          <strong>Demo / mock analysis — not real AI.</strong> This result exists only to exercise the
          pipeline until a real Meta AI key is configured (G1: set Groq gsk_... or Llama LLM_... in
          .local/set-meta-env.ps1).
        </p>
      ) : (
        <p className="notice" role="note">
          <strong>✓ Live Meta AI</strong> — provider{" "}
          <span className="provider-tag">
            {result.provider}
            {result.model ? ` · ${result.model}` : ""}
          </span>{" "}
          analysed this photo. A mock label means a pipeline test only.
        </p>
      )}
      <p>
        <strong>Possible obstacle:</strong> <span className="tag">{result.obstacleType}</span>
      </p>
      {(() => {
        const pct = Math.round(result.confidence * 100);
        const level = pct < 40 ? "low" : pct < 70 ? "mid" : "high";
        const label = level === "low" ? "Low — treat as weak evidence" : level === "mid" ? "Medium — uncertain, verify" : "High — still needs human check";
        return (
          <div className="confidence-row" role="note" aria-label={`Confidence ${pct}% ${level}`}>
            <span className="confidence-label">
              AI confidence
              <span className={`confidence-pct confidence-pct--${level}`}>{pct}%</span>
            </span>
            <div className="confidence-track" title={`${pct}% — ${label}`}>
              <div className={`confidence-fill confidence-fill--${level}`} style={{ width: `${Math.max(4, pct)}%` }} />
            </div>
            <span className="confidence-verdict">{label}</span>
          </div>
        );
      })()}
      {(() => {
        const measurement = parseObstruction(item);
        if (!measurement) {
          return (
            <p className="hint">
              <strong>Obstruction share:</strong> not measured for this photo. Absence of a measurement is
              unknown, not a clear path.
            </p>
          );
        }
        return (
          <div className="callout callout--info" role="note">
            <strong>
              {measurement.walkwayOccupancyPercent}% of the visible walkway in this frame is occupied
            </strong>{" "}
            — measured from {measurement.obstacleBoxCount} box(es) that {measurement.provider} (
            {measurement.model}) returned for the concept “{measurement.obstacleConcept}”, over the region it
            found for “{measurement.walkwayConcept}”.
            <span className="hint">
              {" "}
              Share of the walkway <em>in the photo</em>, not metres — nothing here gives a scale reference —
              and {measurement.provider} returns no confidence score, so this is a measurement, not a
              guarantee.
            </span>
          </div>
        );
      })()}
      {result.visibleEvidence.length > 0 && (
        <>
          <p>
            <strong>Visible evidence</strong>
          </p>
          <ul className="list-bullets">
            {result.visibleEvidence.map((text) => (
              <li key={text}>{text}</li>
            ))}
          </ul>
        </>
      )}
      {result.limitations.length > 0 && (
        <>
          <p>
            <strong>Limitations</strong>
          </p>
          <ul className="list-bullets">
            {result.limitations.map((text) => (
              <li key={text}>{text}</li>
            ))}
          </ul>
        </>
      )}
      {result.humanReviewRequired ? (
        <div className="callout callout--warn" role="note">
          <strong>Human review required</strong> — a reviewer still needs to verify this before it is treated
          as fact. AI can be wrong, especially at low confidence.
        </div>
      ) : (
        <div className="callout callout--info" role="note">
          <strong>Human review completed</strong> — still treat this as evidence, not absolute truth. Check the
          original photo and the comparison.
        </div>
      )}
    </div>
  );
}

function EvidenceCard({
  item,
  onUpdated,
}: {
  item: EvidenceItem;
  onUpdated: (item: EvidenceItem) => void;
}) {
  const [busy, setBusy] = useState(false);
  const pollRef = useRef<number | null>(null);

  const stopPolling = useCallback(() => {
    if (pollRef.current !== null) {
      window.clearInterval(pollRef.current);
      pollRef.current = null;
    }
  }, []);

  useEffect(() => stopPolling, [stopPolling]);

  async function runAnalysis() {
    setBusy(true);
    const job = await startAnalysis(item.id);
    if (!job) {
      setBusy(false);
      return;
    }

    onUpdated({ ...item, analysisStatus: job.status });
    stopPolling();

    // Bounded polling: every 2.5s for at most ~50s, never forever.
    let elapsed = 0;
    pollRef.current = window.setInterval(async () => {
      elapsed += 2.5;
      const status = await getAnalysisJob(job.id);
      if (status?.status === "Completed" || status?.status === "Failed" || elapsed > 50) {
        stopPolling();
        const fresh = await getEvidence(item.id);
        if (fresh) {
          onUpdated(fresh);
        }
        setBusy(false);
      }
    }, 2500);
  }

  return (
    <article className="evidence-card">
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img src={evidenceFileUrl(item)} alt={`Evidence photo ${item.fileName}`} className="evidence-photo" />
      <div>
        <p className="evidence-file">
          {item.fileName} · {(item.fileSize / 1024).toFixed(1)} KB · uploaded{" "}
          {new Date(item.uploadedAt).toLocaleString()}
        </p>
        <div className="list-row__meta">
          <StatusBadge status={item.analysisStatus} />
          {item.analysisProvider ? <span className="provider-tag">{item.analysisProvider}</span> : null}
          <span className="meta-chip">
            evidence id <strong className="mono">{item.id.slice(0, 8)}</strong>
          </span>
        </div>
        {item.qualityFlag && item.qualityFlag !== "Suitable" && (
          <p role="note" className="mock-label">
            <strong>Camera frame check: {item.qualityFlag}.</strong>{" "}
            {item.qualityNote ??
              "This frame was flagged before any AI analysis; a flagged frame is not evidence that a path is clear or blocked."}
          </p>
        )}
        <AnalysisFindings item={item} />
        {(item.analysisStatus === "Pending" || item.analysisStatus === "Failed") && (
          <button type="button" onClick={runAnalysis} disabled={busy} className="btn btn--primary btn--sm">
            {busy ? "Analysing…" : item.analysisStatus === "Failed" ? "Retry analysis" : "Run analysis"}
          </button>
        )}
        {item.analysisStatus === "Processing" && (
          <p aria-live="polite" className="hint">
            Analysis is processing…
          </p>
        )}
      </div>
    </article>
  );
}

export function EvidenceSection({
  reportId,
  initialEvidence,
}: {
  reportId: string;
  initialEvidence: EvidenceItem[];
}) {
  const [items, setItems] = useState(initialEvidence);
  const [selected, setSelected] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [progress, setProgress] = useState<number | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [dragging, setDragging] = useState(false);
  const [captureTime, setCaptureTime] = useState("");
  const [page, setPage] = useState(1);
  const pageSize = 5;
  const totalPages = Math.ceil(items.length / pageSize);
  const paged = items.slice((page - 1) * pageSize, page * pageSize);

  useEffect(() => {
    if (page > totalPages && totalPages > 0) setPage(totalPages);
    if (page === 0 && totalPages > 0) setPage(1);
  }, [items.length, totalPages, page]);

  function chooseFile(file: File | null) {
    setMessage(null);
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
      setPreviewUrl(null);
    }

    if (!file) {
      setSelected(null);
      return;
    }

    if (!ACCEPTED.split(",").includes(file.type)) {
      setMessage("Only JPEG, PNG, or WebP images are allowed.");
      setSelected(null);
      return;
    }

    if (file.size > MAX_BYTES) {
      setMessage("File is too large. The maximum size is 10 MB.");
      setSelected(null);
      return;
    }

    setSelected(file);
    setPreviewUrl(URL.createObjectURL(file));
  }

  async function upload() {
    if (!selected) {
      return;
    }

    setProgress(0);
    // H4.3 client compress to <1MB before upload (saves bandwidth on wheelchair tray mobile)
    const toUpload = await compressImageFile(selected);
    if (toUpload.size !== selected.size) {
      setMessage(`Compressed from ${(selected.size / 1024).toFixed(0)} KB to ${(toUpload.size / 1024).toFixed(0)} KB before upload.`);
    }
    const result = await uploadEvidence(
      reportId,
      toUpload,
      setProgress,
      captureTime ? new Date(captureTime).toISOString() : undefined,
    );
    setProgress(null);

    if (result.ok) {
      setItems((current) => [result.item, ...current]);
      chooseFile(null);
      setMessage("Photo uploaded. Run analysis to check it.");
    } else {
      setMessage(result.message);
    }
  }

  return (
    <section aria-labelledby="evidence-heading" className="evidence-section">
      <h2 id="evidence-heading">Photo evidence</h2>
      <p>
        Upload a photo of the obstacle. AI analysis describes only what is visible
        and always states its limitations; it never proves a location or a time.
      </p>

      <div
        className={dragging ? "drop-zone dragging" : "drop-zone"}
        onDragOver={(event) => {
          event.preventDefault();
          setDragging(true);
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={(event) => {
          event.preventDefault();
          setDragging(false);
          chooseFile(event.dataTransfer.files?.[0] ?? null);
        }}
      >
        <div className="drop-zone__grid">
          <label>
            Choose a photo or drop it here
            <input
              type="file"
              accept={ACCEPTED}
              onChange={(event) => chooseFile(event.target.files?.[0] ?? null)}
            />
          </label>
          <label>
            When was this photo taken? (optional)
            <input
              type="datetime-local"
              value={captureTime}
              onChange={(event) => setCaptureTime(event.target.value)}
            />
          </label>
        </div>
        <p className="status-source">
          Capture time is your claim, not a verified fact. Without it, comparisons cannot make a time-based
          claim.
        </p>
        {selected && (
          <p className="evidence-file">
            {selected.name} · {(selected.size / 1024).toFixed(1)} KB{" "}
            <button type="button" className="btn btn--ghost btn--sm" onClick={() => chooseFile(null)}>
              Remove
            </button>
          </p>
        )}
        {previewUrl && (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={previewUrl} alt="Selected evidence preview" className="drop-zone__preview" />
        )}
        {progress !== null && (
          <p aria-live="polite" className="hint">
            Uploading… {progress}% <progress value={progress} max={100} />
          </p>
        )}
        <div className="drop-zone__actions">
          {selected && progress === null && (
            <button type="button" onClick={upload} className="btn btn--primary">
              Upload photo
            </button>
          )}
          <span className="hint">JPEG, PNG or WebP · up to 10 MB · compressed on this device before upload</span>
        </div>
        {message && (
          <p role="alert" className="field-error">
            {message}
          </p>
        )}
      </div>

      {items.length === 0 ? (
        <p className="empty-state">
          <strong>No photo evidence yet for this report.</strong>
          <span>Without a photo there is nothing to compare, so this place stays unknown.</span>
        </p>
      ) : (
        <>
          <p className="hint">
            {items.length} photo{items.length === 1 ? "" : "s"} · page {page} of {totalPages} · 5 per page — a
            live camera adds about one frame a minute, so the list stays paginated instead of loading all of
            them.
          </p>
          <div className="evidence-list">
            {paged.map((item) => (
              <EvidenceCard
                key={item.id}
                item={item}
                onUpdated={(updated) => setItems((current) => current.map((c) => (c.id === updated.id ? updated : c)))}
              />
            ))}
          </div>
          {totalPages > 1 && (
            <nav aria-label="Evidence pagination" className="pager">
              <button
                type="button"
                disabled={page === 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                className="btn btn--ghost btn--sm"
              >
                ← Previous
              </button>
              {Array.from({ length: totalPages }, (_, i) => i + 1)
                .slice(Math.max(0, page - 3), Math.min(totalPages, page + 2))
                .map((p) => (
                  <button
                    key={p}
                    type="button"
                    onClick={() => setPage(p)}
                    className={p === page ? "btn btn--primary btn--sm" : "btn btn--ghost btn--sm"}
                    aria-current={p === page ? "page" : undefined}
                  >
                    {p}
                  </button>
                ))}
              <button
                type="button"
                disabled={page === totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                className="btn btn--ghost btn--sm"
              >
                Next →
              </button>
              <span className="pager__total">{items.length} total</span>
            </nav>
          )}
        </>
      )}
    </section>
  );
}
