"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { createReport } from "@/lib/api-client";
import { MiniMapPicker } from "@/components/map/MiniMapPicker";

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
        setLocationNote(
          `Location filled from your browser (accuracy about ${Math.round(position.coords.accuracy)} m). Check it before submitting.`,
        );
      },
      () => setLocationNote("Location permission was denied. Enter coordinates manually."),
    );
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setMessage(null);
    setErrors({});

    const result = await createReport({
      title,
      description: description.trim() ? description : null,
      obstacleType,
      severity,
      latitude: Number(latitude),
      longitude: Number(longitude),
      hasStairs: hasStairs === "" ? null : hasStairs === "yes",
      hasRamp: hasRamp === "" ? null : hasRamp === "yes",
      surface: surface === "" ? null : surface,
    });

    setSubmitting(false);

    if (result.ok) {
      router.push(`/reports/${result.report.id}`);
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

      <button type="submit" disabled={submitting} className="primary-button">
        {submitting ? "Saving…" : "Create report"}
      </button>
    </form>
  );
}
