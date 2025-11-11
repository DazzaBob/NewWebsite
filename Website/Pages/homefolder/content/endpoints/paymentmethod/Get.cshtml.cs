using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Text;
using Website.App.Security;
using Website.App.StringBuilders.Pages;

namespace Website.Pages.homefolder.content.endpoints.paymentmethod
{
    [IgnoreAntiforgeryToken]
    public class GetModel : PageModel
    {
        public class InputPayload
        {
            public string? ModalId { get; set; }
        }
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] InputPayload? input)
        {
            if (!User.IsAuthorised()) return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            try
            {
                using DataTable dt = App.Database.Views.DataTables.UserPaymentMethods.DataTable(User.Id());
                if (dt.Rows.Count == 0) return new JsonResult(new { ok = true, html = "" });

                StringBuilder sb = new();
                foreach (DataRow row in dt.Rows)
                {
                    string modalId = input?.ModalId ?? Guid.NewGuid().ToString("N");
                    sb.Append(
                        PaymentMethodCard.Get(
                            modalId: modalId,
                            paymentId: Convert.ToInt64(row["id"]),
                            displayName: Convert.ToString(row["display_name"]) ?? "Payment Method",
                            providerName: Convert.ToString(row["provider_name"]) ?? "Provider",
                            brand: Convert.ToString(row["brand"]),
                            last4: Convert.ToString(row["last4"]),
                            isDefault: Convert.ToBoolean(row["make_default"])
                        )
                    );
                }

                return new JsonResult(new { ok = true, html = sb.ToString() });
            }
            catch (Exception ex)
            {
                App.Bootstrap.Logger?.Add("GetPaymentMethods: " + ex.Message, App.Helper.Logger.LogLevel.Error);
                return new JsonResult(new { ok = false, msg = "Internal server error." });
            }
        }
    }
}