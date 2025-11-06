using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.notifications
{
    [IgnoreAntiforgeryToken]
    public class PollModel : PageModel
    {
        public class PollRequest { public bool IsInitial { get; set; } }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] PollRequest? req)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };

            try
            {
                long userId = User.Id();
                bool isInitial = req?.IsInitial ?? false;
                long unreadCount = 0;
                // Always compute unread so TURN is accurate on every poll
                bool shouldDing = false;
                string sqlUnread = $"SELECT COUNT(*) FROM msg.notification WHERE recipient_user_id = {userId} AND is_read = false;";
                unreadCount = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar("msg", sqlUnread, []));

                if (isInitial)
                {
                    shouldDing = unreadCount > 0;
                }
                else
                {
                    shouldDing = false;
                    // NON-INITIAL: ding if there are unread OR undinged
                    string sqlUndinged = $"SELECT COUNT(*) FROM msg.notification WHERE recipient_user_id = {userId} AND ding = false;";
                    long undingedCount = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar("msg", sqlUndinged, []));

                    shouldDing = undingedCount > 0;
                }
                if (shouldDing)
                {
                    string sqlUpdate = $"UPDATE msg.notification SET ding = true WHERE recipient_user_id = {userId} AND ding = false;";
                    App.Database.DataAccessManager.ExecuteNonQuery("msg", sqlUpdate, []);
                }

                return new JsonResult(new { ok = true, unreadCount, shouldDing });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message });
            }
        }
    }
}