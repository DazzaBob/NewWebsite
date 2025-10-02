using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Text.Json;
using Website.App.Security;

namespace Website.Pages.User.Address.EndPoints
{
    [IgnoreAntiforgeryToken]
    public class AddFromLocationModel(IHttpContextAccessor httpContextAccessor) : PageModel
    {
        private const string NamespaceClass = "Website.Pages.User.Address.AddFromLocationModel.";
        private readonly IHttpContextAccessor? HttpContextAccessor = httpContextAccessor;
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public sealed class Req
        {
            public int AddressTypeID { get; set; }
            public string JSonpayload { get; set; } = "";
        }

        [BindProperty]
        public Req Input { get; set; } = new();
        public IActionResult OnGet() => NotFound();
        public async Task<IActionResult> OnPostAsync()
        {
            if (!User.IsAuthorised()) { return Unauthorized(); }

            if (User.Id() <= 0)
                return new UnauthorizedObjectResult(new { ok = false, message = "User not authenticated" });

            if (Request.Body == null || Request.ContentLength == 0)
                return BadRequest(new { ok = false, message = "Empty body" });

            int userId = User.Id();

            // Shallow-parse for fields we need (type + payload). userId is NOT trusted from client.
            Req? req;
            using App.Helper.Connection connection = App.Database.Shared.Connection(App.Database.Schema.Entities.Database);
            using App.Helper.Connection LocationConnection = App.Database.Shared.Connection(App.Database.Schema.Locations.Database);

            try
            {
                req = await Request.ReadFromJsonAsync<Req>(JsonOpts);
            }
            catch
            {
                return BadRequest(new { ok = false, message = "Malformed JSON" });
            }
            if (req is null)
                return BadRequest(new { ok = false, message = "Invalid body" });

            if (req.AddressTypeID < 1 || req.AddressTypeID > 7)
                return BadRequest(new { ok = false, message = "Invalid address type" });

            // Save using your existing pipeline
            long newId;
            string label;
            try
            {
                newId = App.Validation.AddressValidation.SaveAddressAndGetId(req.JSonpayload, LocationConnection, connection, Input.AddressTypeID, userId, true);
                DataTable dt = App.Database.Shared.GetDataTable(connection, "USER_ADDRESS", $"USER_ID = {userId} AND ISDEFAULT=1");
                if (dt.Rows.Count == 0) return NotFound(new { ok = false, msg = "Address not found." });

                label = dt.Rows[0]["LABEL"] == DBNull.Value ? "Select Address" : dt.Rows[0]["LABEL"].ToString()?.Trim() ?? "Select Address";
            }
            catch (Exception ex)
            {
                string errorMessage = NamespaceClass + ex.Message;
                App.Bootstrap.Logger?.Add(errorMessage, App.Helper.Logger.LogLevel.Error);
                return StatusCode(StatusCodes.Status422UnprocessableEntity, new { ok = false, message = "Address not found" });
            }

            return new JsonResult(new { ok = true, addressId = newId, label });
        }
    }
}