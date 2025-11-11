using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using System.Data;
using Website.App.Database;
using Website.App.Security;
using Website.App.StringBuilders.Pages;

namespace Website.Pages.homefolder.content.endpoints.paymentmethod
{
    [IgnoreAntiforgeryToken]
    public class AddModel : PageModel
    {
        public class PaymentMethodInput
        {
            public int TypeId { get; set; }
            public int ProviderId { get; set; }
            public string? DisplayName { get; set; }
            public string? Token { get; set; }
            public string? Brand { get; set; }
            public string? Last4 { get; set; }
            public string? Account { get; set; }
            public string? ModalId { get; set; }
        }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] PaymentMethodInput input)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };

            if (input == null || input.TypeId <= 0 || input.ProviderId <= 0 || string.IsNullOrWhiteSpace(input.ModalId))
                return BadRequest(new { ok = false, msg = "Invalid request payload." });
            try
            {
                using var existing = DataAccessManager.GetDataTable(Schema.Entities.Name, Schema.Entities.Tables.UserPayment, $"user_id = {User.Id()} AND provider_id = {input.ProviderId} AND is_active = true");
                if (existing.Rows.Count > 0) { return new JsonResult(new { ok = false, msg = "You already have a payment method of this type. Delete it first before adding another." }); }
            }
            catch
            {
                return BadRequest(new { ok = false, msg = "Invalid request payload." });
            }

            try
            {
                string? tokenHash = !string.IsNullOrWhiteSpace(input.Token) ? App.Helper.Shared.HashPassword(input.Token) : null;
                string? accountHash = !string.IsNullOrWhiteSpace(input.Account) ? App.Helper.Shared.HashPassword(input.Account) : null;

                int countDefault = DataAccessManager.GetDataTable(Schema.Entities.Name, Schema.Entities.Tables.UserPayment, $"user_id = {User.Id()} AND is_active = true").Rows.Count;
                bool makeDefault;
                if (countDefault == 0) { makeDefault = true; } else { makeDefault = false; }
                // Keep columns aligned with your table; rely on defaults for booleans if you prefer
                string fields = "user_id, provider_id, display_name, vault_token, brand, last4, make_default";
                var parameters = new[]
                {
                    new NpgsqlParameter("@UID",   User.Id()),
                    new NpgsqlParameter("@PROV",  input.ProviderId),
                    new NpgsqlParameter("@NAME",  input.DisplayName ?? ""),
                    new NpgsqlParameter("@TOKEN", tokenHash ?? accountHash ?? ""),
                    new NpgsqlParameter("@BRAND", input.Brand ?? ""),
                    new NpgsqlParameter("@LAST4", input.Last4 ?? ""),
                    new NpgsqlParameter("@MAKEDEFAULT", makeDefault)
                };
                long id = DataAccessManager.Insert(Schema.Entities.Name, Schema.Entities.Tables.UserPayment, fields, parameters);

                // Use the modalId from the payload for rendering
                string html = BuildCardHtml(input.ModalId!, id, input, makeDefault);

                return new JsonResult(new { ok = true, id, html });
            }
            catch (Exception ex)
            {
                App.Bootstrap.Logger?.Add("AddPaymentMethod: " + ex.Message, App.Helper.Logger.LogLevel.Error);
                return new JsonResult(new { ok = false, msg = "Internal server error." });
            }
        }
        private static string BuildCardHtml(string modalId, long id, PaymentMethodInput input, bool MakeDefault)
        {
            // Look up provider name from cfg.payment_type_provider
            using DataTable dt = DataAccessManager.GetDataTable(Schema.Config.Name, Schema.Config.Tables.PaymentTypeProvider, $"id = {input.ProviderId}");
            string providerName = dt.Rows.Count > 0 ? Convert.ToString(dt.Rows[0]["name"]) ?? "Provider" : "Provider";

            // Use the shared PaymentMethodCard builder
            return PaymentMethodCard.Get(
                modalId: modalId,
                paymentId: id,
                displayName: input.DisplayName ?? "Payment Method",
                providerName: providerName,
                brand: input.Brand,
                last4: input.Last4,
                isDefault: MakeDefault
            );
        }
    }
}
