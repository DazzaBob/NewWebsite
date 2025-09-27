using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website.App;
using Website.App.Security;

namespace Website.Pages.User.Address.EndPoints
{
    [IgnoreAntiforgeryToken]
    public class EditLabelModel : PageModel
    {
        public class SetDefaultInput
        {
            public int Id { get; set; }
            public string Label { get; set; } = string.Empty;
        }
        public IActionResult OnPost([FromBody] SetDefaultInput input)
        {
            if (!User.IsAuthorised()) return Unauthorized();
            if (input is null || input.Id <= 0) return BadRequest(new { ok = false, msg = "Invalid address." });
            if (input.Label == string.Empty) return BadRequest(new { ok = false, msg = "Invalid address." });
            int userId = User.Id();

            using App.Helper.Connection conn = App.Database.Shared.Connection(App.Database.Schema.Entities.Database);
            Microsoft.Data.Sqlite.SqliteParameter[] parameters =
            [
                new Microsoft.Data.Sqlite.SqliteParameter("@userId", userId),
                new Microsoft.Data.Sqlite.SqliteParameter("@id", input.Id),
                new Microsoft.Data.Sqlite.SqliteParameter("@label", input.Label)
            ];

            string sql = "UPDATE USER_ADDRESS SET LABEL=@label WHERE USER_ID=@userId AND ID=@id";
            try
            {
                conn.ExecuteNonQuery(sql, parameters);
                return new JsonResult(new { ok = true, msg = input.Label });
            }
            catch (Exception ex)
            {
                Bootstrap.Logger?.Add($"Pages.User.Address.Endpoints.EditLabel: {ex.Message?.ToString()}");
                return new JsonResult(new { ok = false, msg = "Database error." });
            }
        }
    }
}
