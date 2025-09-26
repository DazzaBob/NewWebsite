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
function cancelEditAddress(card) {
    const input = card.querySelector('.edit-address-input');
    const saveBtn = card.querySelector('.address-save');
    const cancelBtn = card.querySelector('.address-cancel');
    const label = card.querySelector('.address-label');
    const radio = card.querySelector('.address-radio');
    const editBtn = card.querySelector('.address-edit');
    const deleteBtn = card.querySelector('.address-delete');

    input?.remove();
    saveBtn?.remove();
    cancelBtn?.remove();

    if (label) label.style.display = 'inline-block';
    if (radio) radio.style.display = 'inline-block';
    if (editBtn) editBtn.style.display = 'inline-block';
    if (deleteBtn) deleteBtn.style.display = 'inline-block';
}

function saveEditedAddress(card, newLabel) {
    const radio = card.querySelector('.address-radio');
    const addressId = parseInt(radio?.value, 10);
    if (!addressId || !newLabel.trim()) return;

    toggleGlobalSpinner(true);

    fetch('/User/Address/EditLabel', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ Id: addressId, Label: newLabel.trim() })
    })
        .then(resp => resp.json())
        .then(data => {
            if (!data?.ok) throw new Error(data?.msg || 'Failed to update label.');

            // Update label text
            const label = card.querySelector('.address-label');
            if (label) label.textContent = newLabel.trim();

            // Update toggle preview if this is the selected address
            if (radio?.checked) {
                const toggle = document.getElementById('addressToggle');
                if (toggle) {
                    toggle.innerHTML = `<i class="fa-solid fa-home"></i> ${newLabel.trim()}&nbsp;<i class="fa-solid fa-chevron-down dropdown-icon"></i>`;
                }
            }

            cancelEditAddress(card); // Clean up
        })
        .catch(err => {
            console.error(err);
            alert(err.message || 'Error updating address label.');
        })
        .finally(() => {
            toggleGlobalSpinner(false);
        });
}
function startEditAddress(button) {
    const card = button.closest('.address-card');
    if (!card) return;

    const label = card.querySelector('.address-label');
    const radio = card.querySelector('.address-radio');
    const editBtn = card.querySelector('.address-edit');
    const deleteBtn = card.querySelector('.address-delete');

    // Get current label text
    const currentLabel = label?.textContent?.trim() || '';

    // Create input
    const input = document.createElement('input');
    input.type = 'text';
    input.className = 'edit-address-input';
    input.value = currentLabel;
    input.style.width = '100%';

    // Create Save/Cancel buttons
    const saveBtn = document.createElement('button');
    saveBtn.innerHTML = '<i class="fa-solid fa-check"></i>';
    saveBtn.className = 'address-save';
    saveBtn.onclick = () => saveEditedAddress(card, input.value);

    const cancelBtn = document.createElement('button');
    cancelBtn.innerHTML = '<i class="fa-solid fa-xmark"></i>';
    cancelBtn.className = 'address-cancel';
    cancelBtn.onclick = () => cancelEditAddress(card);

    // Hide original elements
    if (label) label.style.display = 'none';
    if (radio) radio.style.display = 'none';
    if (editBtn) editBtn.style.display = 'none';
    if (deleteBtn) deleteBtn.style.display = 'none';

    // Inject input + buttons
    label?.parentElement?.appendChild(input);
    label?.parentElement?.appendChild(saveBtn);
    label?.parentElement?.appendChild(cancelBtn);

    input.focus();
}

function toggleGlobalSpinner(show = false) {
    // Check if spinner element already exists
    let spinner = document.getElementById('globalOverlaySpinner');
    if (!spinner) {
        spinner = document.createElement('div');
        spinner.id = 'globalOverlaySpinner';
        spinner.style.position = 'fixed';
        spinner.style.top = '50%';
        spinner.style.left = '50%';
        spinner.style.transform = 'translate(-50%, -50%)';
        spinner.style.zIndex = '9999';
        spinner.style.display = 'none';
        spinner.innerHTML = `
            <div class="spinner spinner--lg"></div>
        `;
        document.body.appendChild(spinner);
    }

    spinner.style.display = show ? 'block' : 'none';
}