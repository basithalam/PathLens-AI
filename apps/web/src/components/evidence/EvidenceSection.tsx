"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  evidenceFileUrl,
  getAnalysisJob,
  getEvidence,
  parseAnalysisResult,
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
      <div role="alert" className="field-error">
        <p>
          <strong>Analysis failed.</strong> Use “Retry analysis” below. The server stores the exact reason.
        </p>
        <p style={{ fontSize: "0.85rem", opacity: 0.9 }}>
          G1 hint: If <code>/health/ready</code> shows <code>ai-provider Degraded — missing MetaAi:Endpoint/Model/ApiKey</code>, set
          <code> .local/set-meta-env.ps1</code> (Groq <code>gsk_...</code> or Llama API <code>LLM_...</code>) then restart API. Check API logs for exact 401/404 body.
          See <code>docs/AI_GATE_A_REPORT.md</code> G1 for correct endpoint (old <code>api.meta.ai/compat/v1</code> was 404).
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
      {isMock && (
        <p className="mock-label" role="note">
          Demo/mock analysis — not real AI. This result exists only to exercise
          the pipeline until a real Meta AI key is configured (G1: set Groq gsk_... or Llama LLM_... in .local/set-meta-env.ps1).
        </p>
      )}
      {!isMock && (
        <p className="status-source" role="note" style={{ borderLeft: "3px solid #0b6b4f", paddingLeft: 8 }}>
          ✓ Live Meta AI — provider <strong>{result.provider}</strong> {result.model ? `(${result.model})` : ""} analyzed this photo. Mock label means pipeline test only.
        </p>
      )}
      <p>
        <strong>Possible obstacle:</strong> {result.obstacleType} ·{" "}
        <strong>Provider:</strong> {result.provider}
        {result.model ? ` (${result.model})` : ""}
      </p>
      {(() => {
        const pct = Math.round(result.confidence * 100);
        const level = pct < 40 ? "low" : pct < 70 ? "mid" : "high";
        const label = level === "low" ? "Low — treat as weak evidence" : level === "mid" ? "Medium — uncertain, verify" : "High — still needs human check";
        return (
          <div className="confidence-row" role="note" aria-label={`Confidence ${pct}% ${level}`}>
            <span className="confidence-label">Confidence: {pct}%</span>
            <div className="confidence-track" title={`${pct}% — ${label}`}>
              <div className={`confidence-fill confidence-fill--${level}`} style={{ width: `${Math.max(4, pct)}%` }} />
            </div>
            <span className={`confidence-pct confidence-pct--${level}`}>{pct}%</span>
            <span className="status-source" style={{ fontSize: "0.78rem", padding: ".2rem .45rem" }}>{label}</span>
          </div>
        );
      })()}
      {result.visibleEvidence.length > 0 && (
        <>
          <p>
            <strong>Visible evidence</strong>
          </p>
          <ul>
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
          <ul>
            {result.limitations.map((text) => (
              <li key={text}>{text}</li>
            ))}
          </ul>
        </>
      )}
      {result.humanReviewRequired && (
        <div className="callout callout--warn" role="note">
          <strong>Human review required</strong> — a reviewer still needs to verify this before it is treated as fact. AI can be wrong, especially on <span className="confidence-pct--low">low confidence</span>.
        </div>
      )}
      {!result.humanReviewRequired && (
        <div className="callout callout--info" role="note">
          Human review completed — but still treat as evidence, not absolute truth. Check original photo + comparison.
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
        <p>
          {item.fileName} · {(item.fileSize / 1024).toFixed(1)} KB · uploaded{" "}
          {new Date(item.uploadedAt).toLocaleString()}
        </p>
        <p>
          Analysis: <StatusBadge status={item.analysisStatus} />
          {item.analysisProvider && <> · Provider: {item.analysisProvider}</>}
        </p>
        {item.qualityFlag && item.qualityFlag !== "Suitable" && (
          <p role="note" className="mock-label">
            Camera frame check: {item.qualityFlag}.{" "}
            {item.qualityNote ??
              "This frame was flagged before any AI analysis; a flagged frame is not evidence that a path is clear or blocked."}
          </p>
        )}
        <AnalysisFindings item={item} />
        {(item.analysisStatus === "Pending" || item.analysisStatus === "Failed") && (
          <button type="button" onClick={runAnalysis} disabled={busy} className="primary-button">
            {busy ? "Analyzing…" : item.analysisStatus === "Failed" ? "Retry analysis" : "Run analysis"}
          </button>
        )}
        {item.analysisStatus === "Processing" && <p aria-live="polite">Analysis is processing…</p>}
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
        <p className="status-source">
          Capture time is your claim, not a verified fact. Without it, comparisons
          cannot make a time-based claim.
        </p>
        {selected && (
          <p>
            {selected.name} · {(selected.size / 1024).toFixed(1)} KB{" "}
            <button type="button" onClick={() => chooseFile(null)}>
              Remove
            </button>
          </p>
        )}
        {previewUrl && (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={previewUrl} alt="Selected evidence preview" className="evidence-photo" />
        )}
        {progress !== null && (
          <p aria-live="polite">
            Uploading… {progress}% <progress value={progress} max={100} />
          </p>
        )}
        {selected && progress === null && (
          <button type="button" onClick={upload} className="primary-button">
            Upload photo
          </button>
        )}
        {message && (
          <p role="alert" className="field-error">
            {message}
          </p>
        )}
      </div>

      {items.length === 0 ? (
        <p>No photo evidence yet for this report.</p>
      ) : (
        <>
          <p style={{ fontSize: "0.82rem", color: "var(--muted)" }}>
            {items.length} photo(s) — live camera 1/min adds fast, 5/page pagination keeps load fast. Page {page} of {totalPages} — {items.length} total
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
            <nav aria-label="Evidence pagination" style={{ display: "flex", gap: 8, flexWrap: "wrap", alignItems: "center", marginTop: 12 }}>
              <button type="button" disabled={page === 1} onClick={() => setPage((p) => Math.max(1, p - 1))} className="btn btn--ghost">← Previous</button>
              {Array.from({ length: totalPages }, (_, i) => i + 1)
                .slice(Math.max(0, page - 3), Math.min(totalPages, page + 2))
                .map((p) => (
                  <button key={p} type="button" onClick={() => setPage(p)} className={p === page ? "btn btn--primary" : "btn btn--ghost"} style={{ minWidth: 36 }}>
                    {p}
                  </button>
                ))}
              <button type="button" disabled={page === totalPages} onClick={() => setPage((p) => Math.min(totalPages, p + 1))} className="btn btn--ghost">Next →</button>
              <span style={{ fontSize: "0.82rem", color: "var(--muted)" }}>{items.length} total · 5/page</span>
            </nav>
          )}
        </>
      )}
    </section>
  );
}
