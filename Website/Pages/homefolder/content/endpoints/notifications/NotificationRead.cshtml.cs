using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.notifications
{
    [IgnoreAntiforgeryToken]
    public class NotificationReadModel : PageModel
    {
        public class Input { public long Id { get; set; } }

        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] Input input)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };

            try
            {
                string sql = $"UPDATE msg.notification SET is_read = true, ding = true WHERE id = {input.Id} AND recipient_user_id = {User.Id()};";
                long affected = App.Database.DataAccessManager.ExecuteNonQuery("msg", sql, []);
                return new JsonResult(new { ok = affected > 0 });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message });
            }
        }
    }
}
