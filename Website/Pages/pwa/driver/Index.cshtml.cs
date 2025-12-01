using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website.App.Security;

namespace Website.Pages.pwa.driver
{
    public class IndexModel : PageModel
    {
        // Later you can expose strongly-typed properties (current offer, driver state, etc.)
        public IActionResult OnGet()
        {
            if (!User.IsAuthorised())
                return Unauthorized();

            return Page();
        }
    }
}
