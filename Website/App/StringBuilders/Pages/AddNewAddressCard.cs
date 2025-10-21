using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;

namespace Website.App.StringBuilders.Pages
{
    public static class AddNewAddressCard
    {
        public static string GenerateAddressCard(string cardId, string labelInputId, string searchInputId, string listElementId, string hiddenJsonId, string typeContainerId, string typeSelectId)
        {
            StringBuilder sb = new();

            sb.Append($"<div class=\"card address-card new-address-card\" id=\"{cardId}\" style=\"display:none;\">")
              .Append("<div class=\"add-address-form\">")
              .Append("<div class=\"address-option-row\"><label><b>New Address</b></label></div><br />")

              // Label input
              .Append("<div class=\"form-group mt-2\">")
              .Append($"<label><b>Label</b></label>")
              .Append($"<input id=\"{labelInputId}\" type=\"text\" class=\"form-control new-address-label\" placeholder=\"Home, Work, etc.\" maxlength=\"25\" />")
              .Append("</div><br />")

              // Mapbox input
              .Append("<div class=\"mapbox-address-autofill\" style=\"position:relative;\">")
              .Append("<label>Search Address</label>")
              .Append($"<input id=\"{searchInputId}\" type=\"text\" class=\"input-text\" autocomplete=\"off\" placeholder=\"Start typing your address…\" required />")
              .Append($"<ul id=\"{listElementId}\" hidden></ul>")
              .Append($"<input id=\"{hiddenJsonId}\" type=\"hidden\" class=\"location-payload\" />")
              .Append("</div><br />")

              // Address type container
              .Append($"<div class=\"form-group mt-2 address-type-container\" id=\"{typeContainerId}\" style=\"display:none;\">")
              .Append("<label><b>Address Type</b></label>")
              .Append($"<select id=\"{typeSelectId}\" class=\"input-text\" required>")
              .Append("<option value=\"\">--Select address type--</option>");

            foreach (SelectListItem item in App.Helper.Table.AddressType.AddressTypeOptions())
            {
                sb.Append($"<option value=\"{item.Value}\">{item.Text}</option>");
            }

            sb.Append("</select></div><br />")

              // Buttons
              .Append("<div class=\"mt-3\">")
              .Append("<table style=\"width: 100%; padding: 1 1 1 1; border-collapse: collapse;\">")
              .Append("<tr>")
              .Append("<td style=\"width: 50%; text-align: right;\">")
              .Append("<button type=\"button\" class=\"btn btn-primary\" onclick=\"SaveNewAddress(this.closest('.new-address-card'))\">Save</button>&nbsp;")
              .Append("</td>")
              .Append("<td style=\"text-align: left;\">")
              .Append("&nbsp;<button type=\"button\" class=\"btn btn-secondary\" onclick=\"CloseNewAddressCard();\">Cancel</button>")
              .Append("</td>")
              .Append("</tr></table></div>")

              .Append("</div></div>");

            return sb.ToString();
        }
    }
}
