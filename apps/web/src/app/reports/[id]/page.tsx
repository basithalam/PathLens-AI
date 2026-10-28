import Link from "next/link";
import { notFound } from "next/navigation";
import { getComparisons, getEvidenceForReport, getReport, getReportReviews } from "@/lib/api-client";
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
          <dt>Last evidence</dt>
          <dd>
            {report.lastEvidenceAt
              ? new Date(report.lastEvidenceAt).toLocaleString()
              : "No photo evidence analyzed yet."}
          </dd>
        </div>
      </dl>

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
