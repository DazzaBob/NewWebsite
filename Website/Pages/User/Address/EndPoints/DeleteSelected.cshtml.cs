using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App;
using Website.App.Security;

namespace Website.Pages.User.Address.EndPoints
{
    [IgnoreAntiforgeryToken]
    public class DeleteSelectedModel : PageModel
    {
        public class SetDefaultInput { public int Id { get; set; } }
        public void OnGet()
        {
        }

        // this is public facing so best to use parameters.
        public IActionResult OnPost([FromBody] SetDefaultInput input)
        {
            if (!User.IsAuthorised()) return Unauthorized();
            if (input is null || input.Id <= 0) return BadRequest(new { ok = false, msg = "Invalid address." });

            int userId = User.Id();

            using App.Helper.Connection conn = App.Database.Shared.Connection(App.Database.Schema.Entities.Database);
            Microsoft.Data.Sqlite.SqliteParameter[] parameters =
            [
                new Microsoft.Data.Sqlite.SqliteParameter("@userId", userId),
                new Microsoft.Data.Sqlite.SqliteParameter("@id", input.Id)
            ];

            // Check if the address is default
            string checkSql = "SELECT ISDEFAULT FROM USER_ADDRESS WHERE USER_ID=@userId AND ID=@id LIMIT 1";
            object? result = conn.ExecuteScalar(checkSql, parameters);

            if (result is null) return BadRequest(new { ok = false, msg = "Address not found." });

            bool isDefault = Convert.ToInt32(result) == 1;
            if (isDefault) return new JsonResult(new { ok = false, msg = "Cannot delete default address." });

            try
            {
                string sql = "DELETE FROM USER_ADDRESS WHERE USER_ID=@userId AND ID=@id";
                int affected = Convert.ToInt32(conn.ExecuteNonQuery(sql, parameters));
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
