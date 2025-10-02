// ============================================================================
// Name: Dashboard-User-Address (JS) Utilities
// File: DUAddress.js
// Description: JavaScript functions for managing user addresses on the dashboard.
//              Includes adding, editing, deleting, and setting default addresses,
//              as well as modal and input controls.
// Author: Darrell Roberts
// Created: 2025-09-30
//
// Required DOM structure / element IDs:
//   - #addressmodal               : The main address modal
//   - #addressmodalbody           : Container for address cards
//   - .address-card               : Individual address card
//   - .address-label              : Label text for each address
//   - .address-line               : Optional secondary text for the address
//   - .address-radio              : Radio input for selecting default address
//   - .address-actions            : Container for normal card action buttons
//   - .address-actions-edit       : Container for edit-mode action buttons
//   - .address-edit-input         : Input field for editing address label
//   - .address-delete             : Delete button for the address card
//   - #addressToggle              : Element showing the current default address
//   - #addressCancel              : Cancel button for adding a new address
//   - #addressSave                : Save button for adding a new address
//   - #addressmodaladdaddresssectionaddresscard : Section containing new address card
//   - #addressmodaladdaddresscardnewLabel       : Input for new address label
//   - #NewMapboxAddressJSON       : Hidden input with Mapbox JSON for new address
//   - #NewAddressTypeID           : Select for new address type
//   - #NewAutocompleteList        : Autocomplete suggestion list for addresses
// ============================================================================
function closeAddressModal() {
    ResetNewAddressInputs();

    const modal = document.getElementById('addressmodal');
    if (!modal) return;
    modal.classList.remove('show');
    // Wait for CSS fade-out transition to complete
    modal.addEventListener('transitionend', function handler() {
        modal.removeEventListener('transitionend', handler);
        modal.classList.remove('hide');
    });
}
function ResetNewAddressInputs() {
    const label = document.getElementById('addressmodaladdaddresscardnewLabel');
    const search = document.getElementById('NewAddressSearch');
    const payload = document.getElementById('NewMapboxAddressJSON');
    const type = document.getElementById('NewAddressTypeID');
    const list = document.getElementById('NewAutocompleteList');

    if (label) label.value = '';
    if (search) search.value = '';
    if (payload) payload.value = '';
    if (type) type.selectedIndex = 0;
    if (list) list.hidden = true;
}
function UpdateDefaultAddress(radio) {
    if (!radio) return;

    const card = radio.closest('.address-card');
    if (!card) return;

    const selectedId = parseInt(radio.value, 10);
    if (!selectedId || isNaN(selectedId)) return;

    toggleGlobalSpinner(true); // Start spinner

    fetch('/User/Address/EndPoints/SetDefault', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ Id: selectedId })
    })
        .then(resp => resp.json())
        .then(data => {
            if (!data?.ok) throw new Error(data?.msg || 'Failed to save.');

            radio.checked = true;

            document.querySelectorAll('.address-card').forEach(c => {
                const r = c.querySelector('.address-radio');
                const delBtn = c.querySelector('.address-delete');
                if (!r || !delBtn) return;
                delBtn.style.display = r.checked ? 'none' : 'block';
            });

            const labelText = card.querySelector('.address-label')?.textContent?.trim() || '';
            const lineText = card.querySelector('.address-line')?.textContent?.trim() || '';
            const preview = labelText ? labelText : (data.label || lineText);

            const toggle = document.getElementById('addressToggle');
            if (toggle) {
                toggle.innerHTML = `<i class="fa-solid fa-home"></i>${preview}&nbsp;<i class="fa-solid fa-chevron-down dropdown-icon"></i>`;
            }
        })
        .catch(err => {
            console.error(err);
            alert(err.message || 'Error saving default address.');
        })
        .finally(() => {
            toggleGlobalSpinner(false);
        });
}
function DeleteAddressCard(button) {
    if (!button) return;

    const card = button.closest('.address-card');
    if (!card) return;

    const radio = card.querySelector('.address-radio');
    if (!radio) return;

    const addressId = parseInt(radio.value, 10);
    const label = card.querySelector('.address-label')?.textContent?.trim() || 'this address';

    if (!window.confirm(`Are you sure you want to delete "${label}"?`)) {
        card.scrollIntoView({ behavior: 'smooth', block: 'center' });
        radio.focus();
        return;
    }

    toggleGlobalSpinner(true);

    fetch('/User/Address/EndPoints/DeleteSelected', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ Id: addressId })
    })
        .then(resp => resp.json())
        .then(data => {
            if (!data?.ok) throw new Error(data?.msg || 'Failed to delete address.');
            card.remove();
        })
        .catch(err => {
            console.error(err);
            alert(err.message || 'Error deleting address.');
        })
        .finally(() => {
            toggleGlobalSpinner(false);
        });
}
function startEditAddress(button) {
    if (!button) return;

    const card = button.closest('.address-card');
    if (!card) return;

    // Hide label and radio
    const label = card.querySelector('.address-label');
    if (label) label.style.display = 'none';

    const radio = card.querySelector('.address-radio');
    if (radio) radio.style.display = 'none';

    // Show input
    const input = card.querySelector('.address-edit-input');
    if (input) {
        input.style.display = 'inline-block';
        input.style.width = '90%';
        input.focus();
        const valLength = input.value.length;
        input.setSelectionRange(valLength, valLength);
    }

    // Hide normal actions
    const normalActions = card.querySelector('.address-actions');
    if (normalActions) normalActions.style.display = 'none';

    // Show edit actions
    const editActions = card.querySelector('.address-actions-edit');
    if (editActions) {
        Object.assign(editActions.style, {
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'flex-start',
            marginLeft: '1rem'
        });

        const saveBtn = editActions.querySelector('.address-save');
        if (saveBtn) saveBtn.style.display = 'inline-block';

        const cancelBtn = editActions.querySelector('.address-cancel');
        if (cancelBtn) cancelBtn.style.display = 'inline-block';
    }
}
function cancelEditAddress(button) {
    if (!button) return;

    const card = button.closest('.address-card');
    if (!card) return;

    // Restore normal actions
    const normalActions = card.querySelector('.address-actions');
    if (normalActions) normalActions.style.display = 'flex';

    // Hide edit actions
    const editActions = card.querySelector('.address-actions-edit');
    if (editActions) editActions.style.display = 'none';

    // Restore label and radio visibility
    const label = card.querySelector('.address-label');
    if (label) label.style.display = 'inline-block';

    const radio = card.querySelector('.address-radio');
    if (radio) radio.style.display = 'inline-block';

    // Hide the edit input
    const input = card.querySelector('.address-edit-input');
    if (input) input.style.display = 'none';

    // Focus the card itself
    card.tabIndex = -1; // make focusable if not already
    card.focus();
}
function saveEditedAddress(card, newLabel) {
    if (!card || !newLabel?.trim()) return;

    const radio = card.querySelector('.address-radio');
    const addressId = parseInt(radio?.value, 10);
    if (!addressId) return;

    toggleGlobalSpinner(true);

    fetch('/User/Address/EndPoints/EditLabel', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ Id: addressId, Label: newLabel.trim() })
    })
        .then(resp => resp.json())
        .then(data => {
            if (!data?.ok) throw new Error(data?.msg || 'Failed to update label.');

            const label = card.querySelector('.address-label');
            if (label) label.textContent = newLabel.trim();

            if (radio?.checked) {
                const toggle = document.getElementById('addressToggle');
                if (toggle) {
                    toggle.innerHTML = `<i class="fa-solid fa-home"></i> ${newLabel.trim()}&nbsp;<i class="fa-solid fa-chevron-down dropdown-icon"></i>`;
                }
            }

            // Restore the card from edit mode
            cancelEditAddress(card);
        })
        .catch(err => {
            console.error(err);
            alert(err.message || 'Error updating address label.');
        })
        .finally(() => {
            toggleGlobalSpinner(false);
        });
}
function CloseNewAddressCard() {
    ResetNewAddressInputs();

    const btnCancel = document.getElementById('addressCancel');
    const btnSave = document.getElementById('addressSave');
    const section = document.getElementById('addressmodaladdaddresssectionaddresscard');

    if (btnCancel) btnCancel.style.display = 'block';
    if (btnSave) btnSave.style.display = 'block';
    if (section) section.style.display = 'none';
}
function addNewAddressCard(data) {
    if (!data || !data.newcard) return;

    const container = document.getElementById('addressmodalbody');
    if (!container) return;

    // Insert new card at the top
    container.insertAdjacentHTML('afterbegin', data.newcard);

    // Grab the newly inserted card
    const newCard = container.querySelector('.address-card');
    if (newCard) {
        newCard.style.display = 'flex';

        // Animate opacity from 0 to 1
        newCard.style.opacity = 0;
        newCard.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
        newCard.style.transform = 'translateY(-10px)';
        setTimeout(() => {
            newCard.style.opacity = 1;
            newCard.style.transform = 'translateY(0)';
        }, 10);

        // Scroll container to top
        container.scrollTop = 0;
    }
}

