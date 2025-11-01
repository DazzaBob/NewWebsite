using System.Data;
using System.Text;

namespace Website.App.StringBuilders.Pages.Index.Modals.Address
{
    public static class AddressCard
    {
        public static string Get(string modalId, string addressId, string userAddressId, bool IsDefault, string label)
        {
            using DataTable DT = App.Database.Views.DataTables.UserAddress.DataTable(userId: 0, addressId: long.Parse(addressId));
            if (DT == null) { return string.Empty; }
            if (DT.Rows.Count == 0) { return string.Empty; }

            var street = $"{DT.Rows[0]["STREET_NUMBER"]} {DT.Rows[0]["STREET_NAME"]}".Trim();
            var placeLine = string.IsNullOrWhiteSpace(DT.Rows[0]["LOCALITY_NAME"]?.ToString())
                ? DT.Rows[0]["PLACE_NAME"]?.ToString()
                : $"{DT.Rows[0]["LOCALITY_NAME"]}, {DT.Rows[0]["PLACE_NAME"]}";

            StringBuilder sb = new();
            sb.Clear();
            sb.Append("<div class=\"card address-card\">")
              .Append("<table style=\"width: 100%; padding: 0; border-collapse: collapse;\">") //  border=\"1\"
              .Append("<tr>")
              .Append("<td style=\"width:100%; padding: 0;\">")
              .Append("<div class=\"address-line\" style=\"display: flex;\">")
              .Append($"<input type=\"radio\" class=\"address-radio\" name=\"selectedAddress_{modalId}\" value=\"{userAddressId}\" ");
            if (IsDefault) sb.Append("checked ");
            sb.Append("onclick=\"UpdateDefaultAddress(this);\" />&nbsp;")
            .Append($"<input type=\"text\" class=\"address-edit-input\" placeholder=\"{label}\" value=\"{label}\" style=\"display:none;\" maxlength=\"25\" />")
            .Append($"<label class=\"address-label\">&nbsp;{label}</label>")
            .Append("</div>")
            .Append("</td>")
            .Append("<td rowspan=\"2\">")
            .Append("<div class=\"address-actions\">")
            .Append("<button type=\"button\" class=\"btn btn-outline\" onclick=\"StartEditAddress(this);\">")
            .Append("<i class=\"fa-solid fa-pen\"></i>")
            .Append("</button>")
            .Append("<button type=\"button\" class=\"btn btn-outline address-delete\" onclick=\"DeleteAddressCard(this);\" style=\"display:");
            if (IsDefault)
            {
                sb.Append("none");
            }
            else
            {
                sb.Append("block");
            }
            sb.Append(";\">")
            .Append("<i class=\"fa-solid fa-trash\"></i>")
            .Append("</button>")
            .Append("</div>")
            .Append("<div class=\"address-actions-edit\" style=\"display:none;\">")
            .Append("<button type=\"button\" class=\"btn btn-outline\" onclick=\"CancelEditAddress(this);\">")
            .Append("<i class=\"fa-solid fa-xmark\"></i>")
            .Append("</button>")
            .Append("<button type=\"button\" class=\"btn btn-outline\" onclick=\"SaveEditedAddress(this.closest('.address-card'), this.closest('.address-card').querySelector('.address-edit-input').value);\">")
            .Append("<i class=\"fa-solid fa-check\"></i>")
            .Append("</button>")
            .Append("</div>")
            .Append("</td>")
            .Append("</tr>")
            .Append("<tr><td>").Append(street).Append(", ").Append(placeLine).Append("</td></tr>")
            .Append("</table>")
            .AppendLine("</div>");

            return sb.ToString();
        }
    }
}
