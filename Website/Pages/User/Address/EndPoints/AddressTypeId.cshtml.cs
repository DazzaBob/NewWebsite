using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Security;

namespace Website.Pages.User.Address.EndPoints
{
    [IgnoreAntiforgeryToken]
    public class AddressTypeIdModel : PageModel
    {
        private const string NamespaceClass = "Website.Pages.User.Address.Enpoints.AddressTypeId.";
        public class Input
        {
            public string Number { get; set; } = string.Empty;
            public string Street { get; set; } = string.Empty;
            public string Postcode { get; set; } = string.Empty;
            public string Origin { get; set; } = string.Empty;
        }
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] Input input)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };

            if (input == null || string.IsNullOrWhiteSpace(input.Number) || string.IsNullOrWhiteSpace(input.Street))
                return BadRequest(new { success = false, message = "Invalid input" });

            string num = App.Database.Shared.Sanitize(input.Number, true);
            string street = App.Database.Shared.Sanitize(input.Street, true);
            string pc = App.Database.Shared.Sanitize(input.Postcode, true);

            using DataTable dt = App.Database.DataAccessManager.GetDataTable(App.Database.Schema.Locations.Name, App.Database.Schema.Locations.Tables.Address, $"STREET_NUMBER = {num} AND STREET_NAME = {street} AND POSTCODE = {pc}");

            if (dt.Rows.Count == 0)
                return new JsonResult(new { success = false, typeId = (int?)null });

            int addressTypeId = (int)dt.Rows[0]["ADDRESS_TYPE_ID"];

            return new JsonResult(new { success = true, typeId = addressTypeId, origin = input.Origin });
        }
    }
}
