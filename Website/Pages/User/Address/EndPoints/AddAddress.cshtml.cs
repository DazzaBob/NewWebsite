using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Text;
using Website.App.Security;

namespace Website.Pages.User.Address.EndPoints
{
    [IgnoreAntiforgeryToken]
    public class AddAddressModel : PageModel
    {
        public class SetDefaultInput
        {
            public string Label { get; set; } = string.Empty;
            public string AddressJSON { get; set; } = string.Empty;
            public int TypeID { get; set; }
        }
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] SetDefaultInput input)
        {
            if (!User.IsAuthorised()) return Unauthorized();
            if (input is null) return BadRequest(new { ok = false, msg = "Invalid payload." });
            if (string.IsNullOrWhiteSpace(input.Label)) return BadRequest(new { ok = false, msg = "Label required." });
            if (string.IsNullOrWhiteSpace(input.AddressJSON)) return BadRequest(new { ok = false, msg = "Address JSON required." });
            if (input.TypeID <= 0) return BadRequest(new { ok = false, msg = "Invalid address type." });

            long userAddressId;
            long addressId;
            int userId = User.Id();
            string label = App.Database.Shared.SafeHtml(input.Label); // we dont save to the database here, just return it.

            using App.Helper.Connection entitiesConnection = App.Database.Shared.Connection(App.Database.Schema.Entities.Database);
            using App.Helper.Connection locationsConnection = App.Database.Shared.Connection(App.Database.Schema.Locations.Database);

            try
            {  // We assume this is an address card. We may just want to add; so dont set a default here.
                userAddressId = App.Validation.AddressValidation.SaveAddressAndGetId(input.AddressJSON, locationsConnection, entitiesConnection, input.TypeID, userId, false, label);
                using DataTable dt = App.Database.Shared.GetDataTable(entitiesConnection, App.Database.Schema.Entities.Tables.UserAddress, $"ID={userAddressId} AND USER_ID={userId}");
                addressId = dt.Rows.Count > 0 ? (long)dt.Rows[0]["ADDRESS_ID"] : -1;
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                App.Bootstrap.Logger?.Add(errorMessage, App.Helper.Logger.LogLevel.Error);
                return StatusCode(StatusCodes.Status422UnprocessableEntity, new { ok = false, message = "Address not found" });
            }

            using DataTable DT = App.Database.Views.UserAddress.DataTable(entitiesConnection, locationsConnection, userId);
            DataRow[] DR = DT.Select($"ADDRESS_ID={addressId}");
            string streetNo = DR[0]["STREET_NUMBER"]?.ToString() ?? string.Empty;
            string streetName = DR[0]["STREET_NAME"]?.ToString() ?? string.Empty;
            string street = $"{streetNo} {streetName}".Trim();

            string locality = DR[0]["LOCALITY_NAME"]?.ToString() ?? string.Empty;
            string place = DR[0]["PLACE_NAME"]?.ToString() ?? string.Empty;
            string placeLine = string.IsNullOrWhiteSpace(locality) ? place : $"{locality}, {place}";

            return new JsonResult(new { ok = true, Addresslabel = label, newcard = BuildNewCard(userAddressId, false, label, street, placeLine) });
        }
        private static string BuildNewCard(long id, bool isDefault, string label, string street, string placeLine)
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
    }
}
