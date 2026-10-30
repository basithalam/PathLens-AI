import type * as Leaflet from "leaflet";

/**
 * Fix Leaflet's default marker URLs for Next.js.
 * Next.js serves /public at site root, but leaflet.css expects relative images.
 * Without this, browsers request /marker-icon.png and get 404.
 * Call right after `await import("leaflet")` in every map component.
 */
export function fixLeafletIcons(L: typeof Leaflet): void {
  // In Leaflet 1.9 the prototype holds _getIconUrl that returns relative paths.
  // Deleting it forces Leaflet to use mergeOptions URLs below.
  const proto = L.Icon.Default.prototype as unknown as Record<string, unknown>;
  if (proto._getIconUrl) {
    delete proto._getIconUrl;
  }
  L.Icon.Default.mergeOptions({
    iconRetinaUrl: "/marker-icon-2x.png",
    iconUrl: "/marker-icon.png",
    shadowUrl: "/marker-shadow.png",
  });
}
