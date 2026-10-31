"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import "leaflet/dist/leaflet.css";
import { fixLeafletIcons } from "@/lib/leaflet-icons";
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

  const [originLat, setOriginLat] = useState("37.7749");
  const [originLon, setOriginLon] = useState("-122.4194");
  const [destLat, setDestLat] = useState("37.7790");
  const [destLon, setDestLon] = useState("-122.4100");
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
        <fieldset>
          <legend>Start</legend>
          <label>
            Latitude
            <input value={originLat} onChange={(event) => setOriginLat(event.target.value)} inputMode="decimal" />
          </label>
          <label>
            Longitude
            <input value={originLon} onChange={(event) => setOriginLon(event.target.value)} inputMode="decimal" />
          </label>
          <button type="button" onClick={() => setPickMode("origin")} aria-pressed={pickMode === "origin"}>
            Pick start on map
          </button>
        </fieldset>

        <fieldset>
          <legend>Destination</legend>
          <label>
            Latitude
            <input value={destLat} onChange={(event) => setDestLat(event.target.value)} inputMode="decimal" />
          </label>
          <label>
            Longitude
            <input value={destLon} onChange={(event) => setDestLon(event.target.value)} inputMode="decimal" />
          </label>
          <button
            type="button"
            onClick={() => setPickMode("destination")}
            aria-pressed={pickMode === "destination"}
          >
            Pick destination on map
          </button>
        </fieldset>

        <fieldset>
          <legend>Your route preferences (saved on this device only)</legend>
          <p>
            Preferences reorder routes. They never hide a known obstacle, and
            unknown stays unknown whatever you choose.
          </p>
          <label>
            <input
              type="checkbox"
              checked={preferences.avoidStairs}
              onChange={(e) => updatePreference("avoidStairs", e.target.checked)}
            />{" "}
            Avoid stairs — a route with a human-verified stair report cannot
            outrank one without
          </label>
          <br />
          <label>
            <input
              type="checkbox"
              checked={preferences.preferVerifiedEvidence}
              onChange={(e) => updatePreference("preferVerifiedEvidence", e.target.checked)}
            />{" "}
            Prefer verified evidence — fewer uncertain obstacles ranks higher
          </label>
          <br />
          <label>
            <input
              type="checkbox"
              checked={preferences.avoidStaleEvidence}
              onChange={(e) => updatePreference("avoidStaleEvidence", e.target.checked)}
            />{" "}
            Avoid stale evidence — fewer obstacles based on old evidence ranks higher
          </label>
          <br />
          <label>
            <input
              type="checkbox"
              checked={preferences.preferShortest}
              onChange={(e) => updatePreference("preferShortest", e.target.checked)}
            />{" "}
            Prefer shortest — distance counts earlier in ranking
          </label>
        </fieldset>

        <label style={{ marginTop: "0.6rem" }}>
          <input type="checkbox" checked={showCoverage} onChange={(e) => setShowCoverage(e.target.checked)} /> Coverage layer under routes — see where evidence exists vs insufficient (C grey = insufficient, B orange = obstacle, A green = evidence; letter+colour, never colour alone)
        </label>
        {showCoverage && coverage && (
          <p style={{ fontSize: "0.9rem", background: "var(--wash)", padding: "0.5rem 0.7rem", borderRadius: 8 }}>
            Covering {coverage.summary.totalCells} cells · {coverage.summary.coveredCells} with evidence, {coverage.summary.insufficientCells} without — <em>Unknown is not clear; route may cross C cells.</em>{" "}
            {coverageLoading && "Updating…"} Cell size ~{Math.round(coverage.cellSizeMeters)} m.
          </p>
        )}

        <p>
          Clicking the map fills the {pickMode}. Then plan the trip. Routes use a
          walking profile; that is not a validated wheelchair profile, so no route
          is ever labelled wheelchair-safe here.
        </p>

        <button type="button" onClick={plan} disabled={busy} className="primary-button">
          {busy ? "Asking the routing provider…" : "Plan routes"}
        </button>

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

      {recommendation && (
        <section aria-labelledby="route-results-heading" className="route-results">
          <h2 id="route-results-heading">Candidate routes</h2>
          <p>
            From {recommendation.provider} ({recommendation.profile} profile),
            ranked by fewest known obstacles. Ranked first never means proven safe.
          </p>

          {recommendation.limitations.map((limitation) => (
            <p key={limitation} className="limitations">
              {limitation}
            </p>
          ))}

          <p>
            <strong>Preferences applied:</strong>{" "}
            {recommendation.appliedPreferences.join(" ")}
          </p>

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
  return (
    <article className={selected ? "route-card selected" : "route-card"}>
      <h3>
        Route {route.index + 1} · {formatDistance(route.distanceMeters)} · about{" "}
        {Math.round(route.durationSeconds / 60)} min
      </h3>

      {route.hasConfirmedStairs && (
        <p role="note" className="mock-label">
          This route has a human-verified stair report. If stairs are a hard
          barrier for you, do not choose it on distance alone.
        </p>
      )}

      {route.newestEvidenceAt ? (
        <p>
          Newest evidence near this route:{" "}
          {new Date(route.newestEvidenceAt).toLocaleString()}. Fresher evidence
          can come from people or from automatic camera checks; neither proves
          the whole route is accessible.
        </p>
      ) : (
        <p>No evidence near this route yet — it is unknown, not proven clear.</p>
      )}

      {route.unknownLengthPercent > 50 && (
        <p role="note" className="mock-label">
          Mostly unknown: {route.unknownLengthPercent.toFixed(0)}% of this route has
          no nearby report. Unknown is not the same as clear.
        </p>
      )}

      <ul>
        {route.explanation.map((line) => (
          <li key={line}>{line}</li>
        ))}
      </ul>

      {route.knownObstacles.length > 0 && (
        <>
          <p>
            <strong>Known obstacles on this route</strong>
          </p>
          <ul>
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
                  <Link href={`/reports/${obstacle.reportId}`}>{obstacle.title}</Link> —{" "}
                  {obstacle.obstacleType}, {obstacle.verificationStatus}, about{" "}
                  {Math.round(obstacle.distanceMeters)} m from the route, evidence{" "}
                  {new Date(obstacle.evidenceAt).toLocaleDateString()} ({ageDays} day(s) old
                  {ageDays > 30 ? " — stale (H3.3 StaleEvidenceDays=30 default, backend configurable)" : ""}) · {certainty}
                  {obstacle.isCameraCandidate
                    ? " — a person has not confirmed this."
                    : ""}
                  {obstacle.hasStairs !== null || obstacle.hasRamp !== null || obstacle.surface !== null
                    ? ` · Observed: ${[
                        obstacle.hasStairs !== null ? `stairs ${obstacle.hasStairs ? "yes" : "no"}` : null,
                        obstacle.hasRamp !== null ? `ramp ${obstacle.hasRamp ? "yes" : "no"}` : null,
                        obstacle.surface !== null ? `surface ${obstacle.surface}` : null,
                      ]
                        .filter(Boolean)
                        .join(", ")}`
                    : " · Stairs/ramp/surface not measured here (unknown, not estimated)"}
                </li>
              );
            })}
          </ul>
        </>
      )}

      <button type="button" onClick={onSelect} disabled={selected} className="primary-button">
        {selected ? "Showing on map" : "Show on map"}
      </button>
    </article>
  );
}
