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