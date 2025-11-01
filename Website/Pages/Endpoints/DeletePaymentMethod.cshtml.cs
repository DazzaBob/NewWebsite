using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using System.Text.Json;
using Website.App.Database;
using Website.App.Security;

namespace Website.Pages.Endpoints
{
    [IgnoreAntiforgeryToken]
    public class DeletePaymentMethodModel : PageModel
    {
        public class DeletePaymentInput
        {
            public long PaymentId { get; set; }
        }
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] DeletePaymentInput input)
        {
            if (!User.IsAuthorised()) return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            if (input == null || input.PaymentId <= 0) return BadRequest(new { ok = false, msg = "Invalid request payload." });
            try
            {
               _ = DataAccessManager.Update(Schema.Entities.Name, Schema.Entities.Tables.UserPayment, "is_active=false", $"id={input.PaymentId} AND user_id={User.Id()}");
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                App.Bootstrap.Logger?.Add("DeletePaymentMethod: " + ex.Message, App.Helper.Logger.LogLevel.Error);
                return new JsonResult(new { ok = false, msg = "Internal server error." });
            }
        }
    }
}
