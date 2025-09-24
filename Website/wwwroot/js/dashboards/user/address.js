window.showaddaddresscard = function () {
    const card = document.getElementById('addaddresssection');
    const btnCancel = document.getElementById('addressCancel');
    const btnSave = document.getElementById('addressSave');
    const modal = document.getElementById('addressModal');
    if (card) card.style.display = 'block';
    btnCancel.style.display = "none";
    btnSave.style.display = "none";
    if (modal) modal.scrollTop = modal.scrollHeight;
    document.getElementById('newLabel')?.focus();
};

(function () {
    const token = '@Html.Raw(ViewData["MapboxPublicToken"])';
    const input = document.getElementById('NewAddressSearch');
    const list = document.getElementById('NewAutocompleteList');
    const hidden = document.getElementById('NewMapboxAddressJSON');
    let controller = null;

    function debounce(fn, delay = 300) {
        let timeout;
        return (...args) => {
            clearTimeout(timeout);
            timeout = setTimeout(() => fn(...args), delay);
    };
    }

    const fetchSuggestions = debounce(async () => {
        const q = input.value.trim();
        if (!q) return list.hidden = true;

        controller?.abort();
        controller = new AbortController();

        const url = `https://api.mapbox.com/geocoding/v5/mapbox.places/${encodeURIComponent(q)}.json`
            + `?autocomplete=true&country=nz&limit=5&access_token=${token}`;

        try {
            const res = await fetch(url, { signal: controller.signal });
            const { features } = await res.json();
            renderList(features || []);
        } catch (e) {
            if (e.name !== 'AbortError') console.error(e);
        }
    }, 200);

    function renderList(features) {
        list.innerHTML = '';
        if (!features.length) return list.hidden = true;
        features.forEach(feat => {
            const li = document.createElement('li');
            li.textContent = feat.place_name;
            li.addEventListener('mousedown', () => selectFeature(feat));
            list.appendChild(li);
            });
        list.hidden = false;
        }

    function selectFeature(feat) {
        input.value = feat.place_name;
        hidden.value = JSON.stringify(feat);
        list.hidden = true;
    }

    input.addEventListener('input', fetchSuggestions);
    document.addEventListener('click', e => {
        if (!input.contains(e.target) && !list.contains(e.target)) {
            list.hidden = true;
        }
    });
})();


