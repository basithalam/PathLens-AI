// H4.3 Minimal PWA service worker — offline queue fallback
// Currently only caches manifest; real offline sync would need workbox.
// Installed via navigator.serviceWorker.register("/sw.js") when available.
self.addEventListener("install", e => self.skipWaiting());
self.addEventListener("activate", e => self.clients.claim());
self.addEventListener("fetch", e => {
  // Pass-through — offline queue is handled via localStorage in offline-queue.ts
});