// Very basic driver PWA service worker

const DRIVER_SW_VERSION = "v1";

self.addEventListener("install", event => {
    // You can pre-cache core shell assets here later
    event.waitUntil(self.skipWaiting());
});

self.addEventListener("activate", event => {
    event.waitUntil(self.clients.claim());
});

self.addEventListener("fetch", event => {
    // For now, do not interfere – just passthrough
    return;
});