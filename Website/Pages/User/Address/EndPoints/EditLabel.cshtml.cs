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
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] SetDefaultInput input)
        {
            if (!User.IsAuthorised()) return Unauthorized();
            if (input is null || input.Id <= 0) return BadRequest(new { ok = false, msg = "Invalid address." });
            if (input.Label == string.Empty) return BadRequest(new { ok = false, msg = "Invalid address." });
            int userId = User.Id();

            Npgsql.NpgsqlParameter[] parameters =
            [
                new Npgsql.NpgsqlParameter("@userId", userId),
                new Npgsql.NpgsqlParameter("@id", input.Id),
                new Npgsql.NpgsqlParameter("@label", input.Label)
            ];

            string sql = $"UPDATE {App.Database.Schema.Entities.UserAddress} SET LABEL=@label WHERE USER_ID=@userId AND ID=@id";
            try
            {

                App.Database.DataAccessManager.ExecuteNonQuery(App.Database.Schema.Entities.SchemaName, sql, parameters);
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
