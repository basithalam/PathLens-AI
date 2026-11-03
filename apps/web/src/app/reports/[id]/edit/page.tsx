import Link from "next/link";
import { notFound } from "next/navigation";
import { getReport } from "@/lib/api-client";
import { EditReportForm } from "@/components/reports/EditReportForm";

export const dynamic = "force-dynamic";

interface EditReportProps {
  params: Promise<{ id: string }>;
}

export default async function EditReportPage({ params }: EditReportProps) {
  const { id } = await params;
  const report = await getReport(id);

  if (report === null) {
    notFound();
  }

  return (
    <>
      <div className="breadcrumb">
        <Link href={`/reports/${report.id}`} className="backlink">
          ← Back to report
        </Link>
        <span className="mono">{report.id.slice(0, 8)}</span>
      </div>

      <div className="page-header">
        <div className="page-header__eyebrow">Edit claim</div>
        <h1>Edit “{report.title}”</h1>
        <p>
          You are editing the original claim — its text, obstacle type and severity. Evidence, comparisons and
          every reviewer decision stay attached and untouched.
        </p>
      </div>

      <EditReportForm report={report} />
    </>
  );
}
