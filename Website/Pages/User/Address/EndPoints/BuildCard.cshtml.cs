using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Security;

namespace Website.Pages.User.Address.EndPoints
{
    [IgnoreAntiforgeryToken]
    public class BuildCardModel : PageModel
    {
        public class Input
        {
            public long AddressId { get; set; } = 0;
            public bool SetDefault { get; set; } = false;
        }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] Input input)
        {
            if (!User.IsAuthorised())
                return Unauthorized();

            if (input is null)
                return BadRequest(new { ok = false, msg = "Invalid parameters." });

            if (input.AddressId <= 0)
                return BadRequest(new { ok = false, msg = "Invalid address." });

            var (newCardHtml, label) = BuildCard(input.AddressId, input.SetDefault, User.Id());

            if (string.IsNullOrWhiteSpace(newCardHtml))
                return BadRequest(new { ok = false, msg = "Address not found." });

            return new JsonResult(new
            {
                ok = true,
                Addresslabel = label,
                newcard = newCardHtml
            });
        }

        /// <summary>
        /// Builds the HTML for an address card and returns it along with the label.
        /// </summary>
        /// <param name="addressId">The address ID</param>
        /// <param name="setDefault">Whether to mark the card as default</param>
        /// <param name="userId">The user ID</param>
        /// <returns>Tuple of (HTML string, label)</returns>
        public (string cardHtml, string label) BuildCard(long addressId, bool setDefault, int userId)
        {
            using App.Helper.Connection entitiesConnection = App.Database.Shared.Connection(App.Database.Schema.Entities.Database);
            using App.Helper.Connection locationsConnection = App.Database.Shared.Connection(App.Database.Schema.Locations.Database);
            using DataTable DT = App.Database.Views.UserAddress.DataTable(entitiesConnection, locationsConnection, userId);

            System.Diagnostics.Debug.WriteLine("Columns: " + string.Join(", ", DT.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
            foreach (DataRow r in DT.Rows)
            {
                System.Diagnostics.Debug.WriteLine($"Row: ID={r["ID"]}, ADDRESS_ID={r["ADDRESS_ID"]}, LABEL={r["LABEL"]}");
            }

            DataRow[] DR = DT.Select($"ID={addressId}");
            if (DR.Length == 0)
                return (string.Empty, string.Empty);

            string label = DR[0]["LABEL"]?.ToString() ?? string.Empty;
            string streetNo = DR[0]["STREET_NUMBER"]?.ToString() ?? string.Empty;
            string streetName = DR[0]["STREET_NAME"]?.ToString() ?? string.Empty;
            string street = $"{streetNo} {streetName}".Trim();
            if (string.IsNullOrWhiteSpace(label))
                label = street;

            string locality = DR[0]["LOCALITY_NAME"]?.ToString() ?? string.Empty;
            string place = DR[0]["PLACE_NAME"]?.ToString() ?? string.Empty;
            string placeLine = string.IsNullOrWhiteSpace(locality) ? place : $"{locality}, {place}";

            string cardHtml = App.HTML.Pages.Dashboard.Index.BuildNewCard(addressId, setDefault, label, street, placeLine);

            return (cardHtml, label);
        }
    }
}