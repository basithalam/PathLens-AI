import { AdminCamerasPanel } from "@/components/reviewer/AdminCamerasPanel";
import { AdminStatsPanel } from "@/components/admin/AdminStatsPanel";
import { ReputationPanel } from "@/components/admin/ReputationPanel";

export default function AdminPage() {
  return (
    <>
      <div className="page-header">
        <div className="page-header__eyebrow">Admin only</div>
        <h1>Live Cameras</h1>
        <p>
          Only <strong>Admin</strong> can register San Francisco / Datumfeed cameras here. Sign in with
          <code> admin@pathlens.local / Admin@123</code> — a camera cannot be enabled without a permission note.
        </p>
      </div>
      <div className="card">
        <AdminCamerasPanel />
      </div>
      <p className="status-source" style={{ marginTop: "1rem" }}>
        Tip: Enable the camera after creation, then use <em>Poll now</em> to fetch an image immediately. It appears on the
        map as a square <code>C</code>.
      </p>
      <AdminStatsPanel />
      <ReputationPanel />
    </>
  );
}
