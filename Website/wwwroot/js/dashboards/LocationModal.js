function OpenLocationModal() {
    const modal = document.getElementById('locationModal');
    if (!modal) return;

    modal.classList.add('show');
    ResolveCurrentLocation();
}
function CloseLocationModal() {
    ResetLocationModalInputs();

    const modal = document.getElementById('locationModal');
    if (!modal) return;

    modal.classList.remove('show');
    modal.addEventListener('transitionend', function handler() {
        modal.removeEventListener('transitionend', handler);
        modal.classList.remove('hide');
    });
}
function ResetLocationModalInputs() {
    const type = document.getElementById('locationAddressTypeId');
    const preview = document.getElementById('locationPreview');
    const payload = document.getElementById('locationPayload');

    if (type) type.selectedIndex = 0;
    if (preview) preview.textContent = '(not loaded yet)';
    if (payload) payload.value = '';

    const errEl = document.getElementById('locationTypeError');
    if (errEl) errEl.textContent = '';
}
function ResolveCurrentLocation() {
    const preview = document.getElementById('locationPreview');
    if (!('geolocation' in navigator)) {
        if (preview) preview.textContent = 'Location not supported.';
        return;
    }

    const tryGeolocation = (highAccuracy, retries = 3) => {
        navigator.geolocation.getCurrentPosition(
            (pos) => {
                const { latitude: lat, longitude: lon, accuracy } = pos.coords;

                if (highAccuracy && accuracy > 15 && retries > 0) {
                    if (preview) preview.textContent = `Accuracy low (${Math.round(accuracy)} m), retrying…`;
                    setTimeout(() => tryGeolocation(true, retries - 1), 1000);
                    return;
                }

                // Success, call Mapbox
                GetAddressFromCoords(lat, lon);
            },
            (err) => {
                if (highAccuracy) {
                    // fallback to low-accuracy
                    if (preview) preview.textContent = 'High-accuracy failed, trying low-accuracy…';
                    tryGeolocation(false, 1);
                } else {
                    if (preview) preview.textContent = 'Unable to get location. Please enter manually.';
                }
            },
            { enableHighAccuracy: highAccuracy, timeout: highAccuracy ? 10000 : 5000, maximumAge: 0 }
        );
    };

    tryGeolocation(true, 3);
}
function GetAddressFromCoords(lat, lon) {
    const preview = document.getElementById('locationPreview');
    const payload = document.getElementById('locationPayload');
    const token = document.querySelector('meta[name="Website-mapbox-token"]')?.content?.trim();

    if (!token) {
        if (preview) preview.textContent = 'Mapbox token missing.';
        return;
    }

    if (!Number.isFinite(lat) || !Number.isFinite(lon)) {
        if (preview) preview.textContent = 'Invalid coordinates.';
        return;
    }

    // Show interim status
    if (preview) preview.textContent = 'Resolving address…';

    const url = `https://api.mapbox.com/geocoding/v5/mapbox.places/${lon},${lat}.json?types=address&limit=1&country=nz&access_token=${encodeURIComponent(token)}`;

    fetch(url)
        .then(res => res.json())
        .then(data => {
            const feature = data?.features?.[0] || null;
            if (!feature) throw new Error('No address found for this location.');

            const label = (feature.place_name || feature.text || '').trim();

            if (preview) preview.textContent = label;
            if (payload) payload.value = JSON.stringify({
                latitude: lat,
                longitude: lon,
                feature: feature
            });
        })
        .catch(err => {
            if (preview) preview.textContent = err?.message || 'Unable to resolve address.';
        });
}
function ValidateLocationModal() {
    const payload = document.getElementById('locationPayload');
    const type = document.getElementById('locationAddressTypeId');
    const btnSave = document.getElementById('locationSave');

    if (!btnSave) return;

    const hasPayload = payload && payload.value;
    const hasType = type && parseInt(type.value, 10) > 0;

    btnSave.disabled = !(hasPayload && hasType);
}
function SaveLocation() {
    const preview = document.getElementById('locationPreview');
    const typeSelect = document.getElementById('locationAddressTypeId');
    const payloadEl = document.getElementById('locationPayload');

    if (!typeSelect?.value) { alert('Please select an address type.'); return; }
    if (!preview || preview.textContent === '(not loaded yet)') return;
    if (!payloadEl?.value) { alert('No location to save.'); return; }

    const addressData = JSON.parse(payloadEl.value);
    const typeID = parseInt(typeSelect.value, 10);

    toggleGlobalSpinner(true);

    fetch('/User/Address/EndPoints/AddFromLocation', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getCsrf()
        },
        body: JSON.stringify({ addressTypeID: typeID, JSonpayload: JSON.stringify(addressData) })
    })
        .then(resp => resp.json())
        .then(data => {
            if (!data?.ok && !data?.success) throw new Error(data?.message || 'Save failed.');

            const toggle = document.getElementById('addressToggle');
            const label = (data.label || preview.textContent || '').trim();
            if (toggle && label) {
                toggle.innerHTML = `<i class="fa-solid fa-home"></i>${label}&nbsp;<i class="fa-solid fa-chevron-down dropdown-icon"></i>`;
            }

            if (data.addressId) {
                fetch('/User/Address/EndPoints/BuildCard', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getCsrf()
                    },
                    body: JSON.stringify({ AddressId: data.addressId, SetDefault: true })
                })
                    .then(resp => {
                        return resp.json();
                    })
                    .then(cardData => {
                        if (cardData?.ok && cardData?.newcard) {
                            const addressList = document.getElementById('addressmodalbody');
                            if (addressList) addressList.insertAdjacentHTML('afterbegin', cardData.newcard);
                        } else {
                        }
                    })
                    .catch(err => {
                        console.error('BuildCard failed', err);
                    })
                    .finally(() => {
                        CloseLocationModal(); // always close modal after card attempt
                    });
            } else {
                CloseLocationModal();
            }
        })
        .catch(err => {
            if (preview) preview.textContent = err?.message || 'Save failed.';
            console.error(err);
        })
        .finally(() => {
            toggleGlobalSpinner(false);
        });
}

function getCsrf() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
}