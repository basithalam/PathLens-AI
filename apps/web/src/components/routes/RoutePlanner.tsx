"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import "leaflet/dist/leaflet.css";
import { fixLeafletIcons } from "@/lib/leaflet-icons";
import { StatusBadge } from "@/components/StatusBadge";
import {
  getCoverage,
  recommendRoutes,
  DEFAULT_ROUTE_PREFERENCES,
  type CoverageResponse,
  type EvaluatedRoute,
  type MapViewport,
  type RoutePreferences,
  type RouteRecommendation,
} from "@/lib/api-client";

const PREFERENCES_KEY = "pathlens.routePreferences";

function loadPreferences(): RoutePreferences {
  try {
    const raw = window.localStorage.getItem(PREFERENCES_KEY);
    if (raw) {
      return { ...DEFAULT_ROUTE_PREFERENCES, ...(JSON.parse(raw) as Partial<RoutePreferences>) };
    }
  } catch {
    // Corrupt storage falls back to defaults.
  }
  return DEFAULT_ROUTE_PREFERENCES;
}

type PickMode = "origin" | "destination";

const ROUTE_COLORS = ["#0b6b4f", "#8a5a00", "#6b4e9e"];

function escapeHtml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function formatDistance(meters: number): string {
  return meters >= 1000 ? `${(meters / 1000).toFixed(2)} km` : `${Math.round(meters)} m`;
}

