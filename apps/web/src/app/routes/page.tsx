import { RoutePlanner } from "@/components/routes/RoutePlanner";

export default function RoutesPage() {
  return (
    <>
      <h1>Plan a route</h1>
      <p>
        Compare walking routes by what is actually known about them: reported
        obstacles, how fresh the evidence is, and how much of the route nobody
        has reported on yet. No route is labelled safe just because it has no
        reports.
      </p>
      <RoutePlanner />
    </>
  );
}
