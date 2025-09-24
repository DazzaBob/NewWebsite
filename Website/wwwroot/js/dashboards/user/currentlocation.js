// -------------------------------
// CURRENT LOCATION HANDLER
// -------------------------------
Website.CurrentLocation = (function () {
    const btnId = 'useCurrentLocation';

    // local fallback for fetchJson (keeps original behavior)
    async function fetchJson(url, options = {}) {
        if (window.fetchJson && typeof window.fetchJson === 'function') {
            return window.fetchJson(url, options);
        }
        const res = await fetch(url, options);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return await res.json();
    }

    async function fetchMapboxAddress(lat, lon) {
        const token = document.querySelector('meta[name="Website-mapbox-token"]')?.content?.trim();
        if (!token) throw new Error('Mapbox token missing');

        const url = `https://api.mapbox.com/geocoding/v5/mapbox.places/${lon},${lat}.json?types=address&limit=1&country=nz&access_token=${encodeURIComponent(token)}`;
        const res = await fetch(url);
        if (!res.ok) throw new Error('Mapbox lookup failed');
        const data = await res.json();
        const feature = data?.features?.[0];
        if (!feature) throw new Error('No address found');
        return { feature, label: (feature.place_name || feature.text || '').trim() };
    }

    async function handleClick(btn) {
        if (!btn) return;
        btn.disabled = true;
        const oldText = btn.textContent;
        btn.textContent = 'Locating…';
        Website.UI.setButtonLoading(btn, true);

        try {
            if (!('geolocation' in navigator)) throw new Error('Geolocation not supported');
            if (location.protocol !== 'https:' && location.hostname !== 'localhost') throw new Error('Geolocation requires HTTPS');

            // open modal if present (replicates original behavior)
            if (Website.LocationModal?.open) Website.LocationModal.open();

            const pos = await new Promise((res, rej) =>
                navigator.geolocation.getCurrentPosition(res, rej, { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 })
            );

            const { latitude: lat, longitude: lon, accuracy } = pos.coords;

            const { feature, label } = await fetchMapboxAddress(lat, lon);
            const mapbox = { feature, label };

            // POST to server
            try {
                // geolocation, Mapbox, server POST
            const respData = await fetchJson('/User/Address/AddFromLocation', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ lat, lon, accuracy, mapbox })
            });

                const label = (respData?.label || 'Select Address').trim();

                // Update the toggle regardless of modal state
            const toggle = document.getElementById('addressToggle');
                if (toggle) {
                    toggle.innerHTML = `<i class="fa-solid fa-home"></i> ${label}&nbsp;<i class="fa-solid fa-chevron-down dropdown-icon"></i>`;
                }

                // Only close if the modal exists and is open
            if (Website.LocationModal && typeof Website.LocationModal.close === 'function') {
                Website.LocationModal.close();
            }
        } catch (err) {
            console.log('geo/mapbox/server error:', err?.message || err);
            }
        } catch (err) {
            console.log('geo/mapbox/server error:', err?.message || err);
        }
        finally {
            btn.textContent = oldText;
            btn.disabled = false;
            Website.UI.setButtonLoading(btn, false);
            Website.UI.forceStopAllLoading();
        }
    }

    function init() {
        const btn = document.getElementById(btnId);
        if (!btn) return;
        btn.addEventListener('click', e => { e.preventDefault(); handleClick(btn); }, { passive: true });
    }

    return { init };
})();