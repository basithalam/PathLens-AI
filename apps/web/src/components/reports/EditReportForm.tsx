"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { updateReport, type AccessibilityReport } from "@/lib/api-client";

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

export function EditReportForm({ report }: { report: AccessibilityReport }) {
  const router = useRouter();
  const [title, setTitle] = useState(report.title);
  const [description, setDescription] = useState(report.description ?? "");
  const [obstacleType, setObstacleType] = useState(report.obstacleType);
  const [severity, setSeverity] = useState(report.severity);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [message, setMessage] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setSaving(true);
    setErrors({});
    setMessage(null);

    const result = await updateReport(report.id, {
      title,
      description: description.trim() ? description : null,
      obstacleType,
      severity,
    });

    setSaving(false);
    if (result.ok) {
      router.push(`/reports/${report.id}`);
      router.refresh();
    } else {
      setErrors(result.errors);
      setMessage(result.message);
    }
  }

  function fieldError(field: string) {
    const messages = errors[field];
    return messages?.length ? (
      <p role="alert" className="field-error">
        {messages.join(" ")}
      </p>
    ) : null;
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
        <input value={title} onChange={(event) => setTitle(event.target.value)} maxLength={200} required />
      </label>
      {fieldError("title")}

      <label>
        Description (optional)
        <textarea
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          maxLength={2000}
          rows={4}
        />
      </label>
      {fieldError("description")}

      <div className="form-row">
        <label>
          Obstacle type
          <select value={obstacleType} onChange={(event) => setObstacleType(event.target.value)}>
            {OBSTACLE_TYPES.map((type) => (
              <option key={type} value={type}>
                {type}
              </option>
            ))}
          </select>
        </label>
        {fieldError("obstacleType")}

        <label>
          Severity
          <select value={severity} onChange={(event) => setSeverity(event.target.value)}>
            {SEVERITIES.map((level) => (
              <option key={level} value={level}>
                {level}
              </option>
            ))}
          </select>
        </label>
        {fieldError("severity")}
      </div>

      <p className="status-source">
        Location cannot be edited here; coordinates are part of the original claim.
      </p>

      <button type="submit" disabled={saving} className="primary-button">
        {saving ? "Saving…" : "Save changes"}
      </button>
    </form>
  );
}
