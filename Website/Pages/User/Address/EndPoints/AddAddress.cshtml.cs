using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Security;

namespace Website.Pages.User.Address.EndPoints
{
    [IgnoreAntiforgeryToken]
    public class AddAddressModel : PageModel
    {
        public class SetDefaultInput
        {
            public string Label { get; set; } = string.Empty;
            public string AddressJSON { get; set; } = string.Empty;
            public int TypeID { get; set; }
        }
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] SetDefaultInput input)
        {
            if (!User.IsAuthorised()) return Unauthorized();
            if (input is null) return BadRequest(new { ok = false, msg = "Invalid payload." });
            if (string.IsNullOrWhiteSpace(input.Label)) return BadRequest(new { ok = false, msg = "Label required." });
            if (string.IsNullOrWhiteSpace(input.AddressJSON)) return BadRequest(new { ok = false, msg = "Address JSON required." });
            if (input.TypeID <= 0) return BadRequest(new { ok = false, msg = "Invalid address type." });

            long userAddressId;
            long addressId;
            int userId = User.Id();
            string label = App.Database.Shared.SafeHtml(input.Label); // we dont save to the database here, just return it.

            using App.Helper.Connection entitiesConnection = App.Database.Shared.Connection(App.Database.Schema.Entities.Database);
            using App.Helper.Connection locationsConnection = App.Database.Shared.Connection(App.Database.Schema.Locations.Database);

            try
            {  // We assume this is an address card. We may just want to add; so dont set a default here.
                userAddressId = App.Validation.AddressValidation.SaveAddressAndGetId(input.AddressJSON, locationsConnection, entitiesConnection, input.TypeID, userId, false, label);
                using DataTable dt = App.Database.Shared.GetDataTable(entitiesConnection, App.Database.Schema.Entities.Tables.UserAddress, $"ID={userAddressId} AND USER_ID={userId}");
                addressId = dt.Rows.Count > 0 ? (long)dt.Rows[0]["ADDRESS_ID"] : -1;
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                App.Bootstrap.Logger?.Add(errorMessage, App.Helper.Logger.LogLevel.Error);
                return StatusCode(StatusCodes.Status422UnprocessableEntity, new { ok = false, message = "Address not found" });
            }

            var buildCard = new Website.Pages.User.Address.EndPoints.BuildCardModel();
            var (cardLabel, cardHtml) = buildCard.BuildCard(userAddressId, false, User.Id());

            return new JsonResult(new { ok = true, Addresslabel = cardLabel, newcard = cardHtml });
        }
    }
}
