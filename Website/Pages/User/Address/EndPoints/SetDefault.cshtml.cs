using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Security;

namespace Website.Pages.User.Address.EndPoints
{
    [IgnoreAntiforgeryToken]
    public class SetDefaultModel : PageModel
    {
        private readonly string EntitiesSchema = App.Database.Schema.Entities.SchemaName;
        private readonly string UserAddress = App.Database.Schema.Entities.UserAddress;
        public class SetDefaultInput { public int Id { get; set; } }
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] SetDefaultInput input)
        {
            if (!User.IsAuthorised()) { return Unauthorized(); }
            if (input is null || input.Id <= 0) return BadRequest(new { ok = false, msg = "Invalid address." });

            int userId = User.Id();

            App.Database.DataAccessManager.Update(EntitiesSchema, UserAddress, "ISDEFAULT = false", $"USER_ID = {userId}");
            App.Database.DataAccessManager.Update(EntitiesSchema, UserAddress, "ISDEFAULT = true", $"(USER_ID = {userId}) AND (ID = {input.Id})");

            using DataTable DT = App.Database.DataAccessManager.GetDataTable(EntitiesSchema, UserAddress, $"(USER_ID = {userId}) AND (ISDEFAULT = true)");
            if (DT.Rows.Count == 0) return NotFound(new { ok = false, msg = "Address not found." });

            string label = DT.Rows[0]["LABEL"] == DBNull.Value ? "Select Address" : DT.Rows[0]["LABEL"].ToString()?.Trim() ?? "Select Address";
            return new JsonResult(new { ok = true, label });
        }
    }
}
