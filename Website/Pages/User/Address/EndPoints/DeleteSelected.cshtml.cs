using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website.App;
using Website.App.Security;

namespace Website.Pages.User.Address.EndPoints
{
    /// <summary>
    /// Endpoint for deleting a user's selected address in the system.
    /// Ensures that the user is authorized and prevents deletion of the default address.
    /// </summary>
    [IgnoreAntiforgeryToken]
    public class DeleteSelectedModel : PageModel
    {
        private readonly string ADSED = App.Database.Schema.Entities.Name;
        public class SetDefaultInput { public int Id { get; set; } }
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] SetDefaultInput input)
        {
            if (!User.IsAuthorised()) return Unauthorized();
            if (input is null || input.Id <= 0) return BadRequest(new { ok = false, msg = "Invalid address." });

            int userId = User.Id();

            string checkSql = $"SELECT ISDEFAULT FROM {App.Database.Schema.Entities.Tables.UserAddress} WHERE USER_ID={userId} AND ID={input.Id} LIMIT 1";
            object? checkResult = App.Database.DataAccessManager.ExecuteScalar(ADSED, checkSql, []);
            if (checkResult is null) return BadRequest(new { ok = false, msg = "Address not found." });
            bool isDefault = Convert.ToInt32(checkResult) == 1;

            if (isDefault) return new JsonResult(new { ok = false, msg = "Cannot delete default address." });
            try
            {
                string sql = $"DELETE FROM {App.Database.Schema.Entities.Tables.UserAddress} WHERE USER_ID={userId} AND ID={input.Id}";
                long affected = App.Database.DataAccessManager.ExecuteNonQuery(ADSED, sql, []);
                if (affected > 0)
                    return new JsonResult(new { ok = true, msg = "Deleted." });
                else
                    return new JsonResult(new { ok = false, msg = "Nothing deleted." });
            }
            catch (Exception ex)
            {
                // Log ex if you want
                Bootstrap.Logger?.Add($"Pages.User.Address.Endpoints.DeleteSelected: {ex.Message?.ToString()}");
                return new JsonResult(new { ok = false, msg = "Database error." });
            }
        }
    }
}
