using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.Text;

namespace Website.App.HTML.Pages.Dashboard
{
    public static class Index
    {
        public static string AddressModal(Helper.Connection EntityConnection, Helper.Connection LocationConnection, long userId)
        {
            using DataTable dt = Database.Views.UserAddress.DataTable(EntityConnection, LocationConnection, userId);

            StringBuilder sb = new();
            sb.Append("<div id=\"addressmodal\" class=\"custom-modal\">");
            sb.Append("<div id=\"addressmodalcontent\" class=\"custom-modal-content\" role=\"dialog\" aria-modal=\"true\" aria-labelledby=\"addressmodaltitle\" tabindex=\"-1\">");

            sb.Append("<div id=\"addressmodalheader\" class=\"custom-modal-header\"> ");
            sb.Append("<h5 id=\"addressmodaltitle\"> Select Current Address</h5>");
            sb.Append("<button id=\"addressmodalClose\" class=\"btn-close\" aria-label=\"Close\" onclick=\"closeAddressModal();\">×</button>");
            sb.Append("</div>");
            sb.Append("<div id=\"addressmodalbody\" class=\"custom-modal-body\">");
            if (dt.Rows.Count == 0)
            {
                sb.Append("<div id=\"addressmodalemptystate\" class=\"empty-state\">");
                sb.Append("<p>No saved addresses yet.</p>");
                sb.Append("</div>");
            }
            else
            {
                foreach (DataRow row in dt.Rows)
                {
                    long id = Convert.ToInt64(row["ID"]);
                    bool isDefault = Convert.ToInt32(row["ISDEFAULT"]) == 1;
                    string label = (row["LABEL"]?.ToString() ?? string.Empty).Trim();

                    string streetNo = row["STREET_NUMBER"]?.ToString() ?? string.Empty;
                    string streetName = row["STREET_NAME"]?.ToString() ?? string.Empty;
                    string street = $"{streetNo} {streetName}".Trim();

                    string locality = row["LOCALITY_NAME"]?.ToString() ?? string.Empty;
                    string place = row["PLACE_NAME"]?.ToString() ?? string.Empty;
                    string placeLine = string.IsNullOrWhiteSpace(locality) ? place : $"{locality}, {place}";

                    sb.Append($"<div id=\"addressmodaladdresscard_{id}\" class=\"card address-card\" style=\"display:flex; justify-content:space-between; align-items:center; padding:0.5rem 1rem;\">");

                    // Address Content Start
                    sb.Append($"<div id=\"addressmodaladdresscontent_{id}\" class=\"address-content\" style=\"flex:1;\">");

                    // Address Option Row addressmodaladdresactionsedit
                    sb.Append($"<div id=\"addressmodaladdressoptionrow_{id}\" class=\"address-option-row\" style=\"display:flex; align-items:center; gap:0.5rem;\">");
                    sb.Append($"<input id=\"addressmodaladdressradio_{id}\" class=\"address-radio\" name=\"selectedAddress\" onclick=\"UpdateDefaultAddress(this)\" type=\"radio\" value=\"{id}\" ");
                    if (isDefault)
                    {
                        sb.Append(" checked");
                    }
                    sb.Append(" />");

                    sb.Append($"<label id=\"addressmodaladdresslabel_{id}\" class=\"address-label\" for=\"addressmodaladdressradio_{id}\"><b>{label}</b></label>");
                    sb.Append($"<input id=\"addressmodaladdressinput_{id}\" type=\"text\" class=\"address-edit-input\" value=\"{label}\" style=\"display:none; flex:1;\" />");
                    sb.Append("</div>");

                    // Address Line Start
                    sb.Append($"<div id=\"addressmodaladdress_{id}\" class=\"address-line\">{street},&nbsp;{placeLine}</div>");

                    // Address Content End
                    sb.Append("</div>");

                    // Address Actions Start
                    sb.Append($"<div id=\"addressmodaladdressactions_{id}\" class=\"address-actions\" style=\"margin-left:1rem; display:flex; flex-direction:column; gap:0.25rem;\">");
                    sb.Append($"<button id=\"addressmodaladdressactionsedit_{id}\" type=\"button\" class=\"address-edit\" onclick=\"startEditAddress(this);\"><i class=\"fa-solid fa-pen\"></i></button>");

                    sb.Append($"<button id=\"addressmodaladdressactionsdelete_{id}\" class=\"address-delete\" onclick=\"DeleteAddressCard(this)\" ");
                    if (isDefault)
                        sb.Append("style=\"display:none;\">");
                    else
                        sb.Append("style=\"display:block;\">");
                    sb.Append("<i class=\"fa-solid fa-trash\"></i>").Append("</button>");
                    sb.Append("</div>");

                    // Address Actions Edit Start.
                    sb.Append($"<div id=\"addressmodaladdressedit_{id}\" class=\"address-actions-edit\" style=\"display:none;\">");
                    sb.Append($"<button id=\"addressmodaladdresscancel_{id}\" class=\"address-cancel\" onclick=\"cancelEditAddress(this);\"><i class=\"fa-solid fa-xmark\"></i></button>");
                    sb.Append($"<button id=\"addressmodaladdresssave_{id}\" class=\"address-save\" onclick=\"saveEditedAddress(this.closest('.address-card'), this.closest('.address-card').querySelector('.address-edit-input').value)\"><i class=\"fa-solid fa-check\"></i></button>");
                    sb.Append("</div>");

                    // Address Line Finish
                    sb.Append("</div>");
                    //sb.Append("<span>&nbsp;</span>");
                }
                sb.Append("<div id=\"addressmodaladdaddresssection\" class=\"mt-3\"><a id=\"add-address-link\" href=\"#\" role=\"button\" tabindex=\"0\" onclick=\"showaddaddresscard(); return false;\">+ Add New Address</a></div>");

                sb.Append("<div id=\"addressmodaladdaddresssectionaddresscard\" class=\"card address-card\" style=\"display:none\">");
                sb.Append("<div id=\"addressmodaladdaddresscardform\" class=\"add-address-form\"><div id=\"addressmodaladdaddresscardoptionrow\" class=\"address-option-row\"><label><b>New Address</b></label></div>");
                sb.Append("<div id=\"addressmodaladdaddresscardformgrouplabel\" class=\"form-group mt-2\"><label for=\"addressmodaladdaddresscardnewLabel\">Label</label><input id=\"addressmodaladdaddresscardnewLabel\" type = \"text\" class=\"form-control\" placeholder=\"Home, Work, etc.\"/></div>");

                // Search Address.
                sb.Append("<div id=\"addressmodaladdaddresscardformgroupAddressSearch\" class=\"form-group mt-2\" style=\"position:relative;\">");
                sb.Append("<label for=\"NewAddressSearch\">Search Address</label>");
                sb.Append("<input id=\"NewAddressSearch\" class=\"input-text\" autocomplete=\"off\" placeholder=\"Start typing your address…\" required />");
                sb.Append("<ul id=\"NewAutocompleteList\" class=\"autocomplete-list\" hidden></ul>");
                sb.Append("<input type=\"hidden\" id=\"locationPayload\" />");
                sb.Append("</div>");
                sb.Append("<span>&nbsp;</span>");

                // Address Type Start.
                sb.Append("<div id=\"addressmodaladdaddresscardformgroupNewAddressType\" class=\"form-group mt-2\">");
                sb.Append("<label for=\"addressmodalNewAddressTypeID\">Address Type</label>");
                sb.Append("<select id=\"addressmodalNewAddressTypeID\" class=\"input-text\" required>");
                sb.Append("<option value=\"\">--Select address type--</option>");
                foreach (var item in GetAddressTypeOptions(LocationConnection))
                {
                    sb.Append($"<option value=\"{item.Value}\">{item.Text}</option>");
                }
                sb.Append("</select>");
                sb.Append("</div>");

                sb.Append("<span>&nbsp;</span>");
                sb.Append("<div class=\"mt-3 d-flex gap-2\">");
                sb.Append("<div style=\"display: flex; justify-content: space-between;\">");

                sb.Append("<div style=\"text-align: left;\">");
                sb.Append("<button id=\"addressmodaladdaddresscardformgroupsaveNewAddress\" type=\"button\" class=\"btn btn-primary\" onclick=\"saveNewAddressScript(document.getElementById('addressmodaladdaddresssectionaddresscard'))\">Save</button>");
                sb.Append("</div>");
                sb.Append("<div style=\"text-align: right;\">");
                sb.Append("<button id=\"addressmodaladdaddresscardformgroupcancelNewAddress\" type=\"button\" class=\"btn btn-secondary\" onclick=\"CloseNewAddressCard()\">Cancel</button>");
                sb.Append("</div>");

                sb.Append("</div>");
                sb.Append("</div>");

                sb.Append("</div></div></div>");
                sb.Append("<div id=\"addressmodalfooter\" class=\"custom-modal-footer\" ><span>&nbsp;</span></div>");
                sb.Append("</div></div>");
            }
            return sb.ToString();
        }
        public static string LocationModal(Helper.Connection entityConn, Helper.Connection locationConn, long userId)
        {
            StringBuilder sb = new();

            sb.Append("<div id=\"locationModal\" class=\"custom-modal\">")
              .Append("<div class=\"custom-modal-content\" role=\"dialog\" aria-modal=\"true\" aria-labelledby=\"locationModalTitle\" tabindex=\"-1\">");

            // header
            sb.Append("<div class=\"custom-modal-header\">")
              .Append("<h5 id=\"locationModalTitle\">Current Location</h5>")
              .Append("<button id=\"locationClose\" type=\"button\" class=\"btn-close\" aria-label=\"Close\" onclick=\"CloseLocationModal()\">×</button>")
              .Append("</div>");

            // body start
            sb.Append("<div class=\"location-modal-body\">");

            // resolved address card
            sb.Append("<div class=\"card address-card\">")
              .Append("<div class=\"address-option-row\" style=\"align-items:flex-start\">")
              .Append("<label class=\"address-label\"><b>Resolved Address</b></label>")
              .Append("</div>")
              .Append("<div id=\"locationPreview\" class=\"address-line\">(not loaded yet)</div>")
              .Append("</div>");
            // address type card
            sb.Append("<div class=\"card address-card\">")
              .Append("<div class=\"address-option-row\">")
              .Append("<label for=\"locationAddressTypeId\" class=\"address-label\"><b>Address Type</b></label>")
              .Append("</div>")
              .Append("<select id=\"locationAddressTypeId\" class=\"input-text\" name=\"locationAddressTypeId\" required>")
              .Append("<option value=\"\">--Select address type--</option>");
            foreach (var item in GetAddressTypeOptions(locationConn))
            {
                sb.Append($"<option value=\"{item.Value}\">{item.Text}</option>");
            }
            sb.Append("</select>")
              .Append("<div id=\"locationTypeError\" class=\"field-error\" aria-live=\"polite\"></div>")
              .Append("</div>") // close address-type card
              .Append("</div>"); // close location-modal-body

            // footer
            sb.Append("<div class=\"custom-modal-footer\">")
              .Append("<button id=\"locationCancel\" type=\"button\" class=\"btn btn-secondary\" onclick=\"CloseLocationModal()\">Cancel</button>")
              // no form here, so keep Save as a button and wire click in JS
              .Append("<button id=\"locationSave\" type=\"button\" class=\"btn btn-primary\" onclick=\"SaveLocation()\">Save</button>")
              .Append("</div>");

            // close content & modal
            sb.Append("</div>") // custom-modal-content
              .Append("</div>"); // locationModal

            // NOTE: entityConn and userId are unused here — drop if not needed.

            return sb.ToString();
        }
        public static string PackageDeliveryModal(Helper.Connection EntityConnection, Helper.Connection LocationConnection, long userId)
        {
            StringBuilder sb = new();
            sb.Append("<div id=\"PDmodal\" class=\"custom-modal\">")
                .Append("<div id=\"PDmodalcontent\" class=\"custom-modal-content\" role=\"dialog\" aria-modal=\"true\" aria-labelledby=\"addressmodaltitle\" tabindex=\"-1\">");

            sb.Append("<div id=\"PDmodalheader\" class=\"custom-modal-header\"> ")
                .Append("<h5 id=\"PDmodaltitle\">Package Delivery</h5>")
                .Append("<button id=\"PDmodalClose\" class=\"btn-close\" aria-label=\"Close\" onclick=\"closePDModal();\">×</button>")
              .Append("</div>");

            sb.Append("<div id=\"PDmodalbody\" class=\"custom-modal-body\">")
              .Append("<div style=\"display: flex; flex-direction: column; gap: 0.25rem;\">")
                .Append("<div style=\"display: flex; align-items: center; justify-content: flex-start; gap: 0.4rem;\">")
                    .Append("<label style=\"margin: 0;\">Pickup Location:</label>")
                    .Append("<a href=\"#\" ")
                        .Append("style=\"display:inline-flex; align-items:center; justify-content:center; padding:0; margin:0; text-decoration:none; color:inherit; transition: color 0.3s, transform 0.2s;\" ")
                        .Append("onmouseover=\"this.style.color='var(--color-primary)'; this.style.transform='scale(1.15)';\" ")
                        .Append("onmouseout=\"this.style.color='inherit'; this.style.transform='scale(1)';\">")
                        .Append("<i class=\"fa-solid fa-home\"></i>")
                    .Append("</a>")
                .Append("</div>")
                .Append("<input type=\"text\" name=\"PDmodalpickup\" class=\"address-edit-input\" placeholder=\"Type an address or business name\" required />")
            .Append("</div>")
                .Append("<span>&nbsp;</span>");

            sb.Append("<div style=\"display: flex; flex-direction: column; gap: 0.25rem;\">")
                .Append("<div style=\"display: flex; align-items: center; justify-content: flex-start; gap: 0.4rem;\">")
                    .Append("<label style=\"margin: 0;\">Drop off Location:</label>")
                    .Append("<a href=\"#\" ")
                        .Append("style=\"display:inline-flex; align-items:center; justify-content:center; padding:0; margin:0; text-decoration:none; color:inherit; transition: color 0.3s, transform 0.2s;\" ")
                        .Append("onmouseover=\"this.style.color='var(--color-primary)'; this.style.transform='scale(1.15)';\" ")
                        .Append("onmouseout=\"this.style.color='inherit'; this.style.transform='scale(1)';\">")
                        .Append("<i class=\"fa-solid fa-home\"></i>")
                    .Append("</a>")
                .Append("</div>")
                .Append("<input type=\"text\" name=\"PDmodaldropoff\" class=\"address-edit-input\" placeholder=\"Type an address or business name\" required />")
            .Append("</div>");

            sb.Append("<label id=\"PDmodallabelPrice\" style=\"display: none;\">Price: </label>")
                    .Append("<div style=\"display: flex; justify-content: space-between;\">")
                        .Append("<div style=\"text-align: left;\">")
                            .Append("<button id=\"PDmodalPrice\" type=\"button\" class=\"btn btn-primary\" onclick=\"GetPDPDPrice()\">Get Price</button>")
                        .Append("</div>")
                        .Append("<div style=\"text-align: right;\">")
                            .Append("<button id=\"PDmodalCancel\" type=\"button\" class=\"btn btn-secondary\" onclick=\"closePDModal();\">Cancel</button>")
                        .Append("</div>")
                  .Append("</div>");

            sb.Append("<div id=\"PDmodalfooter\" class=\"custom-modal-footer\"><span>&nbsp;</span></div>")
              .Append("</div>") // Close Body
        .Append("</div>") // Close Content
        .Append("</div>"); // Close Modal

            return sb.ToString();
        }


