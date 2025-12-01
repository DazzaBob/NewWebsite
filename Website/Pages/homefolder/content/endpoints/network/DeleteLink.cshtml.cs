using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using Website.App.Database;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.network
{
    [IgnoreAntiforgeryToken]
    public class DeleteLinkModel : PageModel
    {
        private sealed class DeleteDto { public int linked_user_id { get; set; } }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost()
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            if (User.Id() == 0)
                return new JsonResult(new { ok = false, msg = "Bad Request" }) { StatusCode = StatusCodes.Status400BadRequest };

            try
            {
                DeleteDto? dto;
                using (var reader = new StreamReader(Request.Body))
                {
                    var body = reader.ReadToEndAsync().Result ?? string.Empty;
                    dto = JsonSerializer.Deserialize<DeleteDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                if (dto == null || dto.linked_user_id <= 0)
                    return new JsonResult(new { ok = false, msg = "Invalid payload" }) { StatusCode = StatusCodes.Status400BadRequest };

                int userId = User.Id();

                // Remove only THIS user's side of the link
                string sql = $@"DELETE FROM ent.user_link WHERE user_id = {userId} AND linked_user_id = {dto.linked_user_id};";
                var affected = DataAccessManager.ExecuteNonQuery(App.Database.Schema.Entities.Name, sql, []);

                if (affected <= 0)
                    return new JsonResult(new { ok = false, msg = "Link not found" });

                return new JsonResult(new { ok = true, msg = "Link removed" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message });
            }
        }
    }
}