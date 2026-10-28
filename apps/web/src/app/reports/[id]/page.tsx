import Link from "next/link";
import { notFound } from "next/navigation";
import {
  distanceMeters,
  getCameras,
  getComparisons,
  getEvidenceForReport,
  getReport,
  getReportReviews,
} from "@/lib/api-client";
import { StatusBadge } from "@/components/StatusBadge";
import { EvidenceSection } from "@/components/evidence/EvidenceSection";
import { ComparisonSection } from "@/components/comparisons/ComparisonSection";

export const dynamic = "force-dynamic";

interface ReportDetailsProps {
  params: Promise<{ id: string }>;
}

export default async function ReportDetailsPage({ params }: ReportDetailsProps) {
  const { id } = await params;
  const report = await getReport(id);

  if (report === null) {
    notFound();
  }

  const evidence = (await getEvidenceForReport(id)) ?? [];
  const comparisons = (await getComparisons(id)) ?? [];
  const reviews = (await getReportReviews(id)) ?? [];
  const cameras = (await getCameras()) ?? [];

  const cameraEvidence = evidence
    .filter((item) => item.sourceType === "CameraFeed")
    .sort((a, b) => b.uploadedAt.localeCompare(a.uploadedAt));
  const latestCameraEvidence = cameraEvidence[0] ?? null;
  const nearbyCameras = cameras
    .map((camera) => ({ camera, distance: distanceMeters(report, camera) }))
    .filter((entry) => entry.distance <= 100)
    .sort((a, b) => a.distance - b.distance);

  return (
    <>
      <p>
        <Link href="/reports">← All reports</Link>
      </p>
      <h1>{report.title}</h1>
      <p>
        <StatusBadge status={report.verificationStatus} />{" "}
        <span>Report status: {report.status}</span>{" "}
        <Link href={`/reports/${report.id}/edit`}>Edit report</Link>
      </p>

      {report.description ? <p>{report.description}</p> : <p>No description was provided.</p>}

      <dl className="status-list">
        <div>
          <dt>Obstacle type</dt>
          <dd>{report.obstacleType}</dd>
        </div>
        <div>
          <dt>Severity</dt>
          <dd>{report.severity}</dd>
        </div>
        <div>
          <dt>Location (user-supplied)</dt>
          <dd>
            {report.latitude}, {report.longitude}
          </dd>
        </div>
        <div>
          <dt>Created</dt>
          <dd>{new Date(report.createdAt).toLocaleString()}</dd>
        </div>
        <div>
          <dt>Observed attributes</dt>
          <dd>
            {report.hasStairs === null && report.hasRamp === null && report.surface === null
              ? "Stairs, ramp and surface have not been measured here. Unknown — not estimated."
              : [
                  report.hasStairs !== null ? `Stairs: ${report.hasStairs ? "yes" : "no"}` : "Stairs: unknown",
                  report.hasRamp !== null ? `Ramp: ${report.hasRamp ? "yes" : "no"}` : "Ramp: unknown",
                  report.surface !== null ? `Surface: ${report.surface}` : "Surface: unknown",
                  report.attributeSource ? ` (source: ${report.attributeSource})` : "",
                ].join(" · ")}
          </dd>
        </div>
        <div>
          <dt>Last evidence</dt>
          <dd>
            {report.lastEvidenceAt
              ? new Date(report.lastEvidenceAt).toLocaleString()
              : "No photo evidence analyzed yet."}
          </dd>
        </div>
      </dl>

      <section aria-labelledby="camera-freshness-heading">
        <h2 id="camera-freshness-heading">Automatic camera updates</h2>
        {report.cameraSourceId ? (
          <p>
            This is a <strong>CameraDetected candidate</strong>: a camera
            snapshot suggested something here, but no person has verified it.
            It is not human-verified.
          </p>
        ) : null}
        {latestCameraEvidence ? (
          <p>
            Last checked by camera: {new Date(latestCameraEvidence.uploadedAt).toLocaleString()}{" "}
            — analysis {latestCameraEvidence.analysisStatus.toLowerCase()}
            {latestCameraEvidence.analyzedAt
              ? ` at ${new Date(latestCameraEvidence.analyzedAt).toLocaleString()}`
              : ""}
            . A camera check is fresh evidence, not a guarantee that the path
            is now clear or blocked.
          </p>
        ) : nearbyCameras.length > 0 ? (
          <p>
            A camera source is within about {Math.round(nearbyCameras[0].distance)} m (
            {nearbyCameras[0].camera.label}), but it has not provided a snapshot
            for this report yet
            {nearbyCameras[0].camera.isStale
              ? " — and that feed is currently stale, so do not rely on it."
              : "."}
          </p>
        ) : (
          <p>
            No automatic camera source here. This place only updates when a
            person reports or uploads evidence.
          </p>
        )}
      </section>

      <section aria-labelledby="verification-history-heading">
        <h2 id="verification-history-heading">Verification history</h2>
        {reviews.length === 0 ? (
          <p>No human review has been recorded for this report yet.</p>
        ) : (
          <ul className="report-list">
            {reviews.map((review) => (
              <li key={review.id}>
                {new Date(review.createdAt).toLocaleString()} — {review.previousValue ?? "—"}{" "}
                → {review.newValue ?? "—"}
                {review.note ? <> · {review.note}</> : null}
              </li>
            ))}
          </ul>
        )}
      </section>

      <EvidenceSection reportId={report.id} initialEvidence={evidence} />

      <ComparisonSection evidence={evidence} initialComparisons={comparisons} />
    </>
  );
}
