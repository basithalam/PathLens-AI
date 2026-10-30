"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { createReport } from "@/lib/api-client";
import { MiniMapPicker } from "@/components/map/MiniMapPicker";
import { enqueueReport, getQueue } from "@/lib/offline-queue";

const OBSTACLE_TYPES = [
  "BlockedSidewalk",
  "BrokenSurface",
  "MissingOrBlockedRamp",
  "StairsOrStep",
  "ConstructionBarrier",
  "Other",
  "Unknown",
];

const SEVERITIES = ["Unknown", "Low", "Medium", "High"];

function FieldError({ errors, field }: { errors: Record<string, string[]>; field: string }) {
  const messages = errors[field] ?? errors[field[0].toUpperCase() + field.slice(1)];
  if (!messages?.length) {
    return null;
  }
  return (
    <p role="alert" className="field-error">
      {messages.join(" ")}
    </p>
  );
}

export function ReportForm() {
  const router = useRouter();
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [obstacleType, setObstacleType] = useState("Unknown");
  const [severity, setSeverity] = useState("Unknown");
  const [latitude, setLatitude] = useState("");
  const [longitude, setLongitude] = useState("");
  const [hasStairs, setHasStairs] = useState("");
  const [hasRamp, setHasRamp] = useState("");
  const [surface, setSurface] = useState("");
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [message, setMessage] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [locationNote, setLocationNote] = useState<string | null>(null);
  const [showMiniMap, setShowMiniMap] = useState(false);

  function useMyLocation() {
    if (!navigator.geolocation) {
      setLocationNote("This browser does not provide geolocation. Enter coordinates manually.");
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        setLatitude(String(position.coords.latitude));
        setLongitude(String(position.coords.longitude));
        const acc = Math.round(position.coords.accuracy);
        let note = `Location filled from your browser (accuracy about ${acc} m). Check it before submitting.`;
        if (acc > 50) note += " ⚠️ Accuracy is low (>50m) — drag the map pin to fine-tune.";
        if (acc > 100) note += " Very low — please verify on map.";
        setLocationNote(note);
      },
      () => setLocationNote("Location permission was denied. Enter coordinates manually."),
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 },
    );
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setMessage(null);
    setErrors({});

    const payload = {
      title,
      description: description.trim() ? description : null,
      obstacleType,
      severity,
      latitude: Number(latitude),
      longitude: Number(longitude),
      hasStairs: hasStairs === "" ? null : hasStairs === "yes",
      hasRamp: hasRamp === "" ? null : hasRamp === "yes",
      surface: surface === "" ? null : surface,
    };

    // H4.3 offline queue — if navigator is offline, queue locally
    if (typeof navigator !== "undefined" && !navigator.onLine) {
      const queued = enqueueReport(payload);
      setSubmitting(false);
      setMessage(`You are offline — report queued locally (${queued.id.slice(0, 8)}). It will be sent when you are back online. Queue: ${getQueue().length} pending.`);
      return;
    }

    const result = await createReport(payload);

    setSubmitting(false);

    if (result.ok) {
      router.push(`/reports/${result.report.id}`);
      return;
    }

    // Network error → offer to queue
    if (!navigator.onLine || result.message.includes("not reachable")) {
      const queued = enqueueReport(payload);
      setMessage(`Could not reach the API — report queued locally (${queued.id.slice(0, 8)}). Retry when online. Queue: ${getQueue().length}`);
      return;
    }

    setErrors(result.errors);
    setMessage(result.message);
  }

  return (
    <form onSubmit={onSubmit} className="report-form" noValidate>
      {message && (
        <p role="alert" className="status-error">
          {message}
        </p>
      )}

      <label>
        Title
        <input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} required />
      </label>
      <FieldError errors={errors} field="title" />

      <label>
        Description (optional)
        <textarea value={description} onChange={(e) => setDescription(e.target.value)} maxLength={2000} rows={4} />
      </label>
      <FieldError errors={errors} field="description" />

      <div className="form-row">
        <label>
          Obstacle type
          <select value={obstacleType} onChange={(e) => setObstacleType(e.target.value)}>
            {OBSTACLE_TYPES.map((type) => (
              <option key={type} value={type}>
                {type}
              </option>
            ))}
          </select>
        </label>
        <FieldError errors={errors} field="obstacleType" />

        <label>
          Severity
          <select value={severity} onChange={(e) => setSeverity(e.target.value)}>
            {SEVERITIES.map((level) => (
              <option key={level} value={level}>
                {level}
              </option>
            ))}
          </select>
        </label>
        <FieldError errors={errors} field="severity" />
      </div>

      <div className="form-row">
        <label>
          Latitude
          <input value={latitude} onChange={(e) => setLatitude(e.target.value)} inputMode="decimal" placeholder="23.8103" required />
        </label>
        <FieldError errors={errors} field="latitude" />

        <label>
          Longitude
          <input value={longitude} onChange={(e) => setLongitude(e.target.value)} inputMode="decimal" placeholder="90.4125" required />
        </label>
        <FieldError errors={errors} field="longitude" />
      </div>

      <fieldset>
        <legend>What did you actually see? (optional — Unknown is honest)</legend>
        <p>
          Only set these when you observed them yourself. Leave Unknown when
          you did not check; the system never estimates them for you.
        </p>
        <div className="form-row">
          <label>
            Stairs or a step here?
            <select value={hasStairs} onChange={(e) => setHasStairs(e.target.value)}>
              <option value="">Unknown</option>
              <option value="yes">Yes</option>
              <option value="no">No</option>
            </select>
          </label>
          <label>
            Ramp here?
            <select value={hasRamp} onChange={(e) => setHasRamp(e.target.value)}>
              <option value="">Unknown</option>
              <option value="yes">Yes</option>
              <option value="no">No</option>
            </select>
          </label>
          <label>
            Surface
            <select value={surface} onChange={(e) => setSurface(e.target.value)}>
              <option value="">Unknown</option>
              <option value="Smooth">Smooth</option>
              <option value="Uneven">Uneven</option>
              <option value="Broken">Broken</option>
            </select>
          </label>
        </div>
        <FieldError errors={errors} field="surface" />
      </fieldset>

      <p>
        <button type="button" onClick={useMyLocation}>
          Use my location
        </button>{" "}
        <button type="button" onClick={() => setShowMiniMap(v => !v)} style={{ marginLeft: 8 }}>
          {showMiniMap ? "Hide map picker" : "Pick on map"}
        </button>{" "}
        {locationNote && <span>{locationNote}</span>}
      </p>
      {showMiniMap && (
        <div style={{ border: "1px solid var(--line)", borderRadius: 8, overflow: "hidden", height: 260, position: "relative" }}>
          <MiniMapPicker
            latitude={latitude ? Number(latitude) : null}
            longitude={longitude ? Number(longitude) : null}
            onPick={(lat, lon) => {
              setLatitude(String(lat.toFixed(6)));
              setLongitude(String(lon.toFixed(6)));
              setLocationNote(`Picked on map — ${lat.toFixed(5)}, ${lon.toFixed(5)}. Drag marker or click again to adjust.`);
            }}
          />
        </div>
      )}
      <p className="status-source">
        Coordinates you enter are a claim about where the obstacle is, not a verified fact.
        Photo upload and AI analysis arrive in Phase 2.
      </p>

      <label style={{ display: "flex", gap: 8, alignItems: "flex-start", marginTop: 12 }}>
        <input type="checkbox" required aria-label="Consent to store report and photo per retention policy" />
        <span style={{ fontSize: "0.88rem" }}>
          I confirm this photo is my own, contains no identifiable faces/plates, and I consent to storage under the{" "}
          <a href="/privacy" target="_blank">retention policy</a> (camera 30d, evidence 1y, report until deleted). I can request deletion via report delete.
        </span>
      </label>

      <button type="submit" disabled={submitting} className="primary-button">
        {submitting ? "Saving…" : "Create report"}
      </button>
    </form>
  );
}
