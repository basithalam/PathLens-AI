import { AccessibilityMap } from "@/components/map/AccessibilityMap";

export default function MapPage() {
  return (
    <>
      <h1>Accessibility map</h1>
      <p>
        Obstacle reports on OpenStreetMap. Pan or zoom and the reports reload for
        the area you are looking at. A place with no marker is unknown, not
        proven accessible.
      </p>
      <AccessibilityMap />
    </>
  );
}