window.showaddaddresscard = function () {
    const card = document.getElementById('addressmodaladdaddresssectionaddresscard');
    const btnCancel = document.getElementById('addressCancel');
    const btnSave = document.getElementById('addressSave');
    const modal = document.getElementById('addressModal');

    if (card) card.style.display = 'block';
    if (btnCancel) btnCancel.style.display = 'none';
    if (btnSave) btnSave.style.display = 'none';
    if (modal) modal.scrollTop = modal.scrollHeight;

    document.getElementById('addressmodaladdaddresscardnewLabel')?.focus();
};

window.saveNewAddressScript = function (card) {
    if (!card) {
        alert('No card passed');
        return;
    }

    const label = card.querySelector('#addressmodaladdaddresscardnewLabel')?.value.trim();
    const searchJSON = card.querySelector('#NewMapboxAddressJSON')?.value;
    const typeID = card.querySelector('#addressmodalNewAddressTypeID')?.value;

    if (!label || !searchJSON || !typeID) {
        alert('Validation failed: Missing Label, Address or Address Type');
        return;
    }

    toggleGlobalSpinner(true);

    fetch('/User/Address/EndPoints/AddAddress', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ Label: label, AddressJSON: searchJSON, TypeID: parseInt(typeID) })
    })
        .then(resp => resp.json())
        .then(data => {
            if (!data?.ok) throw new Error(data?.msg || 'Failed to save new address');

            addNewAddressCard(data);
            CloseNewAddressCard();
        })
        .catch(err => {
            alert('ERROR: ' + (err.message || 'Unknown error'));
        })
        .finally(() => {
            toggleGlobalSpinner(false);
        });
};