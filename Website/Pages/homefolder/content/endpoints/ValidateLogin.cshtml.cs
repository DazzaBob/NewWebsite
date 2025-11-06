using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints
{
    [IgnoreAntiforgeryToken]
    public class ValidateLoginModel : PageModel
    {
        public class LoginInput
        {
            public string? EmailOrPhone { get; set; }
            public string? Password { get; set; }
        }
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] LoginInput input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.EmailOrPhone) || string.IsNullOrWhiteSpace(input.Password))
                return new JsonResult(new { ok = false, msg = "Missing credentials." });

            try
            {
                string identifier = input.EmailOrPhone.Trim();
                bool isEmail = identifier.Contains('@');

                if (!isEmail && !App.Helper.Shared.IsValidPhone(identifier))
                    return new JsonResult(new { ok = false, msg = "Invalid email/phone format." });

                string whereClause = isEmail
                    ? $"(EMAIL={App.Database.Shared.Sanitize(identifier.ToLowerInvariant(), true, true)})"
                    : $"(PHONE={App.Database.Shared.Sanitize(identifier.ToLowerInvariant(), true, true)})";

                using DataTable dt = App.Database.DataAccessManager.GetDataTable(
                    App.Database.Schema.Entities.Name,
                    App.Database.Schema.Entities.Tables.Users,
                    whereClause);

                if (dt.Rows.Count == 0)
                    return new JsonResult(new { ok = false, msg = "Account not found." });

                string storedHash = dt.Rows[0]["PASSWORDHASH"]?.ToString() ?? string.Empty;
                if (!App.Helper.Shared.VerifyPassword(input.Password, storedHash))
                    return new JsonResult(new { ok = false, msg = "Incorrect password." });

                // login successful
                HttpContext.Session.SetInt32("UserId", Convert.ToInt32(dt.Rows[0]["ID"]));
                HttpContext.Session.SetInt32("IsAuthorised", 1);
                HttpContext.SignInUser( Convert.ToInt32(dt.Rows[0]["ID"]));

                HttpContext.Session.CommitAsync().GetAwaiter().GetResult();

                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                App.Bootstrap.Logger?.Add("ValidateLogin: " + ex.Message, App.Helper.Logger.LogLevel.Error);
                return new JsonResult(new { ok = false, msg = "Server error during login." });
            }
        }
    }
}
