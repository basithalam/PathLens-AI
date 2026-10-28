import { ReviewerDashboard } from "@/components/reviewer/ReviewerDashboard";

export default function ReviewerPage() {
  return (
    <>
      <h1>Reviewer dashboard</h1>
      <p>
        Human review is what turns a claim into something others can rely on.
        Every decision records who decided, what changed, and why — the previous
        value is never erased.
      </p>
      <ReviewerDashboard />
    </>
  );
}
