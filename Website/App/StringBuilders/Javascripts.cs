using System.Text;

/// The Strings must be manaually string built.  because JS sucks!!!
namespace Website.App.StringBuilders
{
    public static partial class Javascripts
    {
        public static void Create()
        {
            string path = Path.Combine("wwwroot", "js", "site.js");
            if (File.Exists(path)) File.Delete(path);
            File.WriteAllText(path, Get());
        }
        private static string Get()
        {
            StringBuilder sb = new();
            sb.Append(Global.Get());
            sb.Append(Pages.Home.Modals.Address.Get());
            sb.Append(Pages.Home.Modals.Location.Get());
            // add more here...

            return sb.ToString();

        }
        private static class Global
        {
            internal static string Get()
            {
                StringBuilder sb = new();
                sb.Append(Common())
                .AppendLine(Spinner());

                return sb.ToString();
            }
            private static string Common()
            {
                StringBuilder sb = new();

                sb.Append("let _globalSpinner; ")
                  .Append("async function openModalByType(modalType) { ")
                  .Append("const modal = document.querySelector(`.custom-modal[data-modal-type = '${modalType}']`); ")
                  .Append("if (!modal) return; modal.classList.add('show'); }; ")
                  .Append("async function closeModalByType(modalType) { ")
                  .Append("const modal = document.querySelector(`.custom-modal[data-modal-type = '${modalType}']`); ")
                  .Append("if (!modal) return; modal.classList.remove('show'); }; ")

                  .Append("window.addEventListener('pageshow',e=>{if(e.persisted)location.reload();}); ")

                  .Append("document.addEventListener('DOMContentLoaded', () => { ")
                  .Append("const resetButton = document.getElementById('AMBTNCLS'); ")
                  .Append("if (resetButton) { resetButton.addEventListener('click', AMBtnCls);}}); ")
                  .Append("window.AMBtnCls = function () { CloseNewAddressCard(); closeModalByType('address');}; ");
                return sb.ToString();
            }
            private static string Spinner()
            {
                StringBuilder sb = new();
                sb.Append("function toggleGlobalSpinner(show = false) {")
                  .Append("if (!_globalSpinner) {")
                  .Append("_globalSpinner = document.createElement('div'); ")
                  .Append("_globalSpinner.id = 'globalOverlaySpinner'; ")
                  .Append("Object.assign(_globalSpinner.style, {")
                  .Append("position:'fixed',")
                  .Append("top:'0',")
                  .Append("left:'0',")
                  .Append("width:'100vw',")
                  .Append("height:'100vh',")
                  .Append("background:'rgba(0,0,0,0.35)',")
                  .Append("display:'flex',")
                  .Append("justifyContent:'center',")
                  .Append("alignItems:'center',")
                  .Append("zIndex:'9999',")
                  .Append("opacity:'0',")
                  .Append("pointerEvents:'none',")
                  .Append("transition:'opacity 0.25s ease'")
                  .Append("}); ")
                  .Append("_globalSpinner.innerHTML = '<div class=\"spinner spinner--lg\"></div>'; ")
                  .Append("document.body.appendChild(_globalSpinner); ")
                  .Append("} ")
                  .Append("if(show) {")
                  .Append("_globalSpinner.style.opacity = '1';")
                  .Append("_globalSpinner.style.pointerEvents = 'auto';")
                  .Append("} else {")
                  .Append("_globalSpinner.style.opacity = '0';")
                  .Append("_globalSpinner.style.pointerEvents = 'none';")
                  .Append("}")
                  .Append("}; ")
                  .Append("function showOverlay(show = true, id = 'globalSpinner') { const el = document.getElementById(id); if (el) el.hidden = !show; }");

                return sb.ToString();
            }
        }
        private static class Pages
        {
            internal static class Home
            {
                internal static class Modals
                {
                    internal static class Address
                    {
                        internal static string Get()
                        {
                            StringBuilder sb = new();
                            sb.Append(ResetNewAddressInputs())
                            .Append(CloseNewAddressCard())
                            .Append(UpdateDefaultAddress())
                            .Append(StartEditAddress())
                            .Append(CancelEditAddress())
                            .Append(SaveEditedAddress())
                            .Append(SaveNewAddress())
                            .Append(DeleteAddressCard())
                            .Append(AddNewAddressCard());

                            return sb.ToString();
                        }
                        private static string ResetNewAddressInputs()
                        {
                            StringBuilder sb = new();
                            sb.Append("window.ResetNewAddressInputs = function () { ")
                              .Append("const label = document.getElementById('AMNAL'); ")
                              .Append("const search = document.getElementById('AMNAS'); ")
                              .Append("const payload = document.getElementById('AMHJSON'); ")
                              .Append("const type = document.getElementById('AMNATID'); ")
                              .Append("const list = document.getElementById('AMACL'); ")
                              .Append("if (label) label.value = ''; ")
                              .Append("if (search) search.value = ''; ")
                              .Append("if (payload) payload.value = ''; ")
                              .Append("if (type) type.selectedIndex = 0; ")
                              .Append("if (list) list.hidden = true; ")
                              .AppendLine("};");
                            return sb.ToString();
                        }
                        private static string CloseNewAddressCard()
                        {
                            StringBuilder sb = new();
                            sb.Append("window.CloseNewAddressCard = function () ")
                            .Append("{")
                            .Append("toggleGlobalSpinner(true); ")
                            .Append("ResetNewAddressInputs(); ")
                            .Append("const modal = document.querySelector('.custom-modal[data-modal-type=\"address\"]'); ")
                            .Append("if (!modal) return; ")
                            .Append("const newCard = modal.querySelector('.new-address-card'); ")
                            .Append("if (newCard) newCard.style.display = 'none';")
                            .Append("toggleGlobalSpinner(false); ")
                            .AppendLine("}");
                            return sb.ToString();
                        }
                        private static string UpdateDefaultAddress()
                        {
                            StringBuilder sb = new();
                            sb.Append("window.UpdateDefaultAddress = function (radio) ")
                                .Append("{ ")
                                .Append("if (!radio) return; ")
                                .Append("const card = radio.closest('.address-card'); ")
                                .Append("if (!card) return; ")
                                .Append("const selectedId = parseInt(radio.value, 10); ")
                                .Append("if (!selectedId || isNaN(selectedId)) return; ")
                                .Append("toggleGlobalSpinner(true); ")
                                .Append("fetch('/User/Address/EndPoints/SetDefault', { ")
                                .Append("method: 'POST', ")
                                .Append("headers: { 'Content-Type': 'application/json' }, ")
                                .Append("body: JSON.stringify({ Id: selectedId }) ")
                                .Append("}) ")
                                .Append(".then(resp => resp.json()) ")
                                .Append(".then(data => { ")
                                .Append("if (!data?.ok) throw new Error(data?.msg || 'Failed to save.'); ")
                                .Append("radio.checked = true; ")
                                .Append("document.querySelectorAll('.address-card').forEach(c => { ")
                                .Append("const r = c.querySelector('.address-radio'); ")
                                .Append("const delBtn = c.querySelector('.address-delete'); ")
                                .Append("if (!r || !delBtn) return; ")
                                .Append("delBtn.style.display = r.checked ? 'none' : 'block'; ")
                                .Append("}); ")
                                .Append("const labelText = card.querySelector('.address-label')?.textContent?.trim() || ''; ")
                                .Append("const lineText = card.querySelector('.address-line')?.textContent?.trim() || ''; ")
                                .Append("const preview = labelText ? labelText : (data.label || lineText); ")
                                .Append("const toggle = document.getElementById('addressToggle'); ")
                                .Append("if (toggle) ")
                                .Append("{ ")
                                .Append("toggle.innerHTML = `<i class=\"fa-solid fa-home\"></i>${preview}&nbsp;<i class=\"fa-solid fa-chevron-down dropdown-icon\"></i>`; ")
                                .Append("} ")
                                .Append("}) ")
                                .Append(".catch(err => { ")
                                .Append("console.error(err); ")
                                .Append("alert(err.message || 'Error saving default address.'); ")
                                .Append("}) ")
                                .Append(".finally(() => { ")
                                .Append("toggleGlobalSpinner(false); ")
                                .Append("}); ")
                                .AppendLine("}");

                            return sb.ToString();
                        }
                        private static string StartEditAddress()
                        {
                            StringBuilder sb = new();
                            sb.Append("window.StartEditAddress = function (button) ")
                                .Append("{ ")
                                .Append("if (!button) return; ")
                                .Append("const card = button.closest('.address-card'); ")
                                .Append("if (!card) return; ")
                                .Append("const label = card.querySelector('.address-label'); ")
                                .Append("if (label) label.style.display = 'none'; ")
                                .Append("const radio = card.querySelector('.address-radio'); ")
                                .Append("if (radio) radio.style.display = 'none'; ")
                                .Append("const input = card.querySelector('.address-edit-input'); ")
                                .Append("if (input) ")
                                .Append("{ ")
                                .Append("toggleGlobalSpinner(true); ")
                                .Append("input.style.display = 'block'; ")
                                .Append("input.style.width = '100%'; ")
                                .Append("input.style.boxSizing = 'border-box'; ")
                                .Append("input.focus(); ")
                                .Append("const valLength = input.value.length; ")
                                .Append("input.setSelectionRange(valLength, valLength); ")
                                .Append("} ")
                                .Append("const normalActions = card.querySelector('.address-actions'); ")
                                .Append("if (normalActions) normalActions.style.display = 'none'; ")
                                .Append("const editActions = card.querySelector('.address-actions-edit'); ")
                                .Append("if (editActions) ")
                                .Append("{ ")
                                .Append("Object.assign(editActions.style, {display: 'flex', flexDirection: 'column', justifyContent: 'flex-start', marginLeft: '1rem'}); ")
                                .Append("const saveBtn = editActions.querySelector('.address-save'); ")
                                .Append("if (saveBtn) saveBtn.style.display = 'inline-block'; ")
                                .Append("const cancelBtn = editActions.querySelector('.address-cancel'); ")
                                .Append("if (cancelBtn) cancelBtn.style.display = 'inline-block'; ")
                                .Append("} ")
                                .Append("toggleGlobalSpinner(false); ")
                                .AppendLine("}");

                            return sb.ToString();
                        }
                        private static string CancelEditAddress()
                        {
                            StringBuilder sb = new();
                            sb.Append("window.CancelEditAddress = function (button) ")
                                .Append("{ ")
                                .Append("if (!button) return; ")
                                .Append("const card = button.closest('.address-card'); ")
                                .Append("if (!card) return; ")
                                .Append("const label = card.querySelector('.address-label'); ")
                                .Append("if (label) label.style.display = 'block'; ")
                                .Append("const radio = card.querySelector('.address-radio'); ")
                                .Append("if (radio) radio.style.display = 'inline-block'; ")
                                .Append("const input = card.querySelector('.address-edit-input'); ")
                                .Append("if (input) ")
                                .Append("{ ")
                                .Append("toggleGlobalSpinner(true); ")
                                .Append("input.style.display = 'none'; ")
                                .Append("input.value = label?.textContent?.trim() || ''; ")
                                .Append("} ")
                                .Append("const normalActions = card.querySelector('.address-actions'); ")
                                .Append("if (normalActions) normalActions.style.display = 'block'; ")
                                .Append("const editActions = card.querySelector('.address-actions-edit'); ")
                                .Append("if (editActions) editActions.style.display = 'none'; ")
                                .Append("toggleGlobalSpinner(false); ")
                                .AppendLine("}");

                            return sb.ToString();
                        }
                        private static string SaveEditedAddress()
                        {
                            StringBuilder sb = new();
                            sb.Append("window.SaveEditedAddress = function (card, newLabel) ")
                                .Append("{ ")
                                .Append("if (!card || !newLabel?.trim()) return; ")
                                .Append("const radio = card.querySelector('.address-radio'); ")
                                .Append("const addressId = parseInt(radio?.value, 10); ")
                                .Append("if (!addressId) return; ")
                                .Append("toggleGlobalSpinner(true); ")
                                .Append("fetch('/User/Address/EndPoints/EditLabel', { ")
                                .Append("method: 'POST', ")
                                .Append("headers: { 'Content-Type': 'application/json' }, ")
                                .Append("body: JSON.stringify({ Id: addressId, Label: newLabel.trim() }) ")
                                .Append("}) ")
                                .Append(".then(resp => resp.json()) ")
                                .Append(".then(data => { ")
                                .Append("if (!data?.ok) throw new Error(data?.msg || 'Failed to update label.'); ")
                                .Append("const label = card.querySelector('.address-label'); ")
                                .Append("if (label) label.textContent = ' ' + newLabel.trim(); ")
                                .Append("if (radio?.checked) { ")
                                .Append("const toggle = document.getElementById('addressToggle'); ")
                                .Append("if (toggle) ")
                                .Append("{ ")
                                .Append("toggle.innerHTML = `<i class=\"fa-solid fa-home\"></i> ${newLabel.trim()}&nbsp;<i class=\"fa-solid fa-chevron-down dropdown-icon\"></i>`; ")
                                .Append("} ")
                                .Append("} ")
                                .Append("CancelEditAddress(card); ")
                                .Append("}) ")
                                .Append(".catch(err => { ")
                                .Append("console.error(err); ")
                                .Append("alert(err.message || 'Error updating address label.'); ")
                                .Append("}) ")
                                .Append(".finally(() => { ")
                                .Append("toggleGlobalSpinner(false); ")
                                .Append("}); ")
                                .AppendLine("}");

                            return sb.ToString();
                        }
                        private static string SaveNewAddress()
                        {
                            StringBuilder sb = new();
                            sb.Append("window.SaveNewAddress = async function (card) { ")
                              .Append("if (!card) { alert('No card passed'); return; } ")
                              .Append("const label = document.getElementById('AMNAL')?.value.trim(); ")
                              .Append("const searchJSON = document.getElementById('AMHJSON')?.value; ")
                              .Append("const typeID = document.getElementById('AMNATID')?.value; ")
                              .Append("const modalId = document.querySelector('.AddressModalId')?.value; ")
                              .Append("if (!label || !searchJSON || !typeID) { alert('Validation failed: Missing Label, Address or Address Type'); return; } ")
                              .Append("toggleGlobalSpinner(true); ")
                              .Append("try { ")
                              .Append("const resp = await fetch('/User/Address/EndPoints/AddAddress', { ")
                              .Append("method: 'POST', ")
                              .Append("headers: { 'Content-Type': 'application/json' }, ")
                              .Append("body: JSON.stringify({ ModalId: modalId, Label: label, AddressJSON: searchJSON, TypeID: parseInt(typeID) }) ")
                              .Append("}); ")
                              .Append("if (!resp.ok) { ")
                              .Append("let errMsg = 'Server returned an error'; ")
                              .Append("try { const errData = await resp.json(); errMsg = errData?.msg || errMsg; } catch {} ")
                              .Append("throw new Error(errMsg); ")
                              .Append("} ")
                              .Append("const data = await resp.json(); ")
                              .Append("if (!data?.ok) throw new Error(data?.msg || 'Failed to save new address'); ")
                              .Append("AddNewAddressCard(data); ")
                              .Append("CloseNewAddressCard(); ")
                              .Append("} catch(err) { ")
                              .Append("alert('ERROR: ' + (err.message || 'Unknown error')); ")
                              .Append("} finally { ")
                              .Append("toggleGlobalSpinner(false); ")
                              .Append("} ")
                              .AppendLine("};");

                            return sb.ToString();
                        }
                        private static string AddNewAddressCard()
                        {
                            StringBuilder sb = new();

                            sb.Append("window.AddNewAddressCard = function (data) { ")
                              .Append("if (!data || !data.newcard) return; ")
                              .Append("const modal = document.querySelector('.custom-modal[data-modal-type=\"address\"]'); ")
                              .Append("if (!modal) return; ")
                              .Append("const container = modal.querySelector('.custom-modal-body'); ")
                              .Append("if (!container) return; ")
                              .Append("const temp = document.createElement('div'); ")
                              .Append("temp.innerHTML = data.newcard.trim(); ")
                              .Append("const newCard = temp.firstElementChild; ")
                              .Append("if (!newCard) return; ")
                              .Append("const addSection = container.querySelector('.mt-3'); ")
                              .Append("if (addSection) { container.insertBefore(newCard, addSection); } else { container.appendChild(newCard); } ")
                              .Append("newCard.style.opacity = 0; ")
                              .Append("newCard.style.transform = 'translateY(-10px)'; ")
                              .Append("newCard.style.transition = 'opacity 0.5s ease, transform 0.5s ease'; ")
                              .Append("if (newCard.style.display === 'none') { newCard.style.display = ''; } ")
                              .Append("requestAnimationFrame(() => { newCard.style.opacity = 1; newCard.style.transform = 'translateY(0)'; }); ")
                              .Append("container.scrollTo({ top: newCard.offsetTop, behavior: 'smooth' }); ")
                              .AppendLine("} ");

                            return sb.ToString();
                        }
                        private static string DeleteAddressCard()
                        {
                            StringBuilder sb = new();

                            sb.Append("window.DeleteAddressCard = async function (button) { ")
                            .Append("if (!button) return; ")
                            .Append("const card = button.closest('.address-card'); ")
                            .Append("if (!card) return; ")
                            .Append("const radio = card.querySelector('.address-radio'); ")
                            .Append("if (!radio) return; ")
                            .Append("const addressId = parseInt(radio.value, 10); ")
                            .Append("const label = card.querySelector('.address-label')?.textContent?.trim() || 'this address'; ")
                            .Append("if (!window.confirm(`Are you sure you want to delete \"${label}\"?`)) { ")
                            .Append("card.scrollIntoView({ behavior: 'smooth', block: 'center' }); ")
                            .Append("radio.focus(); ")
                            .Append("return; ")
                            .Append("} ")
                            .Append("toggleGlobalSpinner(true); ")
                            .Append("try { ")
                            .Append("const response = await fetch('/User/Address/EndPoints/DeleteSelected', { ")
                            .Append("method: 'POST', ")
                            .Append("headers: { 'Content-Type': 'application/json' }, ")
                            .Append("body: JSON.stringify({ Id: addressId }) ")
                            .Append("}); ")
                            .Append("const data = await response.json(); ")
                            .Append("if (!data?.ok) throw new Error(data?.msg || 'Failed to delete address.'); ")
                            .Append("card.remove(); ")
                            .Append("} catch (err) { ")
                            .Append("console.error(err); ")
                            .Append("alert(err.message || 'Error deleting address.'); ")
                            .Append("} finally { ")
                            .Append("toggleGlobalSpinner(false); ")
                            .Append("} ")
                            .AppendLine("} ");

                            return sb.ToString();
                        }
                    }
                    internal static class Location
                    {
                        internal static string Get()
                        {
                            StringBuilder sb = new();

                            sb.AppendLine(ResetLocationModalInputs())
                            .AppendLine(ResolveCurrentLocation())
                            .AppendLine(ValidateLocationModal())
                            .AppendLine(SaveLocation())
                            .AppendLine(GetAddressFromCoords());
                            return sb.ToString();
                        }
                        private static string ResetLocationModalInputs()
                        {
                            StringBuilder sb = new();
                            sb.Append("window.ResetLocationModalInputs = function () { ")
                            .Append("const type = document.getElementById('locationAddressTypeId'); ")
                            .Append("const preview = document.getElementById('locationPreview'); ")
                            .Append("const payload = document.getElementById('locationPayload'); ")
                            .Append("if (type) type.selectedIndex = 0; ")
                            .Append("if (preview) preview.textContent = '(not loaded yet)'; ")
                            .Append("if (payload) payload.value = ''; ")
                            .Append("const errEl = document.getElementById('locationTypeError'); ")
                            .Append("if (errEl) errEl.textContent = ''; } ");

                            return sb.ToString();
                        }
                        private static string ResolveCurrentLocation()
                        {
                            StringBuilder sb = new();
                            sb.Append("window.ResolveCurrentLocation = function () { ")
                            .Append("const preview = document.getElementById('locationPreview'); ")
                            .Append("if (!('geolocation' in navigator)) { ")
                            .Append("if (preview) preview.textContent = 'Location not supported.'; ")
                            .Append("return; } ")
                            .Append("if (preview) preview.textContent = 'Getting location…'; ")
                            .Append("const tryGeolocation = (highAccuracy, retries = 3) => { ")
                            .Append("navigator.geolocation.getCurrentPosition( ")
                            .Append("(pos) => { ")
                            .Append("const { latitude: lat, longitude: lon, accuracy } = pos.coords; ")
                            .Append("if (highAccuracy && accuracy > 15 && retries > 0) ")
                            .Append("{ if (preview) preview.textContent = `Accuracy low(${ Math.round(accuracy)} m), retrying…`; ")
                            .Append("setTimeout(() => tryGeolocation(true, retries - 1), 1000); ")
                            .Append("return; } ")
                            .Append("GetAddressFromCoords(lat, lon); }, ")
                            .Append("(err) => { ")
                            .Append("if (highAccuracy) { ")
                            .Append("if (preview) preview.textContent = 'High-accuracy failed, trying low-accuracy…'; ")
                            .Append("tryGeolocation(false, 1); ")
                            .Append("} else { ")
                            .Append("if (preview) preview.textContent = 'Unable to get location. Please enter manually.'; ")
                            .Append("} }, { enableHighAccuracy: highAccuracy, timeout: highAccuracy? 10000 : 5000, maximumAge: 0 } ); }; ")
                            .Append("tryGeolocation(true, 3); }");

                            return sb.ToString();

                        }
                        private static string ValidateLocationModal()
                        {
                            StringBuilder sb = new();
                            sb.Append("window.ValidateLocationModal = function () { ")
                              .Append("const type = document.getElementById('LMATID'); ")
                              .Append("const payload = document.getElementById('locationPayload'); ")
                              .Append("const errEl = document.getElementById('locationTypeError'); ")
                              .Append("if (!type || !payload) return false; ")
                              .Append("let valid = true; ")
                              .Append("if (type.selectedIndex === 0) { ")
                              .Append("if (errEl) errEl.textContent = 'Please select an address type.'; ")
                              .Append("valid = false; } else { ")
                              .Append("if (errEl) errEl.textContent = ''; } ")
                              .Append("if (!payload.value) { ")
                              .Append("alert('No address data found. Please resolve your location first.'); ")
                              .Append("valid = false; } ")
                              .Append("return valid; }; ");

                            return sb.ToString();
                        }
                        private static string SaveLocation()
                        {
                            StringBuilder sb = new();
                            sb.Append("window.SaveLocation = function () {")
                              .Append("const preview = document.getElementById('locationPreview'); ")
                              .Append("const typeSelect = document.getElementById('LMATID'); ")
                              .Append("const payloadEl = document.getElementById('locationPayload'); ")

                              .Append("if (!typeSelect?.value) { alert('Please select an address type.'); return; } ")
                              .Append("if (!preview || preview.textContent === '(not loaded yet)') return; ")
                              .Append("if (!payloadEl?.value) { alert('No location to save.'); return; } ")

                              .Append("const addressData = JSON.parse(payloadEl.value); ")
                              .Append("const typeID = parseInt(typeSelect.value, 10); ")

                              .Append("toggleGlobalSpinner(true); ")

                              .Append("fetch('/User/Address/EndPoints/AddFromLocation', { ")
                              .Append("method: 'POST', ")
                              .Append("headers: { 'Content-Type': 'application/json' }, ")
                              .Append("body: JSON.stringify({ AddressTypeID: typeID, JSonpayload: JSON.stringify(addressData) }) ")
                              .Append("}) ")
                              .Append(".then(resp => resp.json()) ")
                              .Append(".then(data => { ")
                              .Append("if (!data?.ok && !data?.success) throw new Error(data?.message || 'Save failed.'); ")

                              .Append("const toggle = document.getElementById('addressToggle'); ")
                              .Append("const label = (data.label || preview.textContent || '').trim(); ")
                              .Append("if (toggle && label) { ")
                              // backticks kept for template literal; escape inner double-quotes for C# string
                              .Append("toggle.innerHTML = `<i class=\"fa-solid fa-home\"></i>${label}&nbsp;<i class=\"fa-solid fa-chevron-down dropdown-icon\"></i>`; ")
                              .Append("} ")
                              .Append("if (data.addressId) { ")
                              .Append("const modal = document.querySelector('.custom-modal[data-modal-type=\"address\"]'); ")
                              .Append("const modalId = modal?.getAttribute('data-modal-id') || ''; ")

                              .Append("fetch('/User/Address/EndPoints/BuildCard', { ")
                              .Append("method: 'POST', ")
                              .Append("headers: { 'Content-Type': 'application/json' }, ")
                              .Append("body: JSON.stringify({ ModalId: modalId, UserAddressId: data.addressId, SetDefault: true }) ")
                              .Append("}) ")
                              .Append(".then(resp => resp.json()) ")
                              .Append(".then(cardData => { ")
                              .Append("if (cardData?.ok && cardData?.newcard) { ")
                              .Append("AddNewAddressCard(cardData); ")
                              .Append("} ")
                              .Append("}) ")
                              .Append(".catch(err => { console.error('BuildCard failed', err); }) ")
                              .Append(".finally(() => { CloseLocationModal(); }); ")
                              .Append("} else { CloseLocationModal(); } ")

                              .Append("}) ")
                              .Append(".catch(err => { if (preview) preview.textContent = err?.message || 'Save failed.'; console.error(err); }) ")
                              .Append(".finally(() => { toggleGlobalSpinner(false); ResetLocationModalInputs(); closeModalByType('location'); });")

                              .Append("};");

                            return sb.ToString();
                        }
                        private static string GetAddressFromCoords()
                        {
                            StringBuilder sb = new();

                            sb.Append("window.GetAddressFromCoords = function (lat, lon) { ")
                              .Append("const preview = document.getElementById('locationPreview'); ")
                              .Append("const payload = document.getElementById('locationPayload'); ")
                              .Append("const token = document.querySelector('meta[name=\"t\"]')?.content?.trim(); ")

                              .Append("if (!token) { ")
                              .Append("if (preview) preview.textContent = 'Mapbox token missing.'; ")
                              .Append("return; } ")

                              .Append("if (!Number.isFinite(lat) || !Number.isFinite(lon)) { ")
                              .Append("if (preview) preview.textContent = 'Invalid coordinates.'; ")
                              .Append("return; } ")

                              .Append("if (preview) preview.textContent = 'Resolving address…'; ")

                              .Append("const url = `https://api.mapbox.com/geocoding/v5/mapbox.places/${lon},${lat}.json?types=address&limit=1&country=nz&access_token=${encodeURIComponent(token)}`; ")

                              .Append("fetch(url) ")
                              .Append(".then(res => res.json()) ")
                              .Append(".then(data => { ")
                              .Append("const feature = data?.features?.[0] || null; ")
                              .Append("if (!feature) throw new Error('No address found for this location.'); ")

                              .Append("const label = (feature.place_name || feature.text || '').trim(); ")

                              .Append("if (preview) preview.textContent = label; ")
                              .Append("if (payload) payload.value = JSON.stringify({ ")
                              .Append("latitude: lat, ")
                              .Append("longitude: lon, ")
                              .Append("feature: feature ")
                              .Append("}); ")
                              .Append("}) ")
                              .Append(".catch(err => { ")
                              .Append("if (preview) preview.textContent = err?.message || 'Unable to resolve address.'; ")
                              .Append("}); ")
                              .Append("} ");

                            return sb.ToString();
                        }
                    }
                }
            }
        }
    }
}
