using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website.App.Security;

namespace Website.Pages.User
{
    public class LogoutModel : PageModel
    {
        public async Task<IActionResult> OnGet()
        {
            User.SignOut(HttpContext);
            Response.Cookies.Delete(".AspNetCore.Antiforgery", new CookieOptions { Path = "/" }); // optional
            return RedirectToPage("/User/Login");
        }
    }
}