export function RoutePlanner() {
  const containerRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<import("leaflet").Map | null>(null);
  const layerRef = useRef<import("leaflet").LayerGroup | null>(null);
  const coverageLayerRef = useRef<import("leaflet").LayerGroup | null>(null);
  const leafletRef = useRef<typeof import("leaflet") | null>(null);

  const [originLat, setOriginLat] = useState("24.9116");
  const [originLon, setOriginLon] = useState("91.8498");
  const [destLat, setDestLat] = useState("24.9248");
  const [destLon, setDestLon] = useState("91.8698");
  const [pickMode, setPickModeState] = useState<PickMode>("origin");
  const pickModeRef = useRef<PickMode>("origin");

  function setPickMode(mode: PickMode) {
    pickModeRef.current = mode;
    setPickModeState(mode);
  }
  const [preferences, setPreferences] = useState<RoutePreferences>(DEFAULT_ROUTE_PREFERENCES);

  useEffect(() => {
    setPreferences(loadPreferences());
  }, []);

  function updatePreference<K extends keyof RoutePreferences>(key: K, value: RoutePreferences[K]) {
    setPreferences((current) => {
      const next = { ...current, [key]: value };
      try {
        window.localStorage.setItem(PREFERENCES_KEY, JSON.stringify(next));
      } catch {
        // Storage unavailable: preferences still apply to this session.
      }
      return next;
    });
  }

  const [recommendation, setRecommendation] = useState<RouteRecommendation | null>(null);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [coverage, setCoverage] = useState<CoverageResponse | null>(null);
  const [showCoverage, setShowCoverage] = useState(false);
  const [coverageLoading, setCoverageLoading] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function init() {
      const L = await import("leaflet");
      if (cancelled || !containerRef.current || mapRef.current) {
        return;
      }

      leafletRef.current = L;
      fixLeafletIcons(L);
      const map = L.map(containerRef.current).setView([37.7749, -122.4194], 14);
      mapRef.current = map;

      L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution:
          '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      }).addTo(map);

      coverageLayerRef.current = L.layerGroup().addTo(map);
      layerRef.current = L.layerGroup().addTo(map);

      // Map clicks fill whichever endpoint is currently being picked.
      map.on("click", (event: import("leaflet").LeafletMouseEvent) => {
        if (pickModeRef.current === "origin") {
          setOriginLat(event.latlng.lat.toFixed(6));
          setOriginLon(event.latlng.lng.toFixed(6));
          setPickMode("destination");
        } else {
          setDestLat(event.latlng.lat.toFixed(6));
          setDestLon(event.latlng.lng.toFixed(6));
          setPickMode("origin");
        }
      });
    }

    void init();
    return () => {
      cancelled = true;
      mapRef.current?.remove();
      mapRef.current = null;
      coverageLayerRef.current = null;
      layerRef.current = null;
    };
  }, []);

  function renderCoverage(cells: import("@/lib/api-client").CoverageCell[]) {
    const L = leafletRef.current;
    const layer = coverageLayerRef.current as unknown as import("leaflet").LayerGroup | null;
    if (!L || !layer) return;
    layer.clearLayers();
    if (!showCoverage) return;
    for (const cell of cells) {
      const bounds: [[number, number], [number, number]] = [
        [cell.minLatitude, cell.minLongitude],
        [cell.maxLatitude, cell.maxLongitude],
      ];
      const isObstacle = cell.status === "ObstacleEvidence";
      const isInsufficient = cell.status === "InsufficientInfo";
      const letter = isInsufficient ? "C" : isObstacle ? "B" : "A";
      const label = isInsufficient ? "C — Insufficient" : isObstacle ? `B — Obstacle (${cell.reportCount})` : `A — Evidence`;
      const rect = L.rectangle(bounds, {
        color: isInsufficient ? "#8a8a8a" : isObstacle ? "#8a5a00" : "#0b6b4f",
        weight: 1,
        opacity: 0.5,
        fillColor: isInsufficient ? "#c9d2cd" : isObstacle ? "#ffcc80" : "#a8e6cf",
        fillOpacity: 0.28,
      });
      rect.bindTooltip(`<strong>${label}</strong><br/>Cell ${cell.cellId} · ${cell.reportCount} reports<br/><em>Unknown ≠ clear</em>`, { sticky: true });
      rect.addTo(layer);
      const letterIcon = L.divIcon({
        className: "coverage-letter",
        html: `<span class="coverage-letter-inner coverage-letter-${letter.toLowerCase()}">${letter}</span>`,
        iconSize: [16, 16],
        iconAnchor: [8, 8],
      });
      L.marker([cell.centerLatitude, cell.centerLongitude], { icon: letterIcon, interactive: false, keyboard: false }).addTo(layer);
    }
  }

  async function fetchCoverage(viewport: MapViewport) {
    if (!showCoverage) return;
    setCoverageLoading(true);
    const result = await getCoverage(viewport);
    setCoverageLoading(false);
    if (!result) return;
    setCoverage(result);
    renderCoverage(result.cells);
  }

  function drawRoutes(data: RouteRecommendation, highlight: number) {
    const L = leafletRef.current;
    const layer = layerRef.current;
    const map = mapRef.current;
    if (!L || !layer || !map) {
      return;
    }

    // Coverage drawn underneath routes, so clear coverage first then routes
    if (showCoverage) {
      const bounds = ((): MapViewport | null => {
        const lats = data.routes.flatMap(r => r.geometry.map(p => p.latitude));
        const lons = data.routes.flatMap(r => r.geometry.map(p => p.longitude));
        if (lats.length === 0) return null;
        return { minLatitude: Math.min(...lats), maxLatitude: Math.max(...lats), minLongitude: Math.min(...lons), maxLongitude: Math.max(...lons) };
      })();
      if (bounds) void fetchCoverage(bounds);
    }

    layer.clearLayers();

    L.marker([data.origin.latitude, data.origin.longitude])
      .addTo(layer)
      .bindPopup("Start");
    L.marker([data.destination.latitude, data.destination.longitude])
      .addTo(layer)
      .bindPopup("Destination");

    data.routes.forEach((route, index) => {
      const line = L.polyline(
        route.geometry.map((point) => [point.latitude, point.longitude] as [number, number]),
        {
          color: ROUTE_COLORS[index % ROUTE_COLORS.length],
          weight: index === highlight ? 6 : 3,
          opacity: index === highlight ? 1 : 0.55,
        },
      ).addTo(layer);

      for (const obstacle of route.knownObstacles) {
        L.circleMarker([obstacle.latitude, obstacle.longitude], {
          radius: index === highlight ? 8 : 5,
          color: ROUTE_COLORS[index % ROUTE_COLORS.length],
          weight: 2,
          fillColor: "#ffffff",
          fillOpacity: 1,
        })
          .addTo(layer)
          .bindPopup(
            `<strong>Known obstacle</strong><br/><a href="/reports/${obstacle.reportId}">${escapeHtml(obstacle.title)}</a><br/>${escapeHtml(obstacle.obstacleType)} · ${escapeHtml(obstacle.verificationStatus)}`,
          );
      }

      if (index === highlight) {
        map.fitBounds(line.getBounds(), { padding: [24, 24] });
      }
    });

  }

  useEffect(() => {
    if (recommendation) {
      drawRoutes(recommendation, selectedIndex);
    }
    // drawRoutes reads current refs and state.
  }, [recommendation, selectedIndex]);

  useEffect(() => {
    if (!showCoverage) {
      (coverageLayerRef.current as unknown as import("leaflet").LayerGroup | null)?.clearLayers();
      setCoverage(null);
      return;
    }
    if (recommendation) drawRoutes(recommendation, selectedIndex);
    else if (mapRef.current) {
      const b = (mapRef.current as import("leaflet").Map).getBounds();
      void fetchCoverage({ minLatitude: b.getSouth(), maxLatitude: b.getNorth(), minLongitude: b.getWest(), maxLongitude: b.getEast() });
    }
  }, [showCoverage]);

  async function plan() {
    setMessage(null);

    const origin = { latitude: Number(originLat), longitude: Number(originLon) };
    const destination = { latitude: Number(destLat), longitude: Number(destLon) };

    if (
      [origin.latitude, origin.longitude, destination.latitude, destination.longitude].some(
        (value) => Number.isNaN(value),
      )
    ) {
      setMessage("Coordinates must be numbers. Use the map or type latitude/longitude.");
      return;
    }

    setBusy(true);
    const result = await recommendRoutes(origin, destination, preferences);
    setBusy(false);

    if (result.ok) {
      setRecommendation(result.recommendation);
      setSelectedIndex(0);
    } else {
      setRecommendation(null);
      setMessage(result.message);
    }
  }

  return (
    <>
      <div className="route-form">
        <div className="route-panel">
          <p className="route-panel__title">
            Two ways to set an endpoint — type coordinates, search an address, or tap the map.
          </p>

          <div className="endpoint endpoint--from">
            <div className="endpoint__field">
              <span className="endpoint__label" id="from-label">
                From — start
              </span>
              <div className="endpoint__inputs">
                <input
                  value={originLat}
                  onChange={(e) => setOriginLat(e.target.value)}
                  inputMode="decimal"
                  placeholder="Lat 24.9116"
                  aria-label="Origin latitude"
                />
                <input
                  value={originLon}
                  onChange={(e) => setOriginLon(e.target.value)}
                  inputMode="decimal"
                  placeholder="Lon 91.8498"
                  aria-label="Origin longitude"
                />
              </div>
              <input
                aria-labelledby="from-label"
                placeholder="Search address — e.g. Zindabazar, Sylhet"
                onKeyDown={async (e) => { if (e.key === "Enter") { e.preventDefault(); const q=(e.target as HTMLInputElement).value; if(!q) return; const r=await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(q)}&limit=1`); const j=await r.json(); if(j[0]){ setOriginLat(j[0].lat); setOriginLon(j[0].lon); }}}}
              />
            </div>
            <button
              type="button"
              onClick={() => setPickMode("origin")}
              aria-pressed={pickMode === "origin"}
              className="btn btn--ghost pick-btn"
            >
              {pickMode === "origin" ? "✓ Tap the map…" : "Pick on map"}
            </button>
          </div>

          <div className="endpoint endpoint--to">
            <div className="endpoint__field">
              <span className="endpoint__label" id="to-label">
                To — destination
              </span>
              <div className="endpoint__inputs">
                <input
                  value={destLat}
                  onChange={(e) => setDestLat(e.target.value)}
                  inputMode="decimal"
                  placeholder="Lat 24.9248"
                  aria-label="Destination latitude"
                />
                <input
                  value={destLon}
                  onChange={(e) => setDestLon(e.target.value)}
                  inputMode="decimal"
                  placeholder="Lon 91.8698"
                  aria-label="Destination longitude"
                />
              </div>
              <input
                aria-labelledby="to-label"
                placeholder="Search address — e.g. Amberkhana, Sylhet"
                onKeyDown={async (e) => { if (e.key === "Enter") { e.preventDefault(); const q=(e.target as HTMLInputElement).value; if(!q) return; const r=await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(q)}&limit=1`); const j=await r.json(); if(j[0]){ setDestLat(j[0].lat); setDestLon(j[0].lon); }}}}
              />
            </div>
            <button
              type="button"
              onClick={() => setPickMode("destination")}
              aria-pressed={pickMode === "destination"}
              className="btn btn--ghost pick-btn"
            >
              {pickMode === "destination" ? "✓ Tap the map…" : "Pick on map"}
            </button>
          </div>

          <p className="route-hint">
            The next map tap fills <strong>{pickMode === "origin" ? "From" : "To"}</strong> — the active
            endpoint is highlighted. Green marks the start, red marks the destination.
          </p>
        </div>

        <fieldset>
          <legend>Your route preferences (saved on this device only)</legend>
          <p>
            Preferences reorder routes. They never hide a known obstacle, and unknown stays unknown whatever
            you choose.
          </p>
          <label className="switch">
            <input
              type="checkbox"
              checked={preferences.avoidStairs}
              onChange={(e) => updatePreference("avoidStairs", e.target.checked)}
            />{" "}
            <span>
              <strong>Avoid stairs</strong> — a route with a human-verified stair report cannot outrank one
              without
            </span>
          </label>
          <label className="switch">
            <input
              type="checkbox"
              checked={preferences.preferVerifiedEvidence}
              onChange={(e) => updatePreference("preferVerifiedEvidence", e.target.checked)}
            />{" "}
            <span>
              <strong>Prefer verified evidence</strong> — fewer uncertain obstacles ranks higher
            </span>
          </label>
          <label className="switch">
            <input
              type="checkbox"
              checked={preferences.avoidStaleEvidence}
              onChange={(e) => updatePreference("avoidStaleEvidence", e.target.checked)}
            />{" "}
            <span>
              <strong>Avoid stale evidence</strong> — fewer obstacles based on old evidence ranks higher
            </span>
          </label>
          <label className="switch">
            <input
              type="checkbox"
              checked={preferences.preferShortest}
              onChange={(e) => updatePreference("preferShortest", e.target.checked)}
            />{" "}
            <span>
              <strong>Prefer shortest</strong> — distance counts earlier in ranking
            </span>
          </label>
        </fieldset>

        <label className="switch">
          <input type="checkbox" checked={showCoverage} onChange={(e) => setShowCoverage(e.target.checked)} />{" "}
          <span>
            <strong>Coverage layer under routes</strong> — see where evidence exists versus where it is
            insufficient (C hatched grey = insufficient, B amber = obstacle, A green = evidence; letter and
            colour together, never colour alone)
          </span>
        </label>
        {showCoverage && coverage && (
          <p className="callout callout--unknown">
            Covering {coverage.summary.totalCells} cells · {coverage.summary.coveredCells} with evidence,{" "}
            {coverage.summary.insufficientCells} without · cell size ~{Math.round(coverage.cellSizeMeters)} m
            {coverageLoading ? " · updating…" : ""}.{" "}
            <em>Unknown is not clear — this route may cross C cells.</em>
          </p>
        )}

        <p className="hint">
          Clicking the map fills the <strong>{pickMode}</strong> endpoint, then plan the trip. Routes use a
          walking profile; that is not a validated wheelchair profile, so no route is ever labelled
          wheelchair-safe here.
        </p>

        <div className="auth__actions">
          <button type="button" onClick={plan} disabled={busy} className="btn btn--primary">
            {busy ? "Asking the routing provider…" : "Plan routes"}
          </button>
          {recommendation ? (
            <span className="hint">
              {recommendation.routes.length} candidate route{recommendation.routes.length === 1 ? "" : "s"} ·
              ranked by evidence, then distance
            </span>
          ) : null}
        </div>

        {message && (
          <p role="alert" className="status-error">
            {message}
          </p>
        )}
      </div>

      <div
        ref={containerRef}
        className="map-container route-map"
        role="application"
        aria-label="Route map. Route cards below describe every route in text."
      />

      {/* G4: Preference chips under map — always visible, mirrors checkbox state, no hidden ranking */}
      <div className="preference-chips" aria-label="Active route preferences">
        <span className="preference-chips__label">Preferences:</span>
        <span className={`pill ${preferences.avoidStairs ? "pill--on" : "pill--off"}`} title="A route with a human-verified stair report cannot outrank one without">
          Avoid stairs {preferences.avoidStairs ? "ON" : "OFF"}
        </span>
        <span className={`pill ${preferences.preferVerifiedEvidence ? "pill--on" : "pill--off"}`} title="Fewer uncertain obstacles ranks higher">
          Verified {preferences.preferVerifiedEvidence ? "ON" : "OFF"}
        </span>
        <span className={`pill ${preferences.avoidStaleEvidence ? "pill--on" : "pill--off"}`} title="Fewer obstacles based on old evidence ranks higher">
          Avoid stale {preferences.avoidStaleEvidence ? "ON" : "OFF"}
        </span>
        <span className={`pill ${preferences.preferShortest ? "pill--on" : "pill--off"}`} title="Distance counts earlier in ranking">
          Shortest {preferences.preferShortest ? "ON" : "OFF"}
        </span>
        <span className="status-source">
          Saved on this device · never hides known obstacles · unknown stays unknown
        </span>
      </div>

      {recommendation && (
        <section aria-labelledby="route-results-heading" className="route-results">
          <div className="panel__head" style={{ borderRadius: "var(--radius)" }}>
            <h2 id="route-results-heading">Candidate routes — with alternatives</h2>
            <span className="panel__count">
              {recommendation.provider} · {recommendation.profile} profile
            </span>
          </div>
          <p>
            Ranked by fewest known obstacles.{" "}
            <strong>When the top route is blocked, the next is your alternative</strong> — check its
            obstacles and unknown share before you go. Ranked first never means proven safe.
          </p>
          <p className="callout callout--info">
            PathLens never hides uncertainty: AI confidence is not a guarantee, and a route with no reports
            is shown as <em>unknown, not clear</em>. Alternatives are ranked by evidence, not just distance.
          </p>

          {recommendation.limitations.length > 0 && (
            <ul className="limitations">
              {recommendation.limitations.map((limitation) => (
                <li key={limitation}>{limitation}</li>
              ))}
            </ul>
          )}

          <div className="queue-actions">
            <strong className="queue-actions__label">Preferences applied</strong>
            {recommendation.appliedPreferences.length === 0 ? (
              <span className="pill pill--unknown">Default ranking — fewest known obstacles then shortest</span>
            ) : (
              recommendation.appliedPreferences.map((pref) => (
                <span key={pref} className="pill pill--on" title={`Applied: ${pref}`}>
                  {pref}
                </span>
              ))
            )}
          </div>

          <div className="route-cards">
            {recommendation.routes.map((route) => (
              <RouteCard
                key={route.index}
                route={route}
                selected={route.index === selectedIndex}
                onSelect={() => setSelectedIndex(route.index)}
              />
            ))}
          </div>
        </section>
      )}
    </>
  );
}

