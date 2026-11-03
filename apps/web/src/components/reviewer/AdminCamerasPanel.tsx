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

const PROVIDERS = [
  "Datumfeed",
  "FiveOneOneSfBay",
  "SfMta",
  "Caltrans",
  "GenericPublicFeed",
  "NycDot",
];

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
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editFeedUrl, setEditFeedUrl] = useState("");
  const [editPerm, setEditPerm] = useState("");

  const load = useCallback(async () => {
    const r = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5145"}/api/cameras`, { cache: "no-store" });
    if (r.ok) setCameras((await r.json()) as Cam[]);
  }, []);
  useEffect(() => { setMounted(true); setUser(getSessionUser()); void load(); }, [load]);

  if (!mounted) {
    return (
      <div className="skeleton" aria-live="polite">
        <div className="sk-line sk-line--title" />
        <div className="sk-card" />
        <div className="sk-card" />
      </div>
    );
  }

  if (!user || user.role !== "Admin") {
    return (
      <p className="empty-state">
        <strong>Admin session required.</strong>
        <span>Sign in above with the Admin demo account to register and poll camera feeds.</span>
      </p>
    );
  }

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
  function startEdit(c: Cam) {
    setEditingId(c.id);
    setEditFeedUrl(c.feedImageUrl);
    setEditPerm("");
  }
  async function saveEdit(c: Cam) {
    const payload: Record<string, string> = {};
    if (editFeedUrl.trim() && editFeedUrl !== c.feedImageUrl) payload.feedImageUrl = editFeedUrl.trim();
    if (editPerm.trim()) payload.permissionNote = editPerm.trim();
    if (Object.keys(payload).length === 0) { setEditingId(null); return; }
    const r = await authFetch(`/api/cameras/${c.id}`, { method: "PATCH", body: JSON.stringify(payload) });
    if (r.ok) { setMsg("Camera updated via UI."); setEditingId(null); await load(); }
    else setMsg(`Update failed (${r.status}): ${(await r.text()).slice(0, 400)}`);
  }
  async function enableAll() {
    setMsg("Enabling all cameras…");
    for (const c of cameras) if (!c.enabled && c.hasPermissionNote) await authFetch(`/api/cameras/${c.id}`, { method: "PATCH", body: JSON.stringify({ enabled: true }) });
    await load(); setMsg(`Enabled all eligible cameras (${cameras.length}).`);
  }
  async function pollAll() {
    setMsg("Polling all active cameras…");
    for (const c of cameras) if (c.enabled) await authFetch(`/api/cameras/${c.id}/poll`, { method: "POST" });
    await load(); setMsg("Polled all active cameras.");
  }
  async function importFromDatumfeed(city: string, limit: number) {
    setMsg(`Importing ${limit} active cameras for ${city} from Datumfeed…`);
    const r = await authFetch(`/api/cameras/import?city=${encodeURIComponent(city)}&limit=${limit}`, { method: "POST" });
    if (r.ok) { const j = await r.json(); setMsg(`Imported ${j.imported} cameras for ${city} — ${j.skipped} skipped (duplicates).`); await load(); }
    else setMsg(`Import failed (${r.status}): ${(await r.text()).slice(0, 500)}`);
  }

  const activeCount = cameras.filter((c) => c.enabled).length;

  return (
    <div className="admin-panel stack">
      {msg && (
        <p role="status" className="notice">
          {msg}
        </p>
      )}

      <section className="panel" aria-labelledby="camera-register-heading">
        <div className="panel__head">
          <h2 id="camera-register-heading">Register a camera feed</h2>
          <span className="panel__count">{cameras.length} registered</span>
        </div>
        <div className="panel__body">
          <form onSubmit={onRegister} className="admin-form">
            <label>
              Provider
              <select value={provider} onChange={(e) => setProvider(e.target.value)}>
                {PROVIDERS.map((p) => (
                  <option key={p} value={p}>
                    {p}
                  </option>
                ))}
              </select>
            </label>
            <label>
              External ID
              <input value={externalId} onChange={(e) => setExternalId(e.target.value)} required />
            </label>
            <label>
              Label
              <input value={label} onChange={(e) => setLabel(e.target.value)} required />
            </label>
            <label>
              City
              <input value={city} onChange={(e) => setCity(e.target.value)} required />
            </label>
            <label>
              Latitude
              <input value={lat} onChange={(e) => setLat(e.target.value)} inputMode="decimal" required />
            </label>
            <label>
              Longitude
              <input value={lon} onChange={(e) => setLon(e.target.value)} inputMode="decimal" required />
            </label>
            <label>
              Feed image URL
              <input value={feedUrl} onChange={(e) => setFeedUrl(e.target.value)} required />
            </label>
            <label>
              Cadence (seconds)
              <input value={cadence} onChange={(e) => setCadence(e.target.value)} inputMode="numeric" required />
            </label>
            <label>
              Permission note
              <input value={perm} onChange={(e) => setPerm(e.target.value)} required />
              <span className="field__hint">Required — a camera cannot be enabled without it.</span>
            </label>
            <button type="submit" className="btn btn--primary">
              Register camera
            </button>
          </form>

          <div className="queue-actions">
            <span className="queue-actions__label">Bulk actions</span>
            <button onClick={enableAll} className="btn btn--brand btn--sm">Enable all</button>
            <button onClick={pollAll} className="btn btn--ghost btn--sm">Poll all active</button>
            <button onClick={() => importFromDatumfeed("austin", 10)} className="btn btn--quiet btn--sm">Import 10 · Austin</button>
            <button onClick={() => importFromDatumfeed("sanfrancisco", 10)} className="btn btn--quiet btn--sm">Import 10 · San Francisco</button>
          </div>
        </div>
        <div className="panel__foot">
          Enable a camera, then use <em>Poll now</em> to fetch a frame immediately. It appears on the map as a
          square <code>C</code> marker — evidence, never a verdict.
        </div>
      </section>

      <section className="panel" aria-labelledby="camera-list-heading">
        <div className="panel__head">
          <h2 id="camera-list-heading">Camera registry</h2>
          <span className="panel__count">
            {activeCount} active / {cameras.length}
          </span>
        </div>
        <div className="panel__body">
          {cameras.length === 0 ? (
            <p className="empty-state">
              <strong>No cameras registered yet.</strong>
              <span>
                The pilot city is Sylhet, and Sylhet has no public footpath camera today — that gap is shown on the
                map, not hidden. Register a feed to see the camera path work; the available public feeds are San
                Francisco (Datumfeed / Caltrans / 511SF / SFMTA).
              </span>
            </p>
          ) : (
            <div className="table-wrap">
              <table className="modern-table">
                <thead>
                  <tr>
                    <th scope="col">Camera</th>
                    <th scope="col">Feed</th>
                    <th scope="col">Status</th>
                    <th scope="col">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {cameras.map((c) => (
                    <tr key={c.id}>
                      <td>
                        <strong>{c.label}</strong>
                        <span className="table-meta">
                          {c.provider} · {c.city}
                        </span>
                      </td>
                      <td className="table-mono">
                        {editingId === c.id ? (
                          <div className="stack-sm">
                            <input
                              value={editFeedUrl}
                              onChange={(e) => setEditFeedUrl(e.target.value)}
                              placeholder="https://datumfeed.com/.../image.jpg"
                            />
                            <input
                              value={editPerm}
                              onChange={(e) => setEditPerm(e.target.value)}
                              placeholder="Update permissionNote (optional)"
                            />
                          </div>
                        ) : (
                          <>
                            {c.feedImageUrl}
                            <span className="table-meta">
                              last success: {c.lastSuccessAt ? new Date(c.lastSuccessAt).toLocaleString() : "never"}
                            </span>
                          </>
                        )}
                      </td>
                      <td>
                        <span className={`pill ${c.enabled ? "pill--on" : "pill--off"}`}>
                          {c.enabled ? "Active" : "Inactive"}
                        </span>
                        {c.hasPermissionNote ? null : (
                          <span className="table-meta table-meta--error">No permission note — cannot enable</span>
                        )}
                        {c.lastError && <span className="table-meta table-meta--error">{c.lastError}</span>}
                      </td>
                      <td className="table-actions">
                        {editingId === c.id ? (
                          <>
                            <button onClick={() => saveEdit(c)} className="btn btn--primary btn--sm">Save</button>
                            <button onClick={() => setEditingId(null)} className="btn btn--ghost btn--sm">Cancel</button>
                          </>
                        ) : (
                          <>
                            <button onClick={() => startEdit(c)} className="btn btn--ghost btn--sm">Edit</button>
                            <button onClick={() => toggle(c)} className="btn btn--quiet btn--sm">{c.enabled ? "Disable" : "Enable"}</button>
                            <button onClick={() => poll(c.id)} className="btn btn--brand btn--sm">Poll now</button>
                          </>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </section>
    </div>
  );
}
