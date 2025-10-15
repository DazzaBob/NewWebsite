using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website.App.Security;

namespace Website.Pages.User.Address.EndPoints
{
    [IgnoreAntiforgeryToken]
    public class AddAddressModel : PageModel
    {
        public class SetDefaultInput
        {
            public string ModalId { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public string AddressJSON { get; set; } = string.Empty;
            public int TypeID { get; set; }
        }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] SetDefaultInput input)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };

            if (input is null)
                return BadRequest(new { ok = false, msg = "Invalid payload." });

            if (string.IsNullOrWhiteSpace(input.Label))
                return BadRequest(new { ok = false, msg = "Label required." });

            if (string.IsNullOrWhiteSpace(input.AddressJSON))
                return BadRequest(new { ok = false, msg = "Address JSON required." });

            if (input.TypeID <= 0)
                return BadRequest(new { ok = false, msg = "Invalid address type." });

            if (string.IsNullOrWhiteSpace(input.ModalId))
                return BadRequest(new { ok = false, msg = "Input not from Address." });

            long userAddressId;
            int userId = User.Id();
            string label = App.Database.Shared.Sanitize(input.Label, forHtml: true);

            try
            {
                userAddressId = App.Validation.AddressValidation.SaveAddressAndGetId(input.AddressJSON, input.TypeID, userId, false, label);
            }
            catch (Exception ex)
            {
                App.Bootstrap.Logger?.Add(ex.Message, App.Helper.Logger.LogLevel.Error);
                return StatusCode(StatusCodes.Status422UnprocessableEntity, new { ok = false, msg = "Address not found" });
            }

            try
            {
                BuildCardModel buildCard = new();
                // The tuple expected the HTML, LABEL in this order, they are not by name, rather sequence.
                var (cardHtml, cardLabel) = buildCard.BuildCard(input.ModalId, userAddressId.ToString(), false);
                return new JsonResult(new { ok = true, Addresslabel = cardLabel, newcard = cardHtml });
            }
            catch (Exception ex)
            {
                App.Bootstrap.Logger?.Add(ex.Message, App.Helper.Logger.LogLevel.Error);
                return StatusCode(StatusCodes.Status500InternalServerError, new { ok = false, msg = "Failed to build address card" });
            }
        }
    }
}
