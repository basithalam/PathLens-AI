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

const ACCEPTED = "image/jpeg,image/png,image/webp";
const MAX_BYTES = 10 * 1024 * 1024;

function AnalysisFindings({ item }: { item: EvidenceItem }) {
  const result = parseAnalysisResult(item);

  if (item.analysisStatus === "Failed") {
    return (
      <p role="alert" className="field-error">
        Analysis failed. Use “Retry analysis” below. If the AI provider is not
        configured, the job error on the server explains why.
      </p>
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
          the pipeline until a real Meta AI key is configured.
        </p>
      )}
      <p>
        <strong>Possible obstacle:</strong> {result.obstacleType} ·{" "}
        <strong>Confidence:</strong> {Math.round(result.confidence * 100)}% ·{" "}
        <strong>Provider:</strong> {result.provider}
        {result.model ? ` (${result.model})` : ""}
      </p>
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
        <p>A human reviewer still needs to verify this before it is treated as fact.</p>
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
    const result = await uploadEvidence(
      reportId,
      selected,
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
        <div className="evidence-list">
          {items.map((item) => (
            <EvidenceCard
              key={item.id}
              item={item}
              onUpdated={(updated) =>
                setItems((current) => current.map((c) => (c.id === updated.id ? updated : c)))
              }
            />
          ))}
        </div>
      )}
    </section>
  );
}
