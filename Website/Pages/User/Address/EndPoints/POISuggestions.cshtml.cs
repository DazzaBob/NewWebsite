using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Database.Views;
using Website.App.Security;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Website.Pages.User.Address.EndPoints
{
    [IgnoreAntiforgeryToken]
    public class POISuggestionsModel : PageModel
    {
        public IActionResult OnGet() => NotFound();
        private const string NamespaceClass = "Website.Pages.User.Address.Endpoints.POISuggestions.";
        public class Input
        {
            public string query { get; set; } = string.Empty;
        }
        public IActionResult OnPost([FromBody] Input input)
        {
            if (!User.IsAuthorised()) { return Unauthorized(); }
            if (input is null || input.query == string.Empty) return BadRequest(new { ok = false, msg = "Invalid address." });

            using DataTable dt = App.Database.DataAccessManager.GetDataTable(App.Database.Schema.Locations.SchemaName, App.Database.Schema.Locations.Address, $"LABEL IS NOT NULL AND LOWER(LABEL) LIKE {App.Database.Shared.Sanitize("%" + input.query.Trim().ToLowerInvariant() + "%", true, true)} AND ADDRESS_TYPE_ID <> 1");

            var results = dt.AsEnumerable().Select(row => new
            {
                place_name = row.Field<string>("label") + ", " + row.Field<string>("street_number") + " " + row.Field<string>("street_name") + ", " + row.Field<string>("postcode"),
                streetNumber = row.Field<string>("street_number"),
                streetName = row.Field<string>("street_name"),
                postcode = row.Field<string>("postcode")
            }).ToList();

            return new JsonResult(new { ok = true, results });
        }
    }
}