function RouteCard({
  route,
  selected,
  onSelect,
}: {
  route: EvaluatedRoute;
  selected: boolean;
  onSelect: () => void;
}) {
  const isBlocked = route.knownObstacles.length > 0;
  const isHighUnknown = route.unknownLengthPercent > 50;
  const isLead = route.index === 0 && !isBlocked && !isHighUnknown;
  const badge = isLead ? "Recommended" : isBlocked ? "Blocked — alternative below" : isHighUnknown ? "Mostly unknown" : `Alternative ${route.index + 1}`;
  const badgeClass = isLead
    ? "pill pill--on"
    : isBlocked
      ? "badge badge-bad"
      : isHighUnknown
        ? "badge badge-unknown"
        : "badge badge-neutral";
  const cardClass = [
    "route-card",
    isBlocked ? "route-card--blocked" : isLead ? "route-card--lead" : isHighUnknown ? "route-card--unknown" : "",
    selected ? "selected" : "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <article className={cardClass}>
      <span className="route-rank">
        Route {route.index + 1}
      </span>
      <h3>
        <span className="num">{formatDistance(route.distanceMeters)}</span>
        <span className="muted">· about {Math.round(route.durationSeconds / 60)} min</span>
        <span className={badgeClass}>{badge}</span>
      </h3>

      <div className="unknown-bar">
        <div className="unknown-bar__track" role="img" aria-label={`${route.unknownLengthPercent.toFixed(0)} percent of this route has no nearby evidence`}>
          <div
            className={`unknown-bar__fill ${isHighUnknown ? "unknown-bar__fill--unknown" : isBlocked ? "unknown-bar__fill--warn" : ""}`}
            style={{ width: `${Math.max(2, 100 - route.unknownLengthPercent)}%` }}
          />
        </div>
        <div className="unknown-bar__label">
          <span>{(100 - route.unknownLengthPercent).toFixed(0)}% has nearby evidence</span>
          <span>{route.unknownLengthPercent.toFixed(0)}% unknown</span>
        </div>
      </div>

      {isBlocked && route.index === 0 && (
        <p role="alert" className="callout callout--warn">
          This top-ranked route is still blocked by known obstacles. Check Alternative {route.index + 2}{" "}
          below — it may have fewer obstacles or less unknown. Ranked first never means proven safe.
        </p>
      )}
      {isBlocked && route.index > 0 && (
        <p role="note" className="status-source">
          Alternative: {route.knownObstacles.length === 0 ? "No known obstacles here, but " : `${route.knownObstacles.length} known obstacle(s) — `}
          {route.unknownLengthPercent.toFixed(0)}% unknown. Unknown is not clear.
        </p>
      )}

      {route.hasConfirmedStairs && (
        <p role="note" className="callout callout--bad">
          <strong>Human-verified stairs on this route.</strong> If stairs are a hard barrier for you, do not
          choose it on distance alone.
        </p>
      )}

      {route.newestEvidenceAt ? (
        <p className="hint">
          Newest evidence near this route:{" "}
          <time className="mono">{new Date(route.newestEvidenceAt).toLocaleString()}</time>. Fresher evidence
          can come from people or from automatic camera checks; neither proves the whole route is accessible.
        </p>
      ) : (
        <p role="note" className="callout callout--unknown">
          <strong>No evidence near this route yet.</strong> It is unknown, not proven clear.
        </p>
      )}

      {route.unknownLengthPercent > 50 && (
        <p role="note" className="mock-label">
          Mostly unknown: {route.unknownLengthPercent.toFixed(0)}% of this route has no nearby report.
          Unknown is not the same as clear.
        </p>
      )}

      <ul className="list-bullets">
        {route.explanation.map((line) => (
          <li key={line}>{line}</li>
        ))}
      </ul>

      {route.knownObstacles.length > 0 && (
        <>
          <h4>Known obstacles on this route</h4>
          <ul className="report-list">
            {route.knownObstacles.map((obstacle) => {
              const ageDays = Math.max(
                0,
                Math.floor((Date.now() - new Date(obstacle.evidenceAt).getTime()) / 86_400_000),
              );
              const certainty =
                obstacle.verificationStatus === "HumanVerified"
                  ? "Certain (human-verified)"
                  : obstacle.isCameraCandidate
                    ? "Uncertain (camera-detected candidate)"
                    : "Uncertain (not human-verified)";
              return (
                <li key={obstacle.reportId}>
                  <div className="list-row__head">
                    <Link href={`/reports/${obstacle.reportId}`} className="list-row__title">
                      {obstacle.title}
                    </Link>
                    <StatusBadge status={obstacle.verificationStatus} />
                  </div>
                  <div className="list-row__meta">
                    <span className="meta-chip">{obstacle.obstacleType}</span>
                    <span className="meta-chip">
                      ~<strong>{Math.round(obstacle.distanceMeters)} m</strong> from the route
                    </span>
                    <span className="meta-chip">
                      evidence <strong>{new Date(obstacle.evidenceAt).toLocaleDateString()}</strong> ·{" "}
                      {ageDays} d old
                      {ageDays > 30 ? " — stale (StaleEvidenceDays=30 default, backend configurable)" : ""}
                    </span>
                    <span className="meta-chip">{certainty}</span>
                  </div>
                  {obstacle.isCameraCandidate ? (
                    <span className="hint">A person has not confirmed this.</span>
                  ) : null}
                  <span className="hint">
                    {obstacle.hasStairs !== null || obstacle.hasRamp !== null || obstacle.surface !== null
                      ? `Observed: ${[
                          obstacle.hasStairs !== null ? `stairs ${obstacle.hasStairs ? "yes" : "no"}` : null,
                          obstacle.hasRamp !== null ? `ramp ${obstacle.hasRamp ? "yes" : "no"}` : null,
                          obstacle.surface !== null ? `surface ${obstacle.surface}` : null,
                        ]
                          .filter(Boolean)
                          .join(", ")}`
                      : "Stairs/ramp/surface not measured here (unknown, not estimated)"}
                  </span>
                </li>
              );
            })}
          </ul>
        </>
      )}

      <button type="button" onClick={onSelect} disabled={selected} className="btn btn--primary btn--sm">
        {selected ? "Showing on map" : "Show on map"}
      </button>
    </article>
  );
}
