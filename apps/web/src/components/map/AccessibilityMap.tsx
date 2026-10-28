"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import "leaflet/dist/leaflet.css";
import {
  getCameras,
  getReportsInViewport,
  type AccessibilityReport,
  type CameraSource,
  type MapViewport,
} from "@/lib/api-client";
import { StatusBadge } from "@/components/StatusBadge";

const OBSTACLE_TYPES = [
  "BlockedSidewalk",
  "BrokenSurface",
  "MissingOrBlockedRamp",
  "StairsOrStep",
  "ConstructionBarrier",
  "Other",
  "Unknown",
];

const VERIFICATION_STATUSES = ["Unverified", "HumanVerified", "Disputed", "Rejected"];

// Default view: Dhaka. The map asks for reports wherever the user pans.
const DEFAULT_CENTER: [number, number] = [23.8103, 90.4125];

function escapeHtml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function markerLetter(report: AccessibilityReport): string {
  switch (report.verificationStatus) {
    case "HumanVerified":
      return "V";
    case "Disputed":
      return "?";
    case "Rejected":
      return "X";
    default:
      return "!";
  }
}

export function AccessibilityMap() {
  const containerRef = useRef<HTMLDivElement>(null);
  // Leaflet objects are kept in refs so React state never holds them.
  const mapRef = useRef<unknown>(null);
  const layerRef = useRef<unknown>(null);
  const cameraLayerRef = useRef<unknown>(null);
  const leafletRef = useRef<typeof import("leaflet") | null>(null);

  const [reports, setReports] = useState<AccessibilityReport[]>([]);
  const [cameras, setCameras] = useState<CameraSource[]>([]);
  const [camerasInView, setCamerasInView] = useState<CameraSource[]>([]);
  const [showCameras, setShowCameras] = useState(true);
  const showCamerasRef = useRef(true);
  showCamerasRef.current = showCameras;
  const camerasRef = useRef<CameraSource[]>([]);
  camerasRef.current = cameras;
  const viewportRef = useRef<MapViewport | null>(null);
  const [selected, setSelected] = useState<AccessibilityReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [obstacleType, setObstacleType] = useState("");
  const [verificationStatus, setVerificationStatus] = useState("");
  const [locationNote, setLocationNote] = useState<string | null>(null);

  const filtersRef = useRef({ obstacleType: "", verificationStatus: "" });
  filtersRef.current = { obstacleType, verificationStatus };

  function renderCameras() {
    const L = leafletRef.current;
    const layer = cameraLayerRef.current as import("leaflet").LayerGroup | null;
    const viewport = viewportRef.current;
    if (!L || !layer || !viewport) {
      return;
    }

    layer.clearLayers();
    const inView = camerasRef.current.filter(
      (camera) =>
        camera.latitude >= viewport.minLatitude &&
        camera.latitude <= viewport.maxLatitude &&
        camera.longitude >= viewport.minLongitude &&
        camera.longitude <= viewport.maxLongitude,
    );
    setCamerasInView(inView);

    if (!showCamerasRef.current) {
      return;
    }

    for (const camera of inView) {
      const state = !camera.enabled ? "disabled" : camera.isStale ? "stale" : "ok";
      const icon = L.divIcon({
        className: "map-marker",
        html: `<span class="map-camera-pin map-camera-${state}" title="${escapeHtml(camera.label)} camera"><span class="map-camera-letter">C</span></span>`,
        iconSize: [26, 26],
        iconAnchor: [13, 13],
      });

      const marker = L.marker([camera.latitude, camera.longitude], { icon, title: `${camera.label} camera` });
      marker.bindPopup(
        `<strong>Camera: ${escapeHtml(camera.label)}</strong><br/>${escapeHtml(camera.provider)} · ${escapeHtml(camera.city)}<br/>` +
          (camera.enabled
            ? camera.isStale
              ? `Stale: no successful check recently${camera.lastSuccessAt ? ` (last success ${escapeHtml(new Date(camera.lastSuccessAt).toLocaleString())})` : " (never succeeded)"}. Treat its data as old.`
              : `Last checked ${camera.lastSuccessAt ? escapeHtml(new Date(camera.lastSuccessAt).toLocaleString()) : "never"}${camera.lastChangeAt ? `; last change ${escapeHtml(new Date(camera.lastChangeAt).toLocaleString())}` : ""}.`
            : "This camera source is disabled.") +
          `<br/>A camera snapshot is evidence, not proof that a path is accessible.`,
      );
      marker.addTo(layer);
    }
  }

  async function loadReports(viewport: MapViewport) {
    viewportRef.current = viewport;
    renderCameras();
    setLoading(true);
    const result = await getReportsInViewport(viewport, {
      obstacleType: filtersRef.current.obstacleType || undefined,
      verificationStatus: filtersRef.current.verificationStatus || undefined,
    });
    setLoading(false);

    if (result === null) {
      setError("Reports could not be loaded. Check that the backend is running, then move the map to retry.");
      return;
    }

    setError(null);
    setReports(result);
    renderMarkers(result);
  }

  function renderMarkers(items: AccessibilityReport[]) {
    const L = leafletRef.current;
    const layer = layerRef.current as import("leaflet").LayerGroup | null;
    if (!L || !layer) {
      return;
    }

    layer.clearLayers();
    for (const report of items) {
      const icon = L.divIcon({
        className: "map-marker",
        html: `<span class="map-marker-pin map-marker-${report.verificationStatus.toLowerCase()}" title="${escapeHtml(report.title)}"><span class="map-marker-letter">${markerLetter(report)}</span></span>`,
        iconSize: [30, 30],
        iconAnchor: [15, 15],
      });

      const marker = L.marker([report.latitude, report.longitude], { icon, title: report.title });
      marker.bindPopup(
        `<strong>${escapeHtml(report.title)}</strong><br/>${escapeHtml(report.obstacleType)} · ${escapeHtml(report.verificationStatus)}<br/><a href="/reports/${report.id}">Open report</a>`,
      );
      marker.on("click", () => setSelected(report));
      marker.addTo(layer);
    }
  }

  useEffect(() => {
    let cancelled = false;

    async function init() {
      const L = await import("leaflet");
      if (cancelled || !containerRef.current || mapRef.current) {
        return;
      }

      leafletRef.current = L;
      const map = L.map(containerRef.current).setView(DEFAULT_CENTER, 13);
      mapRef.current = map;

      L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution:
          '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      }).addTo(map);

      layerRef.current = L.layerGroup().addTo(map);
      cameraLayerRef.current = L.layerGroup().addTo(map);

      void getCameras().then((result) => {
        if (!cancelled && result !== null) {
          camerasRef.current = result;
          setCameras(result);
          renderCameras();
        }
      });

      const loadFromBounds = () => {
        const bounds = map.getBounds();
        void loadReports({
          minLatitude: bounds.getSouth(),
          maxLatitude: bounds.getNorth(),
          minLongitude: bounds.getWest(),
          maxLongitude: bounds.getEast(),
        });
      };

      map.on("moveend", loadFromBounds);
      loadFromBounds();
    }

    void init();
    return () => {
      cancelled = true;
      const map = mapRef.current as import("leaflet").Map | null;
      map?.remove();
      mapRef.current = null;
      layerRef.current = null;
      cameraLayerRef.current = null;
    };
    // Initialised once; filter changes reload through the effect below.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Re-render only the camera layer when its toggle changes.
  // renderCameras reads the latest toggle through showCamerasRef.
  useEffect(() => {
    renderCameras();
  }, [showCameras]);

  // Reload the current viewport when filters change.
  useEffect(() => {
    const map = mapRef.current as import("leaflet").Map | null;
    if (!map) {
      return;
    }

    const bounds = map.getBounds();
    void loadReports({
      minLatitude: bounds.getSouth(),
      maxLatitude: bounds.getNorth(),
      minLongitude: bounds.getWest(),
      maxLongitude: bounds.getEast(),
    });
    // loadReports reads the latest filters through filtersRef.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [obstacleType, verificationStatus]);

  function useMyLocation() {
    const map = mapRef.current as import("leaflet").Map | null;
    if (!map) {
      return;
    }

    if (!navigator.geolocation) {
      setLocationNote("This browser does not provide geolocation. Pan the map manually instead.");
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        map.setView([position.coords.latitude, position.coords.longitude], 15);
        setLocationNote("Map moved to your browser location. Reports will reload for this area.");
      },
      () =>
        setLocationNote(
          "Location permission was denied. That is fine — pan and zoom the map to choose an area manually.",
        ),
    );
  }

  return (
    <>
      <div className="filter-bar" aria-label="Map filters">
        <label>
          Obstacle type
          <select value={obstacleType} onChange={(event) => setObstacleType(event.target.value)}>
            <option value="">All types</option>
            {OBSTACLE_TYPES.map((type) => (
              <option key={type} value={type}>
                {type}
              </option>
            ))}
          </select>
        </label>
        <label>
          Verification
          <select
            value={verificationStatus}
            onChange={(event) => setVerificationStatus(event.target.value)}
          >
            <option value="">Any status</option>
            {VERIFICATION_STATUSES.map((status) => (
              <option key={status} value={status}>
                {status}
              </option>
            ))}
          </select>
        </label>
        <button type="button" onClick={useMyLocation}>
          Use my location
        </button>
        <label>
          <input
            type="checkbox"
            checked={showCameras}
            onChange={(event) => setShowCameras(event.target.checked)}
          />{" "}
          Show cameras (square C markers)
        </label>
      </div>

      {locationNote && <p>{locationNote}</p>}
      {loading && <p aria-live="polite">Loading reports for this area…</p>}
      {error && (
        <p role="alert" className="status-error">
          {error}
        </p>
      )}

      <div className="map-layout">
        <div
          ref={containerRef}
          className="map-container"
          role="application"
          aria-label="Accessibility reports map. Use the list beside the map if you cannot use a map."
        />

        <section aria-labelledby="map-list-heading" className="map-list">
          <h2 id="map-list-heading">Reports in this area ({reports.length})</h2>
          <p>
            The list and the map always show the same reports. Marker letters mean:
            ! unverified, V human-verified, ? disputed, X rejected. Square C
            markers are cameras, kept separate from obstacle reports.
          </p>

          {selected && (
            <article className="selected-report" aria-live="polite">
              <h3>{selected.title}</h3>
              <p>
                <StatusBadge status={selected.verificationStatus} /> {selected.obstacleType} ·
                Severity {selected.severity}
              </p>
              <p>
                Reported {new Date(selected.createdAt).toLocaleString()}. This is a
                user claim until a reviewer verifies it.
              </p>
              <Link href={`/reports/${selected.id}`}>Open full report</Link>
            </article>
          )}

          <h3>Cameras in this area ({camerasInView.length})</h3>
          {camerasInView.length === 0 ? (
            <p>
              No automatic camera source here. That area only updates when a
              person reports or uploads evidence — no camera coverage is not
              proof that paths are clear.
            </p>
          ) : (
            <ul className="report-list">
              {camerasInView.map((camera) => (
                <li key={camera.id}>
                  <strong>{camera.label}</strong> ({camera.provider}, {camera.city})
                  <br />
                  <span>
                    {!camera.enabled
                      ? "Disabled."
                      : camera.isStale
                        ? `Stale — last successful check ${camera.lastSuccessAt ? new Date(camera.lastSuccessAt).toLocaleString() : "never"}. Its data may be old.`
                        : `Last checked ${camera.lastSuccessAt ? new Date(camera.lastSuccessAt).toLocaleString() : "never"}${camera.lastChangeAt ? `, last change ${new Date(camera.lastChangeAt).toLocaleString()}` : ""}.`}
                  </span>
                </li>
              ))}
            </ul>
          )}

          {reports.length === 0 && !loading ? (
            <p>
              No reports in this area yet. No reports does not mean the paths here
              are clear — it means nobody has reported yet.
            </p>
          ) : (
            <ul className="report-list">
              {reports.map((report) => (
                <li key={report.id}>
                  <button type="button" onClick={() => setSelected(report)} className="list-select">
                    {report.title}
                  </button>{" "}
                  <StatusBadge status={report.verificationStatus} />
                  <br />
                  <span>
                    {report.obstacleType} · {new Date(report.createdAt).toLocaleString()}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </>
  );
}
