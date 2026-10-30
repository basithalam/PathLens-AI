import { ReviewerDashboard } from "@/components/reviewer/ReviewerDashboard";
import { AdminCamerasPanel } from "@/components/reviewer/AdminCamerasPanel";

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
      <hr style={{ margin: "2rem 0" }} />
      <h1>Admin — ক্যামেরা</h1>
      <p>
        লাইভ ক্যামেরার still ছবি এখান থেকে রেজিস্টার করুন। Admin লগইন প্রয়োজন —
        permissionNote ছাড়া ক্যামেরা সক্রিয় হয় না।
      </p>
      <AdminCamerasPanel />
    </>
  );
}
