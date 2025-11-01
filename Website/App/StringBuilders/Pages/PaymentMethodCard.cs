using System.Net;
using System.Text;

namespace Website.App.StringBuilders.Pages
{
    public static class PaymentMethodCard
    {
        public static string Get(
            string modalId,
            long paymentId,
            string displayName,
            string providerName,
            string? brand,
            string? last4,
            bool isDefault)
        {
            var sb = new StringBuilder();

            // Encode user-visible values
            displayName = WebUtility.HtmlEncode(displayName ?? "Payment Method");
            providerName = WebUtility.HtmlEncode(providerName ?? "Provider");
            brand = WebUtility.HtmlEncode(brand ?? string.Empty);
            last4 = WebUtility.HtmlEncode(last4 ?? string.Empty);

            sb.Append("<div class=\"card payment-card\">")
              .Append("<table style=\"width:100%; padding:0; border-collapse:collapse;\">");

            // Header row
            sb.Append("<tr>")
              .Append("<td style=\"width:100%; padding:0;\">")
              .Append("<div class=\"payment-line\" style=\"display:flex; align-items:center;\">")
              .Append($"<input type=\"radio\" class=\"payment-radio\" name=\"selectedPayment_{modalId}\" value=\"{paymentId}\" ");
            if (isDefault) sb.Append("checked ");
            sb.Append("onclick=\"PMSD(this);\" />&nbsp;")
              .Append($"<input type=\"text\" class=\"payment-edit-input\" placeholder=\"{displayName}\" value=\"{displayName}\" style=\"display:none;\" maxlength=\"40\" />")
              .Append($"<label class=\"payment-label\">&nbsp;{displayName}</label>")
              .Append("</div>")
              .Append("</td>");

            // Actions
            sb.Append("<td rowspan=\"2\" style=\"white-space:nowrap; text-align:right;\">")
              .Append("<div class=\"payment-actions\">")
              .Append("<button type=\"button\" class=\"btn btn-outline\" onclick=\"PMSEPC(this);\"><i class=\"fa-solid fa-pen\"></i></button>")
              .Append("<button type=\"button\" class=\"btn btn-outline payment-delete\" onclick=\"PMDPC(this);\" style=\"display:")
              .Append(isDefault ? "none" : "block")
              .Append(";\">")
              .Append("<i class=\"fa-solid fa-trash\"></i></button>")
              .Append("</div>")
              .Append("<div class=\"payment-actions-edit\" style=\"display:none;\">")
              .Append("<button type=\"button\" class=\"btn btn-outline\" onclick=\"PMCEC(this);\"><i class=\"fa-solid fa-xmark\"></i></button>")
              .Append("<button type=\"button\" class=\"btn btn-outline\" onclick=\"PMSEV(this);\"><i class=\"fa-solid fa-check\"></i></button>")
              .Append("</div>")
              .Append("</td>")
              .Append("</tr>");

            // Detail row (built only from known fields)
            sb.Append("<tr><td>");
            sb.Append(providerName);
            if (!string.IsNullOrWhiteSpace(brand))
                sb.Append($" {brand}");
            if (!string.IsNullOrWhiteSpace(last4))
                sb.Append($" ••••{last4}");
            sb.Append("</td></tr>");

            sb.Append("</table>")
              .Append("</div>");

            return sb.ToString();
        }
    }
}