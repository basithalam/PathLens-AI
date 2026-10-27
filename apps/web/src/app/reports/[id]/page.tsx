import Link from "next/link";
import { notFound } from "next/navigation";
import { getReport } from "@/lib/api-client";
import { StatusBadge } from "@/components/StatusBadge";

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

  return (
    <>
      <p>
        <Link href="/reports">← All reports</Link>
      </p>
      <h1>{report.title}</h1>
      <p>
        <StatusBadge status={report.verificationStatus} />{" "}
        <span>Report status: {report.status}</span>
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
              : "No photo evidence yet. Evidence upload arrives in Phase 2."}
          </dd>
        </div>
      </dl>
    </>
  );
}
