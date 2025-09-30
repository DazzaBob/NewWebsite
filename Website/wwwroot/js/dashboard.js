    (function () {
    const Website = (window.Website = window.Website || {});

    // -------------------------------
    // UTILITY FUNCTIONS (UPDATED)
    // -------------------------------
    const $ = (sel, root = document) => root.querySelector(sel);
    const $$ = (sel, root = document) => Array.from(root.querySelectorAll(sel));
    const on = (el, ev, fn, opts) => el && el.addEventListener(ev, fn, opts);
    const qs = new URLSearchParams(location.search);
    const getCsrf = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    const show = (el, v = true) => { if (el) el.style.display = v ? 'block' : 'none'; };
    const hide = (el) => { if (el) el.style.display = 'none'; };
    const disable = (el, v = true) => { if (el) el.disabled = v; };
    const FOCUS_SEL = 'button,[href],input,select,textarea,[tabindex]:not([tabindex="-1"])';

    // -------------------------------
    // GLOBAL SPINNER / BUTTON UI PATCH (UPDATED)
    // -------------------------------
    (function patchUI() {
        Website.__loadingTargets = Website.__loadingTargets || new WeakSet();
        Website.__loadingLastKnown = Website.__loadingLastKnown || new WeakMap();

        Website.UI = Website.UI || {};
        const _origSetBtnLoading = Website.UI.setButtonLoading;

        Website.UI.setButtonLoading = function (btn, on, txt) {
            if (!btn) return;

            if (on) {
                Website.__loadingTargets.add(btn);
                Website.__loadingLastKnown.set(btn, { txt: txt || btn.textContent || '' });

                btn.classList.add('is-loading');
                btn.setAttribute('aria-busy', 'true');
                btn.disabled = true;

                // Label handling
                let label = btn.querySelector('.btn-label');
                if (!label) {
                    label = document.createElement('span');
                    label.className = 'btn-label';
                    label.textContent = (txt || btn.textContent || '').trim() || 'Working…';
                    btn.textContent = '';
                    btn.appendChild(label);
                } else {
                    label.dataset._old = label.dataset._old || label.textContent;
                    if (txt) label.textContent = txt;
                }

                // Spinner handling
                let spin = btn.querySelector('.spinner');
                if (!spin) {
                    spin = document.createElement('span');
                    spin.className = 'spinner spinner--sm';
                    btn.appendChild(spin);
                }
                spin.hidden = false;
                spin.style.display = '';
                spin.style.animation = '';
            } else {
                // Turn off
                btn.classList.remove('is-loading');
                btn.removeAttribute('aria-busy');
                btn.disabled = false;

                const spin = btn.querySelector('.spinner');
                if (spin) { spin.hidden = true; spin.style.display = 'none'; spin.style.animation = 'none'; }

                const label = btn.querySelector('.btn-label');
                if (label && label.dataset._old) {
                    label.textContent = label.dataset._old;
                    delete label.dataset._old;
                }

                try { Website.__loadingTargets.delete(btn); } catch { /* ignore */ }
            }
        };

        Website.UI.forceStopAllLoading = function () {
            // Gather all known buttons + generic loading nodes
            const candidates = [
                document.getElementById('useCurrentLocation'),
                document.getElementById('addressToggle'),
                document.getElementById('locationSave'),
                document.getElementById('addressSave'),
                ...$$('.is-loading,.loading,.btn-loading,[aria-busy="true"]')
            ].filter(Boolean);

            const seen = new Set();
            candidates.forEach(el => {
                if (!el || seen.has(el)) return;
                seen.add(el);

                el.classList.remove('is-loading', 'loading', 'btn-loading', 'busy', 'is-busy');
                el.removeAttribute('aria-busy');
                el.disabled = false;

                const label = el.querySelector('.btn-label');
                if (label && label.dataset._old) { label.textContent = label.dataset._old; delete label.dataset._old; }

                el.querySelectorAll('.spinner,.ld-spinner,.loader,.loading-spinner').forEach(sp => {
                    sp.hidden = true;
                    sp.style.display = 'none';
                    sp.style.animation = 'none';
                });
            });

            // Remove any leftover global spinner nodes
            $$('body .spinner, body .ld-spinner, body .loader, body .loading-spinner').forEach(sp => {
                sp.hidden = true;
                sp.style.display = 'none';
                sp.style.animation = 'none';
                if (sp.parentElement) sp.parentElement.classList.remove('is-loading', 'loading', 'btn-loading');
            });
        };
    })();
    // -------------------------------
    // FOCUS TRAP UTILITY (UPDATED)
    // -------------------------------
    function trapFocus(container) {
        if (!container) return () => { };

        const FOCUS_SEL = 'button,[href],input,select,textarea,[tabindex]:not([tabindex="-1"])';

        // Return all currently visible & enabled focusable elements
        function focusables() {
            return $$(FOCUS_SEL, container).filter(el => !el.disabled && el.offsetParent !== null);
        }

        function handler(e) {
            if (e.key !== 'Tab') return;

            const f = focusables();
            if (!f.length) {
                e.preventDefault();
                return;
            }

            const first = f[0];
            const last = f[f.length - 1];

            if (e.shiftKey) {
                if (document.activeElement === first) {
                    e.preventDefault();
                    last.focus();
                }
            } else {
                if (document.activeElement === last) {
                    e.preventDefault();
                    first.focus();
                }
            }
        }

        document.addEventListener('keydown', handler, true);

        // Cleanup function
        return () => document.removeEventListener('keydown', handler, true);
    }

    // -------------------------------
    // BOOT / DOM READY
    // -------------------------------
    document.addEventListener('DOMContentLoaded', () => {
        Website.CurrentLocation.init();
        if (Website.LocationModal && typeof Website.LocationModal.bind === 'function') {
            Website.LocationModal.bind('#useCurrentLocation'); // capture-phase bind
        }
        // just in case: blast any pre-existing spinners from SSR/hydration glitches
        Website.UI.forceStopAllLoading();
    });

    // safety: also kill on tab visibility/focus changes to avoid stuck UI
    window.addEventListener('focus', () => Website.UI.forceStopAllLoading());
    document.addEventListener('visibilitychange', () => { if (!document.hidden) Website.UI.forceStopAllLoading(); });
})();

