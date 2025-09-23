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
                Microsoft.Data.Sqlite.SqliteParameter[] EmailPhoneParams;
                if (isEmail)
                    EmailPhoneParams = [new Microsoft.Data.Sqlite.SqliteParameter("@Email", identifier.ToLowerInvariant())];
                else
                    EmailPhoneParams = [new Microsoft.Data.Sqlite.SqliteParameter("@Phone", identifier)];

                string sql = isEmail
                    ? "SELECT ID, PASSWORDHASH FROM USER WHERE EMAIL = @Email LIMIT 1;"
                    : "SELECT ID, PASSWORDHASH FROM USER WHERE PHONE = @Phone LIMIT 1;";

                // run query with parameter
                using App.Helper.Connection connection = App.Database.Shared.Connection(App.Database.Schema.Entities.Database);
                DataTable DT = connection.GetDataTable(sql, EmailPhoneParams);

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

                // OPTIONAL: pull canonical email/phone/display
                //string email = dt.Columns.Contains("EMAIL") ? (dt.Rows[0]["EMAIL"]?.ToString() ?? "") : "";
                //string phone = dt.Columns.Contains("PHONE") ? (dt.Rows[0]["PHONE"]?.ToString() ?? "") : "";
                //string name = dt.Columns.Contains("DISPLAYNAME") ? (dt.Rows[0]["DISPLAYNAME"]?.ToString() ?? "") : (email != "" ? email : phone);

                // REDIRECT BACK (using Session["redirect"] captured by OnRedirectToLogin)
                string? target = HttpContext.Session.GetString("redirect");
                if (!string.IsNullOrEmpty(target) && target.StartsWith('/') && !target.StartsWith("//"))
                {
                    HttpContext.Session.Remove("redirect");
                    return LocalRedirect(target);
                }

                // Fallback
                return RedirectToPage("/User/Dashboard/Index");
            }
            catch
            {
                ErrorMessage = "An error occurred during login.";
                return Page();
            }
        }
    }
}