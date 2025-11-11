using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website.App.Database;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.paymentmethod
{
    [IgnoreAntiforgeryToken]
    public class EditModel : PageModel
    {
        public class EditPaymentInput
        {
            public long PaymentId { get; set; }
            public string? DisplayName { get; set; }
        }
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] EditPaymentInput input)
        {
            if (!User.IsAuthorised()) return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            if (input == null || input.PaymentId <= 0 || string.IsNullOrWhiteSpace(input.DisplayName)) return BadRequest(new { ok = false, msg = "Invalid request payload." });
            try
            {
                _ = DataAccessManager.Update(Schema.Entities.Name, Schema.Entities.Tables.UserPayment, $"display_name={App.Database.Shared.Sanitize(input.DisplayName.Trim(), true, true)}", $"id={input.PaymentId} AND user_id={User.Id()} AND is_active=true");
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                App.Bootstrap.Logger?.Add("EditPaymentMethod: " + ex.Message,
                    App.Helper.Logger.LogLevel.Error);
                return new JsonResult(new { ok = false, msg = "Internal server error." });
            }
        }
    }
}