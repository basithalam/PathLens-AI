import { AdminConsole } from "./AdminConsole";

export default function AdminPage() {
  return (
    <>
      <div className="page-header">
        <div className="page-header__eyebrow">Admin only</div>
        <h1>City operations</h1>
        <p>
          Camera registry, pilot analytics and reporter reputation. Everything on this page reads the same
          store the public map and route planner use — an admin never edits a verdict from here.
        </p>
      </div>
      <AdminConsole />
    </>
  );
}
