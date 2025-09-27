using Microsoft.Extensions.Primitives;
using System.Text;

namespace Website.App.Scripts.Pages.User.Dashboard
{
    public static class Index
    {
        private static string closecurrentaddressmodal = string.Empty;
        private static string closenewaddresscard = string.Empty;
        private static string resetnewaddressinputs = string.Empty;
        private static string updatedefaultaddress = string.Empty;
        private static string deleteaddresscard = string.Empty;
        private static string starteditaddress = string.Empty;
        private static string canceleditaddress = string.Empty;
        private static string saveeditedaddress = string.Empty;
        public static string CloseCurrentAddressModal(bool forceReload = false)
        {
            if (closecurrentaddressmodal == string.Empty || forceReload)
            {
                StringBuilder sb = new();
                sb.Append("function closeAddressModal() ");
                sb.Append('{');
                sb.Append("ResetNewAddressInputs(); ");

                sb.Append("const section = document.getElementById('addaddresssection'); ");
                sb.Append("if (section) section.style.display = 'none'; ");

                //sb.Append("const modal = document.getElementById('addressModal'); ");
                //sb.Append("if (modal) ");
                //sb.Append('{');
                //sb.Append("modal.scrollTop = 0; ");
                //sb.Append("modal.style.display = 'none'; "); // or remove 'show' class if you're using Bootstrap
                //sb.Append('}');
                sb.AppendLine("} ");
                closecurrentaddressmodal = sb.ToString();
            }
            return closecurrentaddressmodal;
        }
        public static string ResetNewAddressInputs(bool forceReload = false)
        {
            if (resetnewaddressinputs == string.Empty || forceReload)
            {
                StringBuilder sb = new();
                sb.Append("function ResetNewAddressInputs() {");
                sb.Append("const label = document.getElementById('newLabel'); ");
                sb.Append("const search = document.getElementById('NewAddressSearch'); ");
                sb.Append("const payload = document.getElementById('NewMapboxAddressJSON'); ");
                sb.Append("const type = document.getElementById('NewAddressTypeID'); ");
                sb.Append("const list = document.getElementById('NewAutocompleteList'); ");
                sb.Append("if (label) label.value = ''; ");
                sb.Append("if (search) search.value = ''; ");
                sb.Append("if (payload) payload.value = ''; ");
                sb.Append("if (type) type.selectedIndex = 0; ");
                sb.Append("if (list) list.hidden = true; ");
                sb.AppendLine("} ");
                resetnewaddressinputs = sb.ToString();
            }
            return resetnewaddressinputs;
        }
        public static string CloseNewAddressCard(bool forceReload = false)
        {
            if (closenewaddresscard == string.Empty || forceReload)
            {
                StringBuilder sb = new();
                sb.Append("function CloseNewAddressCard() {");
                sb.Append("ResetNewAddressInputs(); ");
                sb.Append("const btnCancel = document.getElementById('addressCancel'); ");
                sb.Append("const btnSave = document.getElementById('addressSave'); ");
                sb.Append("const section = document.getElementById('addaddresssection'); ");
                sb.Append("if (btnCancel) btnCancel.style.display = \"block\"; ");
                sb.Append("if (btnSave) btnSave.style.display = \"block\"; ");
                sb.Append("if (section) section.style.display = 'none'; ");
                sb.AppendLine("} ");
                closenewaddresscard = sb.ToString();
            }
            return closenewaddresscard;
        }
        public static string UpdateDefaultAddress(bool forceReload = false)
        {
            if (string.IsNullOrEmpty(updatedefaultaddress) || forceReload)
            {
                StringBuilder sb = new();

                sb.Append("function UpdateDefaultAddress(radio) {");
                sb.Append("if (!radio) return; ");
                sb.Append("const card = radio.closest('.address-card'); ");
                sb.Append("if (!card) return; ");
                sb.Append("const selectedId = parseInt(radio.value, 10); ");
                sb.Append("if (!selectedId || isNaN(selectedId)) return; ");
                sb.Append("toggleGlobalSpinner(true); "); // Start spinner

                sb.Append("fetch('/User/Address/EndPoints/SetDefault', {");
                sb.Append("method: 'POST', ");
                sb.Append("headers: { 'Content-Type': 'application/json' }, ");
                sb.Append("body: JSON.stringify({ Id: selectedId }) ");
                sb.Append("}) ");
                sb.Append(".then(resp => resp.json()) ");
                sb.Append(".then(data => { ");
                sb.Append("if (!data?.ok) throw new Error(data?.msg || 'Failed to save.'); ");
                sb.Append("radio.checked = true; ");

                sb.Append("document.querySelectorAll('.address-card').forEach(c => { ");
                sb.Append("const r = c.querySelector('.address-radio'); ");
                sb.Append("const delBtn = c.querySelector('.address-delete'); ");
                sb.Append("if (!r || !delBtn) return; ");
                sb.Append("delBtn.style.display = r.checked ? 'none' : 'block'; ");
                sb.Append("}); ");

                sb.Append("const labelText = card.querySelector('.address-label')?.textContent?.trim() || ''; ");
                sb.Append("const lineText = card.querySelector('.address-line')?.textContent?.trim() || ''; ");
                sb.Append("const preview = labelText ? labelText : (data.label || lineText); ");

                sb.Append("const toggle = document.getElementById('addressToggle'); ");
                sb.Append("if (toggle) { toggle.innerHTML = `<i class=\"fa-solid fa-home\"></i> ${preview}&nbsp;<i class=\"fa-solid fa-chevron-down dropdown-icon\"></i>`; } ");

                sb.Append("}) "); // closes then block
                sb.Append(".catch(err => { console.error(err); alert(err.message || 'Error saving default address.'); }) ");
                sb.Append(".finally(() => { toggleGlobalSpinner(false); }); ");

                sb.AppendLine("} "); // closes function

                updatedefaultaddress = sb.ToString();
            }

            return updatedefaultaddress;
        }
        public static string DeleteAddressCard(bool forceReload = false)
        {
            if (string.IsNullOrEmpty(deleteaddresscard) || forceReload)
            {
                StringBuilder sb = new();

                sb.Append("function DeleteAddressCard(button) { ");
                sb.Append("if (!button) return; ");
                sb.Append("const card = button.closest('.address-card'); ");
                sb.Append("if (!card) return; ");
                sb.Append("const radio = card.querySelector('.address-radio'); ");
                sb.Append("if (!radio) return; ");
                sb.Append("const addressId = parseInt(radio.value, 10); ");
                sb.Append("const label = card.querySelector('.address-label')?.textContent?.trim() || 'this address'; ");
                sb.Append("if (!window.confirm(`Are you sure you want to delete \"${label}\"?`)) { ");
                sb.Append("card.scrollIntoView({ behavior: 'smooth', block: 'center' }); ");
                sb.Append("radio.focus(); ");
                sb.Append("return; } ");
                sb.Append("toggleGlobalSpinner(true); ");
                sb.Append("fetch('/User/Address/EndPoints/DeleteSelected', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ Id: addressId }) }) ");
                sb.Append(".then(resp => resp.json()) ");
                sb.Append(".then(data => { ");
                sb.Append("if (!data?.ok) throw new Error(data?.msg || 'Failed to delete address.'); ");
                sb.Append("card.remove(); ");
                sb.Append("}) ");
                sb.Append(".catch(err => { ");
                sb.Append("console.error(err); ");
                sb.Append("alert(err.message || 'Error deleting address.'); ");
                sb.Append("}) ");
                sb.Append(".finally(() => { ");
                sb.Append("toggleGlobalSpinner(false); ");
                sb.Append("}); ");
                sb.AppendLine("} ");

                deleteaddresscard = sb.ToString();
            }
            return deleteaddresscard;
        }
        public static string StartEditAddress(bool forceReload = false)
        {
            if (string.IsNullOrEmpty(starteditaddress) || forceReload)
            {
                StringBuilder sb = new();

                sb.Append("function startEditAddress(button) { ");
                sb.Append("const card = button.closest('.address-card'); ");

                // Hide label and radio
                sb.Append("const label = card.querySelector('.address-label'); ");
                sb.Append("if(label){ label.style.display='none'; } ");
                sb.Append("const radio = card.querySelector('.address-radio'); ");
                sb.Append("if(radio){ radio.style.display='none'; } ");

                // Show input
                sb.Append("const input = card.querySelector('.address-edit-input'); ");
                sb.Append("if(input){ input.style.display='inline-block'; width='90%'; input.focus(); ");
                sb.Append("const valLength = input.value.length; ");
                sb.Append("input.setSelectionRange(valLength, valLength); } ");

                // Hide normal actions
                sb.Append("const normalActions = card.querySelector('.address-actions'); ");
                sb.Append("if(normalActions){ normalActions.style.display='none'; } ");

                // Show edit actions
                sb.Append("const editActions = card.querySelector('.address-actions-edit'); ");
                sb.Append("if(editActions) { editActions.style.display='flex'; ");
                sb.Append("editActions.style.flexDirection='column'; editActions.style.justifyContent = 'flex-start'; editActions.style.marginLeft = '1rem';");

                sb.Append("const saveBtn = editActions.querySelector('.address-save'); if(saveBtn){ saveBtn.style.display='inline-block'; } ");
                sb.Append("const cancelBtn = editActions.querySelector('.address-cancel'); if(cancelBtn){ cancelBtn.style.display='inline-block'; } } ");

                sb.AppendLine("} "); // close function

                starteditaddress = sb.ToString();
            }
            return starteditaddress;
        }
        public static string CancelEditAddress(bool forceReload = false)
        {
            if (string.IsNullOrEmpty(canceleditaddress) || forceReload)
            {
                StringBuilder sb = new();

                sb.Append("function cancelEditAddress(button) { ");
                sb.Append("const card = button.closest('.address-card'); ");
                sb.Append("if(!card) return; ");

                sb.Append("const normalActions = card.querySelector('.address-actions'); ");
                sb.Append("if(normalActions) { normalActions.style.display = 'flex'; } ");

                sb.Append("const editActions = card.querySelector('.address-actions-edit'); ");
                sb.Append("if(editActions) { editActions.style.display = 'none'; } ");

                sb.Append("const label = card.querySelector('.address-label'); ");
                sb.Append("if(label) { label.style.display = 'inline-block'; } ");

                sb.Append("const radio = card.querySelector('.address-radio'); ");
                sb.Append("if(radio) { radio.style.display = 'inline-block'; } ");

                sb.Append("const input = card.querySelector('.address-edit-input'); ");
                sb.Append("if(input) { input.style.display = 'none'; } ");

                // Set focus on the card itself
                sb.Append("card.tabIndex = -1; "); // make focusable if not already
                sb.Append("card.focus(); ");

                sb.AppendLine("} "); // close function

                canceleditaddress = sb.ToString();
            }

            return canceleditaddress;
        }
        public static string SaveEditedAddress(bool forceReload = false)
        {
            if (string.IsNullOrEmpty(saveeditedaddress) || forceReload)
            {
                StringBuilder sb = new();

                sb.Append("function saveEditedAddress(card, newLabel) { ");
                sb.Append("const radio = card.querySelector('.address-radio'); ");
                sb.Append("const addressId = parseInt(radio?.value, 10); ");
                sb.Append("if(!addressId || !newLabel.trim()) return; ");

                sb.Append("window.alert('made it to spinner start'); ");
                sb.Append("toggleGlobalSpinner(true); ");
                sb.Append("window.alert('togglespinner called'); ");

                sb.Append("fetch('/User/Address/EndPoints/EditLabel', { ");
                sb.Append("method: 'POST', ");
                sb.Append("headers: { 'Content-Type': 'application/json' }, ");
                sb.Append("body: JSON.stringify({ Id: addressId, Label: newLabel.trim() }) ");
                sb.Append("}) ");
                sb.Append(".then(resp => resp.json()) ");
                sb.Append(".then(data => { ");
                sb.Append("if(!data?.ok) throw new Error(data?.msg || 'Failed to update label.'); ");

                sb.Append("const label = card.querySelector('.address-label'); ");
                sb.Append("if(label) label.textContent = newLabel.trim(); ");

                sb.Append("if(radio?.checked) { ");
                sb.Append("const toggle = document.getElementById('addressToggle'); ");
                sb.Append("if(toggle) { toggle.innerHTML = `<i class=\"fa-solid fa-home\"></i> ${newLabel.trim()}&nbsp;<i class=\"fa-solid fa-chevron-down dropdown-icon\"></i>`; } ");
                sb.Append("} ");

                sb.Append("cancelEditAddress(card); "); // Clean up
                sb.Append("}) ");
                sb.Append(".catch(err => { console.error(err); alert(err.message || 'Error updating address label.'); }) ");
                sb.Append(".finally(() => { toggleGlobalSpinner(false); }); ");

                sb.AppendLine("} "); // close function

                saveeditedaddress = sb.ToString();
            }

            return saveeditedaddress;
        }
    }
}
