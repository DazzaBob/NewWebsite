using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Data;
using Website.App.Security;

namespace Website.Pages.User
{
    public class LoginModel : PageModel
    {
        private const string ErrorText = "Invalid email/phone or password.";
        [Required][BindProperty] public string EmailOrPhone { get; set; } = string.Empty;
        [Required][BindProperty] public string Password { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet() { }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            try
            {
                string identifier = (EmailOrPhone ?? string.Empty).Trim();
                bool isEmail = identifier.Contains('@');

                // sanity-check phone before querying
                if (!isEmail && !App.Helper.Shared.IsValidPhone(identifier))
                {
                    ErrorMessage = "Invalid email/phone format.";
                    return Page();
                }

                // build SQL
                string WhereClause;
                if (isEmail)
                    WhereClause = $"(EMAIL={App.Database.Shared.Sanitize(identifier.ToLowerInvariant(), true, true)})";
                else
                    WhereClause = $"(PHONE={App.Database.Shared.Sanitize(identifier.ToLowerInvariant(), true, true)})";

                using DataTable DT = App.Database.DataAccessManager.GetDataTable(App.Database.Schema.Entities.SchemaName, App.Database.Schema.Entities.Users, WhereClause);
                if (DT.Rows.Count == 0)
                {
                    ErrorMessage = ErrorText;
                    return Page();
                }

                string storedHash = DT.Rows[0]["PASSWORDHASH"]?.ToString() ?? string.Empty;
                if (!App.Helper.Shared.VerifyPassword(Password, storedHash))
                {
                    ErrorMessage = ErrorText;
                    return Page();
                }

                int userId = Convert.ToInt32(DT.Rows[0]["ID"]);
                if (userId <= 0)
                {
                    ErrorMessage = ErrorText;
                    return Page();
                }
                HttpContext.SignInUser(Convert.ToInt32(DT.Rows[0]["ID"]));

                // REDIRECT BACK (using Session["redirect"] captured by OnRedirectToLogin)
                string? target = HttpContext.Session.GetString("redirect");
                if (!string.IsNullOrEmpty(target) && target.StartsWith('/') && !target.StartsWith("//"))
                {
                    HttpContext.Session.Remove("redirect");
                    return LocalRedirect(target);
                }

                // Fallback
                return RedirectToPage("/Home/Index");
            }
            catch
            {
                ErrorMessage = "An error occurred during login.";
                return Page();
            }
        }
    }
}