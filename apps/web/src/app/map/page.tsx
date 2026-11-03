import Link from "next/link";
import { AccessibilityMap } from "@/components/map/AccessibilityMap";

export default function MapPage() {
  return (
    <>
      <div className="page-header">
        <div className="page-header__eyebrow">OpenStreetMap · live viewport</div>
        <h1>Accessibility map</h1>
        <p>
          Obstacle reports on OpenStreetMap. Pan or zoom and the reports reload for the area you are looking
          at. A place with no marker is unknown, not proven accessible.
        </p>
        <div className="page-header__actions">
          <Link href="/report/new" className="btn btn--ghost btn--sm">
            Report what you see
          </Link>
          <Link href="/routes" className="btn btn--quiet btn--sm">
            Plan a route instead
          </Link>
        </div>
      </div>
      <AccessibilityMap />
    </>
  );
}
