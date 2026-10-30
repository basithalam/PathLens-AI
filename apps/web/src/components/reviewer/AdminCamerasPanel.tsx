"use client";

import { useCallback, useEffect, useState } from "react";
import { authFetch, getSessionUser, type SessionUser } from "@/lib/auth";

type Cam = {
  id: string;
  provider: string;
  label: string;
  city: string;
  feedImageUrl: string;
  enabled: boolean;
  hasPermissionNote: boolean;
  lastSuccessAt: string | null;
  lastError: string | null;
};

export function AdminCamerasPanel() {
  const [user, setUser] = useState<SessionUser | null>(null);
  const [mounted, setMounted] = useState(false);
  const [cameras, setCameras] = useState<Cam[]>([]);
  const [msg, setMsg] = useState<string | null>(null);
  const [provider, setProvider] = useState("Datumfeed");
  const [externalId, setExternalId] = useState("SF_001");
  const [label, setLabel] = useState("Market St & 5th — SF Demo");
  const [city, setCity] = useState("San Francisco");
  const [lat, setLat] = useState("37.7749");
  const [lon, setLon] = useState("-122.4194");
  const [feedUrl, setFeedUrl] = useState("https://datumfeed.com/api/cameras/SF_001/image");
  const [perm, setPerm] = useState("Datumfeed Developer Agreement 2026-10-30, powered by Datumfeed, SF demo");
  const [cadence, setCadence] = useState("60");

  const load = useCallback(async () => {
    const r = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5145"}/api/cameras`, { cache: "no-store" });
    if (r.ok) setCameras((await r.json()) as Cam[]);
  }, []);
  useEffect(() => { setMounted(true); setUser(getSessionUser()); void load(); }, [load]);
  useEffect(() => { const id = setInterval(() => setUser(getSessionUser()), 1000); return () => clearInterval(id); }, []);

  if (!mounted) return <p className="status-source">Loading admin panel…</p>;
  if (!user || user.role !== "Admin") return <p>Admin sign-in required — log in above with <code>admin@pathlens.local / Admin@123</code> to register cameras.</p>;

  async function onRegister(e: React.FormEvent) {
    e.preventDefault();
    setMsg(null);
    const res = await authFetch("/api/cameras", {
      method: "POST",
      body: JSON.stringify({
        provider, externalId, label, city, latitude: Number(lat), longitude: Number(lon), feedImageUrl: feedUrl, cadenceSeconds: Number(cadence), permissionNote: perm,
      }),
    });
    if (res.ok) { setMsg("Camera registered successfully."); await load(); }
    else setMsg(`Failed (${res.status}): ${(await res.text()).slice(0, 400)}`);
  }
  async function toggle(c: Cam) {
    const r = await authFetch(`/api/cameras/${c.id}`, { method: "PATCH", body: JSON.stringify({ enabled: !c.enabled }) });
    setMsg(r.ok ? (c.enabled ? "Disabled." : "Enabled.") : `Update failed (${r.status})`);
    if (r.ok) await load();
  }
  async function poll(id: string) {
    const r = await authFetch(`/api/cameras/${id}/poll`, { method: "POST" });
    setMsg(`Poll ${r.status}: ${(await r.text()).slice(0, 500)}`);
    await load();
  }

  return (
    <div className="admin-panel">
      {msg && <p role="status" className="status-source">{msg}</p>}
      <form onSubmit={onRegister} className="admin-form">
        <label>Provider <select value={provider} onChange={(e) => setProvider(e.target.value)}><option>Datumfeed</option><option>FiveOneOneSfBay</option><option>SfMta</option><option>Caltrans</option><option>GenericPublicFeed</option><option>NycDot</option></select></label>
        <label>ExternalId <input value={externalId} onChange={(e) => setExternalId(e.target.value)} required /></label>
        <label>Label <input value={label} onChange={(e) => setLabel(e.target.value)} required /></label>
        <label>City <input value={city} onChange={(e) => setCity(e.target.value)} required /></label>
        <label>Latitude <input value={lat} onChange={(e) => setLat(e.target.value)} required /></label>
        <label>Longitude <input value={lon} onChange={(e) => setLon(e.target.value)} required /></label>
        <label>FeedImageUrl <input value={feedUrl} onChange={(e) => setFeedUrl(e.target.value)} required /></label>
        <label>Cadence <input value={cadence} onChange={(e) => setCadence(e.target.value)} required /></label>
        <label>PermissionNote <input value={perm} onChange={(e) => setPerm(e.target.value)} required /></label>
        <button type="submit">Register camera</button>
      </form>
      <h3>Cameras ({cameras.length})</h3>
      {cameras.length === 0 ? (
        <p className="empty-state">No cameras registered yet. Use the form above to add your first SF / Datumfeed camera.</p>
      ) : (
        <div className="table-wrap">
          <table className="modern-table">
            <thead>
              <tr>
                <th>Camera</th>
                <th>Feed</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {cameras.map((c) => (
                <tr key={c.id}>
                  <td>
                    <strong>{c.label}</strong>
                    <span className="table-meta">{c.provider} · {c.city}</span>
                  </td>
                  <td className="table-mono">{c.feedImageUrl}</td>
                  <td>
                    <span className={`pill ${c.enabled ? "pill--on" : "pill--off"}`}>{c.enabled ? "Active" : "Inactive"}</span>
                    {c.lastError && <span className="table-meta table-meta--error">Error: {c.lastError}</span>}
                  </td>
                  <td className="table-actions">
                    <button onClick={() => toggle(c)} className="btn btn--ghost btn--sm">{c.enabled ? "Disable" : "Enable"}</button>
                    <button onClick={() => poll(c.id)} className="btn btn--primary btn--sm">Poll now</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
