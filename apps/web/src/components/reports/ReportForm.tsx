"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { createReport } from "@/lib/api-client";
import { MiniMapPicker } from "@/components/map/MiniMapPicker";
import { enqueueReport, getQueue } from "@/lib/offline-queue";

const OBSTACLE_TYPES = [
  { id: "BlockedSidewalk", label: "Blocked Sidewalk", icon: "🚧", desc: "Scooter, trash, etc." },
  { id: "BrokenSurface", label: "Broken Surface", icon: "🕳️", desc: "Cracks, potholes" },
  { id: "MissingOrBlockedRamp", label: "Ramp Blocked", icon: "♿", desc: "No ramp or blocked" },
  { id: "StairsOrStep", label: "Stairs / Step", icon: "🪜", desc: "Unexpected stairs" },
  { id: "ConstructionBarrier", label: "Construction", icon: "🚧", desc: "Fence, scaffolding" },
  { id: "Other", label: "Other", icon: "❓", desc: "Something else" },
  { id: "Unknown", label: "Unknown", icon: "❔", desc: "Not sure yet" },
] as const;

const SEVERITIES = [
  { id: "Unknown", label: "Unknown", color: "#64748b", bg: "#f1f5f9" },
  { id: "Low", label: "Low", color: "#2563eb", bg: "#dbeafe" },
  { id: "Medium", label: "Medium", color: "#d97706", bg: "#fef3c7" },
  { id: "High", label: "High", color: "#dc2626", bg: "#fee2e2" },
] as const;

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
  const [step, setStep] = useState(1);
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
  const [showMiniMap, setShowMiniMap] = useState(true);

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

      <div style={{ display: "flex", gap: ".5rem", marginBottom: "1rem", padding: ".75rem", background: "var(--surface-2)", borderRadius: "var(--radius-sm)", border: "1px solid var(--line)" }}>
        <div style={{ flex: 1, display: "flex", alignItems: "center", gap: ".5rem", fontWeight: 700, fontSize: ".85rem", color: step >= 1 ? "var(--brand-dark)" : "var(--muted)" }}>
          <span style={{ width: "28px", height: "28px", borderRadius: "50%", background: step >= 1 ? "var(--brand)" : "var(--line)", color: "#fff", display: "grid", placeItems: "center", fontSize: ".8rem", fontWeight: 800 }}>1</span> What
        </div>
        <div style={{ flex: 1, display: "flex", alignItems: "center", gap: ".5rem", fontWeight: 700, fontSize: ".85rem", color: step >= 2 ? "var(--brand-dark)" : "var(--muted)" }}>
          <span style={{ width: "28px", height: "28px", borderRadius: "50%", background: step >= 2 ? "var(--brand)" : "var(--line)", color: "#fff", display: "grid", placeItems: "center", fontSize: ".8rem", fontWeight: 800 }}>2</span> Where
        </div>
        <div style={{ flex: 1, display: "flex", alignItems: "center", gap: ".5rem", fontWeight: 700, fontSize: ".85rem", color: step >= 3 ? "var(--brand-dark)" : "var(--muted)" }}>
          <span style={{ width: "28px", height: "28px", borderRadius: "50%", background: step >= 3 ? "var(--brand)" : "var(--line)", color: "#fff", display: "grid", placeItems: "center", fontSize: ".8rem", fontWeight: 800 }}>3</span> Confirm
        </div>
      </div>

      <div>
        <label style={{ fontWeight: 700, fontSize: ".95rem", display: "block", marginBottom: ".5rem" }}>What did you see? Tap one</label>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(160px, 1fr))", gap: ".6rem" }}>
          {OBSTACLE_TYPES.map((type) => (
            <button
              key={type.id}
              type="button"
              onClick={() => { setObstacleType(type.id); setStep(2); }}
              style={{
                display: "flex", flexDirection: "column", alignItems: "center", gap: ".35rem",
                padding: ".85rem .5rem", borderRadius: "var(--radius-sm)", border: `2px solid ${obstacleType === type.id ? "var(--brand)" : "var(--line)"}`,
                background: obstacleType === type.id ? "var(--brand-ghost)" : "var(--surface)", cursor: "pointer", transition: "all .15s",
                boxShadow: obstacleType === type.id ? "0 0 0 3px rgba(15,118,110,.12)" : "none"
              }}
            >
              <span style={{ fontSize: "1.5rem" }}>{type.icon}</span>
              <span style={{ fontWeight: 700, fontSize: ".85rem", color: obstacleType === type.id ? "var(--brand-dark)" : "var(--ink)", textAlign: "center" }}>{type.label}</span>
              <span style={{ fontSize: ".7rem", color: "var(--muted)", textAlign: "center" }}>{type.desc}</span>
            </button>
          ))}
        </div>
        <FieldError errors={errors} field="obstacleType" />
      </div>

      <div>
        <label style={{ fontWeight: 700, fontSize: ".95rem", display: "block", marginBottom: ".5rem" }}>How severe?</label>
        <div style={{ display: "flex", gap: ".5rem", flexWrap: "wrap" }}>
          {SEVERITIES.map((level) => (
            <button
              key={level.id}
              type="button"
              onClick={() => setSeverity(level.id)}
              style={{
                padding: ".55rem 1rem", borderRadius: "var(--radius-pill)", border: `2px solid ${severity === level.id ? level.color : "var(--line)"}`,
                background: severity === level.id ? level.bg : "var(--surface)", color: severity === level.id ? level.color : "var(--muted)",
                fontWeight: 700, fontSize: ".85rem", cursor: "pointer", transition: "all .15s"
              }}
            >
              {level.label}
            </button>
          ))}
        </div>
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
        <p>Only set when you observed yourself. Leave Unknown — system never estimates.</p>
        <div className="form-row">
          <label>Stairs? <select value={hasStairs} onChange={(e) => setHasStairs(e.target.value)}><option value="">Unknown</option><option value="yes">Yes</option><option value="no">No</option></select></label>
          <label>Ramp? <select value={hasRamp} onChange={(e) => setHasRamp(e.target.value)}><option value="">Unknown</option><option value="yes">Yes</option><option value="no">No</option></select></label>
          <label>Surface <select value={surface} onChange={(e) => setSurface(e.target.value)}><option value="">Unknown</option><option value="Smooth">Smooth</option><option value="Uneven">Uneven</option><option value="Broken">Broken</option></select></label>
        </div>
        <FieldError errors={errors} field="surface" />
      </fieldset>

      <div style={{ background: "var(--surface-2)", border: "1px solid var(--line)", borderRadius: "var(--radius)", padding: "1.25rem", display: "grid", gap: "1rem" }}>
        <h3 style={{ margin: 0, fontSize: "1rem", fontWeight: 800, display: "flex", alignItems: "center", gap: ".5rem" }}>📍 Where is it?</h3>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
          <label style={{ display: "grid", gap: ".4rem", fontWeight: 600, fontSize: ".85rem" }}>Latitude <input value={latitude} onChange={(e) => { setLatitude(e.target.value); setStep(3); }} inputMode="decimal" placeholder="37.7749" required style={{ border: "1px solid var(--line)", borderRadius: "var(--radius-sm)", padding: ".65rem .8rem" }} /></label>
          <label style={{ display: "grid", gap: ".4rem", fontWeight: 600, fontSize: ".85rem" }}>Longitude <input value={longitude} onChange={(e) => setLongitude(e.target.value)} inputMode="decimal" placeholder="-122.4194" required style={{ border: "1px solid var(--line)", borderRadius: "var(--radius-sm)", padding: ".65rem .8rem" }} /></label>
        </div>
        <div style={{ display: "flex", gap: ".5rem", flexWrap: "wrap" }}>
          <button type="button" onClick={useMyLocation} style={{ flex: 1, minWidth: "160px", padding: ".7rem 1rem", borderRadius: "var(--radius-pill)", border: "1px solid var(--brand)", background: "var(--brand-ghost)", color: "var(--brand-dark)", fontWeight: 700, cursor: "pointer" }}>📍 Use my location</button>
          <button type="button" onClick={() => setShowMiniMap(v => !v)} style={{ flex: 1, minWidth: "140px", padding: ".7rem 1rem", borderRadius: "var(--radius-pill)", border: "1px solid var(--line)", background: "var(--surface)", fontWeight: 600, cursor: "pointer" }}>{showMiniMap ? "Hide map" : "Show map 🗺️"}</button>
        </div>
        {locationNote && <div style={{ padding: ".6rem .8rem", background: "var(--accent-light)", border: "1px solid #bfdbfe", borderRadius: "var(--radius-sm)", fontSize: ".85rem", color: "var(--ink-light)" }}>{locationNote}</div>}
        {showMiniMap && (
          <div style={{ border: "2px solid var(--brand)", borderRadius: "var(--radius-sm)", overflow: "hidden", height: 340, position: "relative", boxShadow: "var(--shadow)" }}>
            <MiniMapPicker
              latitude={latitude ? Number(latitude) : null}
              longitude={longitude ? Number(longitude) : null}
              onPick={(lat, lon) => {
                setLatitude(String(lat.toFixed(6)));
                setLongitude(String(lon.toFixed(6)));
                setLocationNote(`📍 Picked on map — ${lat.toFixed(5)}, ${lon.toFixed(5)}. Drag marker or click again to adjust.`);
                setStep(3);
              }}
            />
            <div style={{ position: "absolute", top: "8px", left: "50%", transform: "translateX(-50%)", background: "var(--ink)", color: "#fff", padding: ".4rem .7rem", borderRadius: "var(--radius-pill)", fontSize: ".75rem", fontWeight: 700, pointerEvents: "none", boxShadow: "var(--shadow-md)" }}>Tap map to set pin • Drag to adjust</div>
          </div>
        )}
        <p style={{ margin: 0, fontSize: ".82rem", color: "var(--muted)", display: "flex", gap: ".4rem", alignItems: "center" }}>ℹ️ Coordinates are a claim, not verified fact. Photo + AI in next step.</p>
      </div>

      <div style={{ background: "var(--surface)", border: "1px solid var(--line)", borderRadius: "var(--radius-sm)", padding: "1rem", display: "flex", gap: ".75rem", alignItems: "flex-start" }}>
        <input type="checkbox" required aria-label="Consent to store report and photo per retention policy" style={{ marginTop: "3px", accentColor: "var(--brand)", width: "18px", height: "18px" }} />
        <span style={{ fontSize: ".85rem", lineHeight: 1.5, color: "var(--muted)" }}>
          I confirm this is my own photo with no faces/plates, and I consent to storage per <a href="/privacy" target="_blank" style={{ color: "var(--brand-dark)", fontWeight: 600 }}>retention policy</a> (camera 30d, evidence 1y). I can delete anytime.
        </span>
      </div>

      <button type="submit" disabled={submitting} className="primary-button" style={{ width: "100%", justifyContent: "center", fontSize: "1rem", padding: "1rem", display: "flex", alignItems: "center", gap: ".5rem" }}>
        {submitting ? "⏳ Creating report…" : "✅ Create report →"}
      </button>
      <p style={{ textAlign: "center", fontSize: ".82rem", color: "var(--muted)", margin: ".5rem 0 0" }}>Takes 2 seconds • You can add photos next</p>
    </form>
  );
}
