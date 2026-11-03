import { ReviewerDashboard } from "@/components/reviewer/ReviewerDashboard";

export default function ReviewerPage() {
  return (
    <>
      <div className="page-header">
        <div className="page-header__eyebrow">Human in the loop</div>
        <h1>Reviewer dashboard</h1>
        <p>
          Human review turns a claim into something others can rely on. Every decision records who decided,
          what changed, and why — the previous value is never erased.
        </p>
      </div>
      <ReviewerDashboard />
    </>
  );
}
