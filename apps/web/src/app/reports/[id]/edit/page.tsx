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
      <p>
        <Link href={`/reports/${report.id}`}>← Back to report</Link>
      </p>
      <h1>Edit report</h1>
      <EditReportForm report={report} />
    </>
  );
}
