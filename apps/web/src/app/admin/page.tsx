import { AdminCamerasPanel } from "@/components/reviewer/AdminCamerasPanel";

export default function AdminPage() {
  return (
    <>
      <h1>Admin — Live Cameras</h1>
      <p>
        Only <strong>Admin</strong> can register San Francisco / Datumfeed cameras here.
        Sign in with <code>admin@pathlens.local / Admin@123</code> — a camera cannot be enabled without a permission note.
      </p>
      <AdminCamerasPanel />
      <p style={{ marginTop: "1.5rem", fontSize: "0.9rem", opacity: 0.8 }}>
        Tip: Enable the camera after creation, then use <em>Poll now</em> to fetch an image immediately. It appears on the map as a square <code>C</code>.
      </p>
    </>
  );
}
