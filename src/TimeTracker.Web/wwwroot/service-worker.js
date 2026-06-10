// Minimal service worker. Chrome/Edge require an active SW with a fetch handler before
// they'll show the "Install" button next to the URL bar. This one is intentionally
// pass-through — it doesn't precache, doesn't intercept, just exists so the install
// criteria are met. Once the app is installed it behaves identically to the browser tab.
//
// IF you later want offline support: Blazor WASM ships a richer template at
// service-worker.published.js / service-worker.js that integrates with the static-web-
// assets manifest and precaches the WASM bundle. Adding it is a separate piece of work
// (involves an MSBuild target + cache versioning) and is NOT done here.
self.addEventListener('install', () => {
    // Take over the page as soon as the SW activates so the install button appears on
    // the first visit, not the second.
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil(self.clients.claim());
});

self.addEventListener('fetch', () => {
    // No-op handler: the install criteria require *some* fetch listener. Falling through
    // means the request hits the network normally.
});
