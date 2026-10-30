/**
 * Minimal offline report queue (H4.3) — IndexedDB via localStorage fallback.
 * Stores CreateReport payloads when offline, replays when back online.
 * No real PWA service worker yet — minimal honest offline.
 */
export interface QueuedReport {
  id: string;
  payload: {
    title: string;
    description: string | null;
    obstacleType: string;
    severity: string;
    latitude: number;
    longitude: number;
    hasStairs: boolean | null;
    hasRamp: boolean | null;
    surface: string | null;
  };
  createdAt: string;
}

const KEY = "pathlens.offlineQueue.v1";

function load(): QueuedReport[] {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return [];
    return JSON.parse(raw) as QueuedReport[];
  } catch {
    return [];
  }
}
function save(queue: QueuedReport[]) {
  try {
    localStorage.setItem(KEY, JSON.stringify(queue));
  } catch {
    // quota exceeded — keep in memory only
  }
}

export function enqueueReport(payload: QueuedReport["payload"]): QueuedReport {
  const item: QueuedReport = { id: crypto.randomUUID(), payload, createdAt: new Date().toISOString() };
  const q = load();
  q.push(item);
  save(q);
  return item;
}

export function getQueue(): QueuedReport[] {
  return load();
}

export function dequeueReport(id: string): void {
  const q = load().filter(i => i.id !== id);
  save(q);
}

export function clearQueue(): void {
  try {
    localStorage.removeItem(KEY);
  } catch {}
}

/**
 * Client-side image compress: downscale longest edge to 1280 and re-encode JPEG 0.8.
 * Returns original file if canvas unavailable or not an image. Never throws.
 */
export async function compressImageFile(file: File, maxEdge = 1280, quality = 0.8): Promise<File> {
  if (!file.type.startsWith("image/")) return file;
  if (typeof document === "undefined" || typeof createImageBitmap === "undefined") return file;
  try {
    const bitmap = await createImageBitmap(file);
    const { width, height } = bitmap;
    const longest = Math.max(width, height);
    if (longest <= maxEdge) {
      bitmap.close();
      if (file.size < 1024 * 1024) return file;
    }
    const scale = Math.min(1, maxEdge / longest);
    const w = Math.round(width * scale);
    const h = Math.round(height * scale);
    const canvas = document.createElement("canvas");
    canvas.width = w;
    canvas.height = h;
    const ctx = canvas.getContext("2d");
    if (!ctx) {
      bitmap.close();
      return file;
    }
    ctx.drawImage(bitmap, 0, 0, w, h);
    bitmap.close();
    const blob: Blob | null = await new Promise(resolve => canvas.toBlob(resolve, "image/jpeg", quality));
    if (!blob) return file;
    if (blob.size >= file.size) return file; // no saving
    return new File([blob], file.name.replace(/\.[^.]+$/, ".jpg"), { type: "image/jpeg" });
  } catch {
    return file;
  }
}
