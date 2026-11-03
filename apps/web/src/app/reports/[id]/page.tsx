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
import { QuickFeedback } from "@/components/feedback/QuickFeedback";

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

  const attributesUnknown =
    report.hasStairs === null && report.hasRamp === null && report.surface === null;

  return (
    <>
      <div className="breadcrumb">
        <Link href="/reports" className="backlink">
          ← All reports
        </Link>
        <span className="mono">{report.id.slice(0, 8)}</span>
        <span className="spacer" />
        <Link href={`/reports/${report.id}/edit`} className="btn btn--ghost btn--sm">
          Edit report
        </Link>
      </div>

      <div className="page-header">
        <div className="row">
          <span className="page-header__eyebrow">{report.obstacleType}</span>
          <StatusBadge status={report.verificationStatus} />
          <span className="badge badge-neutral">Status: {report.status}</span>
        </div>
        <h1>{report.title}</h1>
        <p>
          {report.description ??
            "No description was provided. The claim, its evidence and its review history are all that exist for this place."}
        </p>
      </div>

      {report.duplicateOfReportId && (
        <p className="callout callout--info">
          <strong>Merged as a duplicate.</strong> This report points to{" "}
          <Link href={`/reports/${report.duplicateOfReportId}`}>the main report for this obstacle</Link>.
          Its evidence and history are kept here; counts and routes use the main report.
        </p>
      )}
      {!report.duplicateOfReportId && report.possibleDuplicateOfReportId && (
        <p className="callout callout--warn">
          <strong>Possible duplicate</strong> of{" "}
          <Link href={`/reports/${report.possibleDuplicateOfReportId}`}>another nearby report</Link> — the
          suggestion is automatic and waits for a reviewer to confirm or reject. Both reports stay visible
          until then.
        </p>
      )}

      <dl className="meta-grid">
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
          <dd className="mono">
            {report.latitude}, {report.longitude}
            <small>A claim from the reporter, not a verified position.</small>
          </dd>
        </div>
        <div>
          <dt>Created</dt>
          <dd>
            {new Date(report.createdAt).toLocaleString()}
            <small>{new Date(report.createdAt).toLocaleDateString()}</small>
          </dd>
        </div>
        <div>
          <dt>Observed attributes</dt>
          <dd>
            {attributesUnknown ? (
              <span className="badge badge-unknown">Unknown — not estimated</span>
            ) : (
              [
                report.hasStairs !== null ? `Stairs: ${report.hasStairs ? "yes" : "no"}` : "Stairs: unknown",
                report.hasRamp !== null ? `Ramp: ${report.hasRamp ? "yes" : "no"}` : "Ramp: unknown",
                report.surface !== null ? `Surface: ${report.surface}` : "Surface: unknown",
                report.attributeSource ? `source: ${report.attributeSource}` : "",
              ].join(" · ")
            )}
          </dd>
        </div>
        <div>
          <dt>Last evidence</dt>
          <dd>
            {report.lastEvidenceAt ? (
              new Date(report.lastEvidenceAt).toLocaleString()
            ) : (
              <span className="badge badge-unknown">No analysed photo yet</span>
            )}
          </dd>
        </div>
      </dl>

      <nav className="row" aria-label="Sections of this report">
        <a href="#camera-freshness-heading" className="tag">
          Camera updates
        </a>
        <a href="#verification-history-heading" className="tag">
          Review history
        </a>
        <a href="#quick-feedback-heading" className="tag">
          Quick feedback
        </a>
        <a href="#evidence-heading" className="tag">
          Photo evidence
        </a>
        <a href="#comparison-heading" className="tag">
          Compare over time
        </a>
      </nav>

      <section className="panel" id="camera-freshness-heading" aria-labelledby="camera-freshness-title">
        <div className="panel__head">
          <h2 id="camera-freshness-title">Automatic camera updates</h2>
          {report.cameraSourceId ? <span className="badge badge-info">CameraDetected candidate</span> : null}
        </div>
        <div className="panel__body">
          {report.cameraSourceId && (
            <p className="callout callout--info">
              A camera snapshot suggested something here, but no person has verified it. It is an
              observation awaiting review, not a human-verified report.
            </p>
          )}
          {latestCameraEvidence ? (
            <p>
              Last checked by camera:{" "}
              <time className="mono">{new Date(latestCameraEvidence.uploadedAt).toLocaleString()}</time> —
              analysis <StatusBadge status={latestCameraEvidence.analysisStatus} />
              {latestCameraEvidence.analyzedAt ? (
                <>
                  {" "}
                  at <time className="mono">{new Date(latestCameraEvidence.analyzedAt).toLocaleString()}</time>
                </>
              ) : null}
              . A camera check is fresh evidence, not a guarantee that the path is now clear or blocked.
            </p>
          ) : nearbyCameras.length > 0 ? (
            <p className="callout callout--unknown">
              A camera source is about {Math.round(nearbyCameras[0].distance)} m away (
              {nearbyCameras[0].camera.label}) but has not provided a snapshot for this report yet
              {nearbyCameras[0].camera.isStale
                ? " — and that feed is currently stale, so do not rely on it."
                : "."}
            </p>
          ) : (
            <p className="callout callout--unknown">
              <strong>No automatic camera source here.</strong> This place only updates when a person reports
              or uploads evidence — absence of a feed is not absence of obstacles.
            </p>
          )}
        </div>
      </section>

      <section
        className="panel"
        id="verification-history-heading"
        aria-labelledby="verification-history-title"
      >
        <div className="panel__head">
          <h2 id="verification-history-title">Verification history</h2>
          <span className="panel__count">{reviews.length}</span>
        </div>
        <div className="panel__body">
          {reviews.length === 0 ? (
            <p className="empty-state">
              <strong>No human review recorded yet.</strong>
              <span>
                This report is a community claim until a reviewer verifies, disputes or rejects it in the{" "}
                <Link href="/reviewer">review console</Link>.
              </span>
            </p>
          ) : (
            <ul className="audit-list">
              {reviews.map((review) => (
                <li key={review.id}>
                  <time className="audit-time" dateTime={review.createdAt}>
                    {new Date(review.createdAt).toLocaleString()}
                  </time>
                  <span className="audit-flow">
                    <span className="from">{review.previousValue ?? "—"}</span>
                    <span className="arrow" aria-hidden="true">
                      →
                    </span>
                    <span className="to">{review.newValue ?? "—"}</span>
                  </span>
                  {review.note ? <span className="audit-note">{review.note}</span> : null}
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>

      <QuickFeedback reportId={report.id} />

      <EvidenceSection reportId={report.id} initialEvidence={evidence} />

      <ComparisonSection evidence={evidence} initialComparisons={comparisons} />
    </>
  );
}
