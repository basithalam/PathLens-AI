"use client";

import { useEffect, useRef } from "react";
import { fixLeafletIcons } from "@/lib/leaflet-icons";

export function MiniMapPicker({ latitude, longitude, onPick }: { latitude: number | null; longitude: number | null; onPick: (lat: number, lon: number) => void }) {
  const ref = useRef<HTMLDivElement>(null);
  const mapRef = useRef<unknown>(null);
  const markerRef = useRef<unknown>(null);
  const leafletRef = useRef<typeof import("leaflet") | null>(null);
  const onPickRef = useRef(onPick);
  onPickRef.current = onPick;

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const L = await import("leaflet");
      if (cancelled || !ref.current || mapRef.current) return;
      leafletRef.current = L;
      fixLeafletIcons(L);
      const center: [number, number] = latitude !== null && longitude !== null ? [latitude, longitude] : [24.9116, 91.8498];
      const map = L.map(ref.current, { zoomControl: true }).setView(center, 15);
      mapRef.current = map as unknown;
      L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", { maxZoom: 19, attribution: '&copy; OpenStreetMap contributors' }).addTo(map);
      if (latitude !== null && longitude !== null) {
        markerRef.current = L.marker([latitude, longitude], { draggable: true }).addTo(map) as unknown;
        (markerRef.current as import("leaflet").Marker).on("dragend", (e: unknown) => {
          const pos = (e as { target: import("leaflet").Marker }).target.getLatLng();
          onPickRef.current(pos.lat, pos.lng);
        });
      }
      map.on("click", (e: unknown) => {
        const { lat, lng } = (e as { latlng: { lat: number; lng: number } }).latlng;
        const existing = markerRef.current as import("leaflet").Marker | null;
        if (existing) existing.setLatLng([lat, lng]);
        else {
          const m = L.marker([lat, lng], { draggable: true }).addTo(map);
          markerRef.current = m as unknown;
          m.on("dragend", (ev: unknown) => {
            const p = (ev as { target: import("leaflet").Marker }).target.getLatLng();
            onPickRef.current(p.lat, p.lng);
          });
        }
        onPickRef.current(lat, lng);
      });
      setTimeout(() => map.invalidateSize(), 100);
    })();
    return () => { cancelled = true; (mapRef.current as import("leaflet").Map | null)?.remove(); mapRef.current = null; };
  }, []);

  useEffect(() => {
    const L = leafletRef.current;
    const map = mapRef.current as import("leaflet").Map | null;
    const marker = markerRef.current as import("leaflet").Marker | null;
    if (!L || !map) return;
    if (latitude !== null && longitude !== null) {
      map.setView([latitude, longitude], map.getZoom());
      if (marker) marker.setLatLng([latitude, longitude]);
      else {
        const m = L.marker([latitude, longitude], { draggable: true }).addTo(map);
        markerRef.current = m as unknown;
        m.on("dragend", (ev: unknown) => {
          const p = (ev as { target: import("leaflet").Marker }).target.getLatLng();
          onPickRef.current(p.lat, p.lng);
        });
      }
    }
  }, [latitude, longitude]);

  return <div ref={ref} style={{ width: "100%", height: "100%" }} role="application" aria-label="Map picker — click to set report location, drag marker to adjust" />;
}
