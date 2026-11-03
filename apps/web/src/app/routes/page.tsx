import { RoutePlanner } from "@/components/routes/RoutePlanner";

export default function RoutesPage() {
  return (
    <>
      <div className="page-header">
        <div className="page-header__eyebrow">Evidence-aware routing</div>
        <h1>Plan a route</h1>
        <p>
          Compare walking routes by what is actually known about them: reported obstacles, freshness of
          evidence, and how much of the route is unreported. No route is labelled safe just because it has no
          reports.
        </p>
      </div>
      <RoutePlanner />
    </>
  );
}
