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




