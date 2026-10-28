"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import "leaflet/dist/leaflet.css";
import {
  recommendRoutes,
  type EvaluatedRoute,
  type RouteRecommendation,
} from "@/lib/api-client";

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
  const leafletRef = useRef<typeof import("leaflet") | null>(null);

  const [originLat, setOriginLat] = useState("23.8103");
  const [originLon, setOriginLon] = useState("90.4125");
  const [destLat, setDestLat] = useState("23.8150");
  const [destLon, setDestLon] = useState("90.4200");
  const [pickMode, setPickModeState] = useState<PickMode>("origin");
  const pickModeRef = useRef<PickMode>("origin");

  function setPickMode(mode: PickMode) {
    pickModeRef.current = mode;
    setPickModeState(mode);
  }
  const [recommendation, setRecommendation] = useState<RouteRecommendation | null>(null);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function init() {
      const L = await import("leaflet");
      if (cancelled || !containerRef.current || mapRef.current) {
        return;
      }

      leafletRef.current = L;
      const map = L.map(containerRef.current).setView([23.8125, 90.416], 14);
      mapRef.current = map;

      L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution:
          '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      }).addTo(map);

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
      layerRef.current = null;
    };
  }, []);

  function drawRoutes(data: RouteRecommendation, highlight: number) {
    const L = leafletRef.current;
    const layer = layerRef.current;
    const map = mapRef.current;
    if (!L || !layer || !map) {
      return;
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
    const result = await recommendRoutes(origin, destination);
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
            {route.knownObstacles.map((obstacle) => (
              <li key={obstacle.reportId}>
                <Link href={`/reports/${obstacle.reportId}`}>{obstacle.title}</Link> —{" "}
                {obstacle.obstacleType}, {obstacle.verificationStatus}, about{" "}
                {Math.round(obstacle.distanceMeters)} m from the route, evidence{" "}
                {new Date(obstacle.evidenceAt).toLocaleDateString()}
              </li>
            ))}
          </ul>
        </>
      )}

      <button type="button" onClick={onSelect} disabled={selected} className="primary-button">
        {selected ? "Showing on map" : "Show on map"}
      </button>
    </article>
  );
}
