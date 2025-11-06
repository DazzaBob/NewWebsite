using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website.App.Security;
namespace Website.Pages.homefolder.content.endpoints.notifications
{
    [IgnoreAntiforgeryToken]
    public class DeleteNotificationModel : PageModel
    {
        public class Input
        {
            public long Id { get; set; } = 0;
        }
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] Input input)
        {
            try
            {
                string sql = $"DELETE FROM msg.notification WHERE id = {input.Id} AND recipient_user_id = {User.Id()};";
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
