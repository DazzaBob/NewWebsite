using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Security;

namespace Website.Pages.User.Address
{
    [IgnoreAntiforgeryToken]
    public class SetDefaultModel : PageModel
    {

        public class SetDefaultInput { public int Id { get; set; } }
        public void OnGet()
        {
        }
        public IActionResult OnPost([FromBody] SetDefaultInput input)
        {
            if (!User.IsAuthorised()) { return Unauthorized(); }
            if (input is null || input.Id <= 0) return BadRequest(new { ok = false, msg = "Invalid address." });

            int userId = User.Id();

            using App.Helper.Connection conn = App.Database.Shared.Connection(App.Database.Schema.Entities.Database);
            Website.App.Database.Shared.Update(conn, "USER_ADDRESS", "ISDEFAULT = 0", $"USER_ID = {userId}");
            Website.App.Database.Shared.Update(conn, "USER_ADDRESS", "ISDEFAULT = 1", $"(USER_ID = {userId}) AND (ID = {input.Id})");
            DataTable dt = Website.App.Database.Shared.GetDataTable(conn, "USER_ADDRESS", $"(USER_ID = {userId}) AND (ID = {input.Id})");
            if (dt.Rows.Count == 0) return NotFound(new { ok = false, msg = "Address not found." });

            string label = dt.Rows[0]["LABEL"] == DBNull.Value ? "Select Address" : dt.Rows[0]["LABEL"].ToString()?.Trim() ?? "Select Address";
            return new JsonResult(new { ok = true, label });
        }
    }
}
