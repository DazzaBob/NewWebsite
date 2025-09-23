window.Website = window.Website || {};

Website.UI = (() => {
    const $ = (sel, root = document) => root.querySelector(sel);

    function setButtonLoading(btn, loading, textIfLoading) {
        if (!btn) return;

        // find/create spinner inside the button
        let spin = btn.querySelector('.spinner');
        if (!spin) {
            spin = document.createElement('span');
            spin.className = 'spinner spinner--sm';
            spin.hidden = true;
            btn.appendChild(spin);
        }

        // find/create label span
        let label = btn.querySelector('.btn-label');
        if (!label) {
            const t = document.createTextNode(btn.textContent.trim());
            btn.textContent = '';
            label = document.createElement('span');
            label.className = 'btn-label';
            label.appendChild(t);
            btn.prepend(label);
        }

        if (loading) {
            btn.classList.add('is-loading');
            btn.disabled = true;
            spin.hidden = false;
            if (textIfLoading) {
                label.dataset._old = label.textContent;
                label.textContent = textIfLoading;
            }
        } else {
            btn.classList.remove('is-loading');
            btn.disabled = false;
            spin.hidden = true;
            if (label.dataset._old) {
                label.textContent = label.dataset._old;
                delete label.dataset._old;
            }
        }
    }

    function showOverlay(show = true, id = 'globalSpinner') {
        const el = document.getElementById(id);
        if (el) el.hidden = !show;
    }

    // fetch wrapper that toggles a button’s spinner
    async function fetchJson(url, options = {}, btn, loadingText = 'Saving…') {
        try {
            if (btn) setButtonLoading(btn, true, loadingText);
            const res = await fetch(url, options);
            const data = await res.json().catch(() => ({}));
            if (!res.ok) {
                const err = new Error(data?.message || res.statusText || `HTTP ${res.status}`);
                err.status = res.status;
                throw err;
            }
            return data;
        } finally {
            if (btn) setButtonLoading(btn, false);
        }
    }

    // Force-stop any lingering spinners globally
    function forceStopAllLoading() {
        document.querySelectorAll('.is-loading,.spinner').forEach(el => {
            el.classList.remove('is-loading');
            el.removeAttribute('aria-busy');
            el.disabled = false;
            const spin = el.querySelector('.spinner');
            if (spin) { spin.hidden = true; spin.style.display = 'none'; spin.style.animation = 'none'; }
            const label = el.querySelector('.btn-label');
            if (label && label.dataset?._old) {
                label.textContent = label.dataset._old;
                delete label.dataset._old;
            }
        });
        const overlay = document.getElementById('globalSpinner');
        if (overlay) overlay.hidden = true;
    }

    return { setButtonLoading, fetchJson, showOverlay, forceStopAllLoading };
})();