// === Website.LocationModal (opens on Current location; sends SINGLE FEATURE) ===
(function () {
    const Website = (window.Website = window.Website || {});
    if (Website.LocationModal) return;

    const $ = (sel, root = document) => root.querySelector(sel);
    const $$ = (sel, root = document) => Array.from(root.querySelectorAll(sel));
    const show = (el, v = true) => { if (el) el.style.display = v ? 'block' : 'none'; };
    const disable = (el, v = true) => { if (el) el.disabled = v; };
    const FOCUS_SEL = 'button,[href],input,select,textarea,[tabindex]:not([tabindex="-1"])';

    let modal, dialog, btnClose, btnCancel, btnSave, form, selType, errEl, previewEl, untrap, lastFocused, isOpen = false;

    // store a SINGLE FEATURE (not a FeatureCollection)
    let state = { mapboxFeature: null, label: "", typeId: 0 };

    function trapFocus(container) {
        function focusables() { return $$(FOCUS_SEL, container).filter(el => !el.hasAttribute('disabled') && el.offsetParent !== null); }
        function handler(e) {
            if (e.key !== 'Tab') return;
            const f = focusables(); if (!f.length) { e.preventDefault(); return; }
            const first = f[0], last = f[f.length - 1];
            if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
            else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
        }
        document.addEventListener('keydown', handler, true);
        return () => document.removeEventListener('keydown', handler, true);
    }
    function onEsc(e) { if (e.key === 'Escape') { e.preventDefault(); close(); } }

    // --- spinner helpers --------------------------------------------
    function setBtnLoading(btn, on, txt) { Website.UI.setButtonLoading(btn, on, txt); }
    function resetSaveButton() {
        if (!btnSave) return;
        Website.UI.setButtonLoading(btnSave, false);
        btnSave.disabled = false;
        const label = btnSave.querySelector('.btn-label');
        if (label) { label.textContent = 'Save'; delete label.dataset?._old; }
        const spin = btnSave.querySelector('.spinner');
        if (spin) { spin.hidden = true; spin.style.display = 'none'; spin.style.animation = 'none'; }
    }

    function getToken() {
        return document.querySelector('meta[name="Website-mapbox-token"]')?.content?.trim() || '';
    }
    function getCsrf() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    function validate() {
        const typeOk = !!(selType && selType.value && +selType.value >= 1 && +selType.value <= 7);
        const jsonOk = !!state.mapboxFeature;
        const ok = typeOk && jsonOk;
        disable(btnSave, !ok);
        if (errEl) errEl.textContent = typeOk ? '' : 'Please select an address type.';
        return ok;
    }

    async function resolveFeatureFromCoords(lat, lon) {
        const token = getToken();
        if (!token) throw new Error('Mapbox token missing');
        if (!Number.isFinite(lat) || !Number.isFinite(lon)) throw new Error('Invalid coordinates');

        const url = `https://api.mapbox.com/geocoding/v5/mapbox.places/${lon},${lat}.json?types=address&limit=1&country=nz&access_token=${encodeURIComponent(token)}`;
        const res = await fetch(url, { method: 'GET' });
        const data = await res.json();
        const feature = data?.features?.[0] || null;
        if (!feature) throw new Error('No address found for this location.');
        const label = (feature.place_name || feature.text || '').trim();
        return { feature, label };
    }

    function open() {
        if (isOpen) return;

        modal = modal || $('#locationModal');
        if (!modal) return;
        dialog = dialog || modal.querySelector('.custom-modal-content');
        btnClose = btnClose || $('#locationClose');
        btnCancel = btnCancel || $('#locationCancel');
        btnSave = btnSave || $('#locationSave');
        form = form || $('#locationForm');
        selType = selType || $('#locationAddressTypeId');
        errEl = errEl || $('#locationTypeError');
        previewEl = previewEl || $('#locationPreview');

        // Force-stop any lingering spinners before we show
        Website.UI.forceStopAllLoading();

        isOpen = true;
        lastFocused = document.activeElement;
        show(modal, true);
        document.body.style.overflow = 'hidden';
        untrap = trapFocus(dialog);
        (dialog.querySelector('select,button,[tabindex]') || dialog).focus();
        document.addEventListener('keydown', onEsc, true);

        // Reset state & UI
        state.mapboxFeature = null;
        state.label = '';
        state.typeId = 0;
        if (selType) selType.value = '';
        resetSaveButton();
        disable(btnSave, true);
        if (previewEl) previewEl.textContent = 'Resolving address…';
        validate();

        // Bind events (once-per-open for close; persistent for select)
        btnClose && btnClose.addEventListener('click', (e) => { e.preventDefault(); close(); }, { once: true });
        btnCancel && btnCancel.addEventListener('click', (e) => { e.preventDefault(); close(); }, { once: true });
        if (selType && !selType.dataset._bound) {
            selType.addEventListener('change', () => { state.typeId = parseInt(selType.value || '0', 10) || 0; validate(); });
            selType.dataset._bound = '1';
        }
        form && form.addEventListener('submit', onSubmit, { once: true });

        // Geolocation (progress shown inside modal)
        if (!('geolocation' in navigator)) { if (previewEl) previewEl.textContent = 'Location not supported.'; return; }
        navigator.geolocation.getCurrentPosition(async (pos) => {
            try {
                const { latitude: lat, longitude: lon } = pos.coords || {};
                const { feature, label } = await resolveFeatureFromCoords(lat, lon);
                state.mapboxFeature = feature;
                state.label = label;
                if (previewEl) previewEl.textContent = label || 'Address not found';
                validate();
            } catch (e) {
                if (previewEl) previewEl.textContent = (e && e.message) ? e.message : 'Unable to resolve address.';
            }
        }, (err) => {
            if (previewEl) previewEl.textContent = err?.message || 'Unable to get location.';
        }, { enableHighAccuracy: true, timeout: 12000, maximumAge: 0 });
    }

    function close() {
        if (!isOpen || !modal) return;

        // Force-stop any lingering spinners when closing
        Website.UI.forceStopAllLoading();

        isOpen = false;
        show(modal, false);
        document.body.style.overflow = '';
        untrap && untrap();
        document.removeEventListener('keydown', onEsc, true);
        (lastFocused)?.focus();
        validate();
    }

    async function onSubmit(e) {
        e.preventDefault();
        if (!validate()) return;

        const addressTypeID = state.typeId;
        const JSonpayload = JSON.stringify(state.mapboxFeature || {});

        // start Save spinner explicitly
        setBtnLoading(btnSave, true, 'Saving…');

        try {
            let data;
            if (Website.UI?.fetchJson) {
                // do NOT pass btnSave; we control spinner ourselves
                data = await Website.UI.fetchJson('/User/Address/AddFromLocation', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getCsrf()
                    },
                    body: JSON.stringify({ addressTypeID, JSonpayload })
                });
            } else {
                const res = await fetch('/User/Address/AddFromLocation', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getCsrf()
                    },
                    body: JSON.stringify({ addressTypeID, JSonpayload })
                });
                data = await res.json().catch(() => ({}));
                if (!res.ok) throw new Error(data?.message || `HTTP ${res.status}`);
            }

            const ok = (data && (data.success === true || data.ok === true));
            if (!ok) throw new Error(data?.message || 'Save failed.');

            const label = (data.label || state.label || '').trim();
            const toggle = document.getElementById('addressToggle');
            if (label && toggle) {
                toggle.innerHTML = `<i class="fa-solid fa-home"></i> ${label}&nbsp;<i class="fa-solid fa-chevron-down dropdown-icon"></i>`;
            }

            // Stop spinners BEFORE closing to avoid any race
            Website.UI.forceStopAllLoading();
            close();
        } catch (err) {
            if (errEl) errEl.textContent = err?.message || 'Save failed.';
        } finally {
            // Always restore Save button & nuke any leftover spinners
            setBtnLoading(btnSave, false);
            resetSaveButton();
            Website.UI.forceStopAllLoading();
            validate();
        }
    }

    Website.LocationModal = {
        open,
        close,
        bind: (selector) => {
            const btn = document.querySelector(selector);
            if (!btn || btn.dataset._ulxBound) return;
            btn.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopImmediatePropagation(); // stop legacy handler early
                open();
            }, true); // capture-phase
            btn.dataset._ulxBound = '1';
        }
    };
})();
