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
            public long UserAddressId { get; set; } = 0;
            public bool SetDefault { get; set; } = false;
            public string ModalId { get; set; } = string.Empty;
        }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] Input input)
        {
            if (!User.IsAuthorised())
                return Unauthorized();

            if (input is null)
                return BadRequest(new { ok = false, msg = "Invalid parameters." });

            if (input.UserAddressId <= 0)
                return BadRequest(new { ok = false, msg = "Invalid address." });

            var (newCardHtml, label) = BuildCard(input.ModalId, input.UserAddressId.ToString(), input.SetDefault);

            if (string.IsNullOrWhiteSpace(newCardHtml))
                return BadRequest(new { ok = false, msg = "Address not found." });

            return new JsonResult(new
            {
                ok = true,
                Addresslabel = label,
                newcard = newCardHtml
            });
        }

        public (string cardHtml, string label) BuildCard(string modalid, string userAddressId, bool setDefault)
        {
            using DataTable dt = App.Database.DataAccessManager.GetDataTable(App.Database.Schema.Entities.Database, App.Database.Schema.Entities.Tables.UserAddress, $"ID={userAddressId}");
            if (dt.Rows.Count == 0)
                return (string.Empty, string.Empty);

            string label = dt.Rows[0]["LABEL"]?.ToString() ?? string.Empty;
            string addressId = dt.Rows[0]["ADDRESS_ID"]?.ToString() ?? string.Empty;
            string cardHtml = App.HTML.Pages.Home.IndexPartials.AddressModelAddressCard.Get(modalid, addressId, userAddressId, setDefault, label);

            return (cardHtml, label);
        }
    }
}