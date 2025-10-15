using System.Data;
using System.Text;

namespace Website.App.StringBuilders.Pages.Index.Modals.Address
{
    public static class AddressCard
    {
        public static string Get(string modalId, string addressId, string userAddressId, bool IsDefault, string label)
        {
            StringBuilder sb = new();
            sb.AppendLine("SELECT a.ID AS ADDRESS_ID, a.STREET_NUMBER, a.STREET_NAME, l.NAME AS LOCALITY_NAME, p.NAME AS PLACE_NAME ");
            sb.AppendLine("FROM ADDRESS a LEFT JOIN LOCALITY l ON a.LOCALITY_ID = l.ID LEFT JOIN PLACE p ON a.PLACE_ID = p.ID ");
            sb.AppendLine($"WHERE (a.ID = {addressId})");
            using DataTable DT = Database.DataAccessManager.GetDataTable(Database.Schema.Locations.Database, sb.ToString(), []);
            if (DT == null) { return string.Empty; }
            if (DT.Rows.Count == 0 ) { return string.Empty; }

            var street = $"{DT.Rows[0]["STREET_NUMBER"]} {DT.Rows[0]["STREET_NAME"]}".Trim();
            var placeLine = string.IsNullOrWhiteSpace(DT.Rows[0]["LOCALITY_NAME"]?.ToString())
                ? DT.Rows[0]["PLACE_NAME"]?.ToString()
                : $"{DT.Rows[0]["LOCALITY_NAME"]}, {DT.Rows[0]["PLACE_NAME"]}";

            sb.Clear();
            sb.Append("<div class=\"card address-card\">")
              .Append("<table style=\"width: 100%; padding: 0; border-collapse: collapse;\">")
              .Append("<tr>")
              .Append("<td style=\"width:90%; padding: 0;\">")
              .Append("<table style=\"border: 0; border-collapse: collapse;\">")
              .Append("<tr>")
              .Append("<td>")
              .Append("<input type=\"radio\" class=\"address-radio\" name=\"selectedAddress_").Append(modalId)
              .Append("\" value=\"").Append(userAddressId).Append("\" ");
            if (IsDefault) sb.Append("checked ");
            sb.Append("onclick=\"UpdateDefaultAddress(this);\" />")
            .Append("</td>")
            .Append("<td style=\"width: 90%;\">")
            .Append("<input type=\"text\" class=\"address-edit-input\" value=\"").Append(label)
            .Append("\" style=\"display:none;\" maxlength=\"25\" />")
            .Append("<label class=\"address-label\"><b>&nbsp;").Append(label).Append("</b></label>")
            .Append("</td>")
            .Append("</tr>")
            .Append("</table>")
            .Append("</td>")
            .Append("<td rowspan=\"2\">")
            .Append("<div class=\"address-actions\">")
            .Append("<button class=\"address-edit\" type=\"button\" onclick=\"StartEditAddress(this);\">")
            .Append("<i class=\"fa-solid fa-pen\"></i>")
            .Append("</button>")
            .Append("<button class=\"address-delete\" type=\"button\" onclick=\"DeleteAddressCard(this);\" style=\"display:");
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
            .Append("<button class=\"address-cancel\" type=\"button\" onclick=\"CancelEditAddress(this);\">")
            .Append("<i class=\"fa-solid fa-xmark\"></i>")
            .Append("</button>")
            .Append("<button class=\"address-save\" type=\"button\" onclick=\"SaveEditedAddress(this.closest('.address-card'), this.closest('.address-card').querySelector('.address-edit-input').value);\">")
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
