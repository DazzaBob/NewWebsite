// Opens a modal by type

async function openModalByType(modalType)
{
    const modal = document.querySelector(`.custom-modal[data-modal-type='${modalType}']`);
    if (!modal) return;
        modal.classList.add('show');
}

// Closes a modal by type
async function closeModalByType(modalType) {
    const modal = document.querySelector(`.custom-modal[data-modal-type='${modalType}']`);
    if (!modal) return;
        modal.classList.remove('show');
}