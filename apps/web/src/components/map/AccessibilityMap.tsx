"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import "leaflet/dist/leaflet.css";
import {
  getCameras,
  getCoverage,
  getReportsInViewport,
  type AccessibilityReport,
  type CameraSource,
  type CoverageCell,
  type CoverageResponse,
  type MapViewport,
} from "@/lib/api-client";
import { StatusBadge } from "@/components/StatusBadge";
import { fixLeafletIcons } from "@/lib/leaflet-icons";

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

// Default view: Sylhet, Bangladesh (pilot city — user requested, SF deferred). The map asks for reports wherever the user pans.
const DEFAULT_CENTER: [number, number] = [24.9116, 91.8498];

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
  const coverageLayerRef = useRef<unknown>(null);
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
  const [coverage, setCoverage] = useState<CoverageResponse | null>(null);
  const [showCoverage, setShowCoverage] = useState(false);
  const [coverageLoading, setCoverageLoading] = useState(false);
  const showCoverageRef = useRef(false);
  showCoverageRef.current = showCoverage;
  const [selectedCell, setSelectedCell] = useState<CoverageCell | null>(null);

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

  function renderCoverage(cells: CoverageCell[]) {
    const L = leafletRef.current;
    const layer = coverageLayerRef.current as import("leaflet").LayerGroup | null;
    if (!L || !layer) return;
    layer.clearLayers();
    if (!showCoverageRef.current) return;
    for (const cell of cells) {
      const bounds: [[number, number], [number, number]] = [
        [cell.minLatitude, cell.minLongitude],
        [cell.maxLatitude, cell.maxLongitude],
      ];
      const isObstacle = cell.status === "ObstacleEvidence";
      const isInsufficient = cell.status === "InsufficientInfo";
      const letter = isInsufficient ? "C" : isObstacle ? "B" : "A";
      const rectangle = L.rectangle(bounds, {
        color: isInsufficient ? "#8a8a8a" : isObstacle ? "#8a5a00" : "#0b6b4f",
        weight: 1,
        opacity: 0.6,
        fillColor: isInsufficient ? "#c9d2cd" : isObstacle ? "#ffcc80" : "#a8e6cf",
        fillOpacity: isInsufficient ? 0.35 : 0.38,
      });
      const label = isInsufficient ? "C — Insufficient" : isObstacle ? `B — Obstacle (${cell.reportCount})` : `A — Evidence (${cell.reportCount})`;
      const newest = cell.newestEvidenceAt ? new Date(cell.newestEvidenceAt).toLocaleDateString() : "—";
      rectangle.bindTooltip(
        `<strong>${label}</strong><br/>Cell ${escapeHtml(cell.cellId)} · ${cell.reportCount} report(s), verified ${cell.verifiedCount}<br/>Newest: ${escapeHtml(newest)}${cell.staleCount ? ` · stale ${cell.staleCount}` : ""}<br/><em>C is unknown, not proof of clear.</em>`,
        { sticky: true }
      );
      rectangle.bindPopup(
        `<strong>${label}</strong><br/>Centre ${cell.centerLatitude.toFixed(5)}, ${cell.centerLongitude.toFixed(5)}<br/>Reports: ${cell.reportCount}, verified: ${cell.verifiedCount}<br/>Newest evidence: ${escapeHtml(newest)}<br/><em>No cell is proven safe. Pan to see reports inside it.</em>`
      );
      rectangle.on("click", () => {
        setSelectedCell(cell);
        const map = mapRef.current as import("leaflet").Map | null;
        if (map) map.fitBounds(bounds, { maxZoom: 17 });
      });
      (rectangle as unknown as { options: Record<string, unknown> }).options["ariaLabel"] = `Coverage cell ${cell.cellId}, ${label}`;
      rectangle.addTo(layer);

      const letterIcon = L.divIcon({
        className: "coverage-letter",
        html: `<span class="coverage-letter-inner coverage-letter-${letter.toLowerCase()}" aria-hidden="true">${letter}</span>`,
        iconSize: [18, 18],
        iconAnchor: [9, 9],
      });
      L.marker([cell.centerLatitude, cell.centerLongitude], { icon: letterIcon, interactive: false, keyboard: false }).addTo(layer);
    }
  }

  async function fetchCoverage(viewport: MapViewport) {
    if (!showCoverageRef.current) return;
    setCoverageLoading(true);
    const result = await getCoverage(viewport, undefined, {
      obstacleType: filtersRef.current.obstacleType || undefined,
      verificationStatus: filtersRef.current.verificationStatus || undefined,
    });
    setCoverageLoading(false);
    if (result === null) return;
    setCoverage(result);
    renderCoverage(result.cells);
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
    void fetchCoverage(viewport);
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
        html: `<span class="map-marker-pin map-marker-${report.verificationStatus.toLowerCase()}" title="${escapeHtml(report.title)}"><span class="map-marker-letter" aria-hidden="true">${markerLetter(report)}</span><span class="sr-only">${report.verificationStatus} ${report.obstacleType}</span></span>`,
        iconSize: [30, 30],
        iconAnchor: [15, 15],
      });

      const marker = L.marker([report.latitude, report.longitude], {
        icon,
        title: report.title,
        alt: `${report.title} — ${report.verificationStatus} — ${report.obstacleType}`,
        keyboard: true,
        riseOnHover: true,
      });
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
      fixLeafletIcons(L);
      const map = L.map(containerRef.current).setView(DEFAULT_CENTER, 13);
      mapRef.current = map;

      L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution:
          '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      }).addTo(map);

      coverageLayerRef.current = L.layerGroup().addTo(map);
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

      let moveDebounce: ReturnType<typeof setTimeout> | null = null;
      map.on("moveend", () => {
        if (moveDebounce) clearTimeout(moveDebounce);
        moveDebounce = setTimeout(loadFromBounds, 400);
      });
      loadFromBounds();
    }

    void init();
    return () => {
      cancelled = true;
      const map = mapRef.current as import("leaflet").Map | null;
      map?.remove();
      mapRef.current = null;
      coverageLayerRef.current = null;
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

  // Coverage toggle: fetch or clear layer.
  useEffect(() => {
    const map = mapRef.current as import("leaflet").Map | null;
    if (!map) return;
    if (!showCoverage) {
      const layer = coverageLayerRef.current as import("leaflet").LayerGroup | null;
      layer?.clearLayers();
      setCoverage(null);
      return;
    }
    const bounds = map.getBounds();
    void fetchCoverage({
      minLatitude: bounds.getSouth(),
      maxLatitude: bounds.getNorth(),
      minLongitude: bounds.getWest(),
      maxLongitude: bounds.getEast(),
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [showCoverage]);

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
        <button type="button" onClick={useMyLocation} className="btn btn--brand">
          Use my location
        </button>
        <label className="switch">
          <input
            type="checkbox"
            checked={showCameras}
            onChange={(event) => setShowCameras(event.target.checked)}
          />{" "}
          Show cameras — square <span className="mono">C</span> markers
        </label>
        <label className="switch">
          <input
            type="checkbox"
            checked={showCoverage}
            onChange={(event) => setShowCoverage(event.target.checked)}
          />{" "}
          Coverage layer — where we have / do not have evidence
        </label>
      </div>

      {showCoverage && (
        <section className="coverage-summary" aria-live="polite">
          {coverage ? (
            <>
              <h3>Evidence coverage in this view</h3>
              <p>
                <strong>{coverage.summary.totalCells} cells</strong> ·{" "}
                {coverage.summary.coveredCells} with evidence,{" "}
                {coverage.summary.insufficientCells} without · cell size ~
                {Math.round(coverage.cellSizeMeters)} m
                {coverageLoading ? " · updating…" : ""}
              </p>
              <div className="coverage-legend">
                <span>
                  <span className="swatch swatch--c" aria-hidden="true" />C — Insufficient (no data)
                </span>
                <span>
                  <span className="swatch swatch--b" aria-hidden="true" />B — Obstacle evidence
                </span>
                <span>
                  <span className="swatch swatch--a" aria-hidden="true" />A — Evidence present
                </span>
              </div>
              <p className="hint">
                Each cell is a summary, not a verdict. A “C” cell may still contain a usable path — it only
                means nobody has reported there yet. The letter inside each cell matches its colour, so the
                map is never readable by colour alone.
              </p>
              {selectedCell && (
                <div className="coverage-selected" role="status">
                  <strong>
                    Selected cell <span className="mono">{selectedCell.cellId}</span>
                  </strong>
                  <span>
                    {selectedCell.status === "InsufficientInfo"
                      ? "C — Insufficient: no data, travel with caution"
                      : selectedCell.status === "ObstacleEvidence"
                        ? `B — Obstacle evidence (${selectedCell.reportCount} reports)`
                        : `A — Evidence present (${selectedCell.reportCount} reports)`}
                  </span>
                  <span className="mono">
                    centre {selectedCell.centerLatitude.toFixed(5)}, {selectedCell.centerLongitude.toFixed(5)}{" "}
                    · verified {selectedCell.verifiedCount} · stale {selectedCell.staleCount}
                  </span>
                  <span className="hint">
                    {reports.filter(
                      (r) =>
                        r.latitude >= selectedCell.minLatitude &&
                        r.latitude <= selectedCell.maxLatitude &&
                        r.longitude >= selectedCell.minLongitude &&
                        r.longitude <= selectedCell.maxLongitude,
                    ).length}{" "}
                    loaded reports fall inside this cell — pick one from the list, or pan to see them.
                  </span>
                  <span>
                    <button type="button" className="btn btn--ghost btn--sm" onClick={() => setSelectedCell(null)}>
                      Clear selection
                    </button>
                  </span>
                </div>
              )}
            </>
          ) : (
            <p className="coverage-empty">
              {coverageLoading
                ? "Loading coverage…"
                : "Pan to San Francisco — coverage will appear here. No cell is proven safe."}{" "}
              <em>
                C grey hatched = insufficient, B amber = obstacle evidence, A green = evidence present.
              </em>
            </p>
          )}
        </section>
      )}

      {locationNote && (
        <p role="status" className="notice">
          {locationNote}
        </p>
      )}
      {loading ? (
        <div className="skeleton" aria-live="polite">
          <div className="sk-line sk-line--sm" />
          <div className="sk-card" />
        </div>
      ) : null}
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
            The list and the map always show the same reports. Marker letters mean{" "}
            <span className="tag">! unverified</span> <span className="tag">V verified</span>{" "}
            <span className="tag">? disputed</span> <span className="tag">X rejected</span>. Square{" "}
            <span className="tag">C</span> markers are cameras, kept separate from obstacle reports.
          </p>

          {selected && (
            <article className="selected-report" aria-live="polite">
              <h3>{selected.title}</h3>
              <p>
                <StatusBadge status={selected.verificationStatus} /> {selected.obstacleType} · Severity{" "}
                {selected.severity}
              </p>
              <p>
                Reported {new Date(selected.createdAt).toLocaleString()}. This is a user claim until a
                reviewer verifies it.
              </p>
              <Link href={`/reports/${selected.id}`} className="btn btn--primary btn--sm">
                Open full report
              </Link>
            </article>
          )}

          <h3>Cameras in this area ({camerasInView.length})</h3>
          {camerasInView.length === 0 ? (
            <p className="callout callout--unknown">
              <strong>No automatic camera source here.</strong> That area only updates when a person reports
              or uploads evidence — no camera coverage is not proof that paths are clear.
            </p>
          ) : (
            <ul className="report-list">
              {camerasInView.map((camera) => (
                <li key={camera.id}>
                  <div className="list-row__head">
                    <span className="list-row__title">{camera.label}</span>
                    <span
                      className={`pill ${camera.enabled && !camera.isStale ? "pill--on" : camera.enabled ? "pill--off" : "pill--unknown"}`}
                    >
                      {!camera.enabled ? "Disabled" : camera.isStale ? "Stale" : "Live"}
                    </span>
                  </div>
                  <span className="list-row__meta">{camera.provider} · {camera.city}</span>
                  <span className="hint">
                    {!camera.enabled
                      ? "Disabled — it contributes no evidence at all."
                      : camera.isStale
                        ? `Stale — last successful check ${camera.lastSuccessAt ? new Date(camera.lastSuccessAt).toLocaleString() : "never"}. Its data may be old.`
                        : `Last checked ${camera.lastSuccessAt ? new Date(camera.lastSuccessAt).toLocaleString() : "never"}${camera.lastChangeAt ? `, last change ${new Date(camera.lastChangeAt).toLocaleString()}` : ""}.`}
                  </span>
                </li>
              ))}
            </ul>
          )}

          {reports.length === 0 && !loading ? (
            <p className="empty-state">
              <strong>No reports in this area yet.</strong>
              <span>
                No reports does not mean the paths here are clear — it means nobody has reported yet.
              </span>
            </p>
          ) : (
            <ul className="report-list">
              {reports.map((report) => (
                <li key={report.id}>
                  <div className="list-row__head">
                    <button type="button" onClick={() => setSelected(report)} className="list-select">
                      {report.title}
                    </button>
                    <StatusBadge status={report.verificationStatus} />
                  </div>
                  <span className="list-row__meta">
                    <span className="meta-chip">{report.obstacleType}</span>
                    <span className="meta-chip">
                      Severity <strong>{report.severity}</strong>
                    </span>
                    <span className="meta-chip">{new Date(report.createdAt).toLocaleString()}</span>
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