        public static string BuildNewCard(long id, bool isDefault, string label, string street, string placeLine)
        {
            StringBuilder sb = new();

            sb.Append($"<div id=\"addressmodaladdresscard_{id}\" class=\"card address-card\" style=\"display:flex; justify-content:space-between; align-items:center; padding:0.5rem 1rem; display:block;\">");

            // Address Content Start
            sb.Append($"<div id=\"addressmodaladdresscontent_{id}\" class=\"address-content\" style=\"flex:1;\">");

            // Address Option Row addressmodaladdresactionsedit
            sb.Append($"<div id=\"addressmodaladdressoptionrow_{id}\" class=\"address-option-row\" style=\"display:flex; align-items:center; gap:0.5rem;\">");
            sb.Append($"<input id=\"addressmodaladdressradio_{id}\" class=\"address-radio\" name=\"selectedAddress\" onclick=\"UpdateDefaultAddress(this)\" type=\"radio\" value=\"{id}\" ");
            if (isDefault)
            {
                sb.Append(" checked");
            }
            sb.Append(" />");

            sb.Append($"<label id=\"addressmodaladdresslabel_{id}\" class=\"address-label\" for=\"addressmodaladdressradio_{id}\"><b>{label}</b></label>");
            sb.Append($"<input id=\"addressmodaladdressinput_{id}\" type=\"text\" class=\"address-edit-input\" value=\"{label}\" style=\"display:none; flex:1;\" />");
            sb.Append("</div>");

            // Address Line Start
            sb.Append($"<div id=\"addressmodaladdress_{id}\" class=\"address-line\">{street},&nbsp;{placeLine}</div>");

            // Address Content End
            sb.Append("</div>");

            // Address Actions Start
            sb.Append($"<div id=\"addressmodaladdressactions_{id}\" class=\"address-actions\" style=\"margin-left:1rem; display:flex; flex-direction:column; gap:0.25rem;\">");
            sb.Append($"<button id=\"addressmodaladdressactionsedit_{id}\" type=\"button\" class=\"address-edit\" onclick=\"startEditAddress(this);\"><i class=\"fa-solid fa-pen\"></i></button>");

            sb.Append($"<button id=\"addressmodaladdressactionsdelete_{id}\" class=\"address-delete\" onclick=\"DeleteAddressCard(this)\" ");
            if (isDefault)
                sb.Append("style=\"display:none;\">");
            else
                sb.Append("style=\"display:block;\">");
            sb.Append("<i class=\"fa-solid fa-trash\"></i>").Append("</button>");
            sb.Append("</div>");

            // Address Actions Edit Start.
            sb.Append($"<div id=\"addressmodaladdressedit_{id}\" class=\"address-actions-edit\" style=\"display:none;\">");
            sb.Append($"<button id=\"addressmodaladdresscancel_{id}\" class=\"address-cancel\" onclick=\"cancelEditAddress(this);\"><i class=\"fa-solid fa-xmark\"></i></button>");
            sb.Append($"<button id=\"addressmodaladdresssave_{id}\" class=\"address-save\" onclick=\"saveEditedAddress(this.closest('.address-card'), this.closest('.address-card').querySelector('.address-edit-input').value)\"><i class=\"fa-solid fa-check\"></i></button>");
            sb.Append("</div>");

            // Address Line Finish
            sb.Append("</div>");

            sb.Append("</div>");
            //sb.Append("<span>&nbsp;</span>");

            return sb.ToString();
        }

        private static SelectList GetAddressTypeOptions(Helper.Connection LocationConnection)
        {
            var (list, error) = Helper.Table.AddressType.GetAddressTypeOptions(LocationConnection); // Load the address types for the dropdown
            SelectList? SelectOption = list;
            if (!string.IsNullOrEmpty(error))
            {
                SelectOption = new("");
            }
            return SelectOption;
        }

    }
}
