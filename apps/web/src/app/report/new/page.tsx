import { ReportForm } from "@/components/reports/ReportForm";

export default function NewReportPage() {
  return (
    <>
      <h1>Report an obstacle</h1>
      <p>
        Describe a possible pedestrian obstacle and where it is. Your report starts
        as unverified — it becomes trustworthy only after evidence and review.
      </p>
      <ReportForm />
    </>
  );
}
