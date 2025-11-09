using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.messages
{
    [IgnoreAntiforgeryToken]
    public class HideThreadModel : PageModel
    {
        private const string Schema = "msg";

        public class HideRequest
        {
            public long ThreadId { get; set; }
        }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] HideRequest? req)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" })
                { StatusCode = StatusCodes.Status401Unauthorized };

            if (req == null || req.ThreadId <= 0)
                return new JsonResult(new { ok = false, msg = "Invalid thread id." })
                { StatusCode = StatusCodes.Status400BadRequest };

            try
            {
                long userId = User.Id();
                long threadId = req.ThreadId;

                // Ensure the user actually has visibility in the thread
                string sqlCheck = @$"SELECT COUNT(*) FROM msg.message_thread_user 
                    WHERE thread_id = {threadId} 
                      AND user_id = {userId};";
                long exists = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar(Schema, sqlCheck, []));
                if (exists == 0)
                    return new JsonResult(new { ok = false, msg = "Thread not found or not visible." });

                // Mark as hidden — do NOT delete
                string sqlHide = @$"
                    UPDATE msg.message_thread_user
                       SET is_hidden = true 
                     WHERE thread_id = {threadId}
                       AND user_id = {userId};";
                App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlHide, []);

                // Optional: clear any unread counts for this thread
                string sqlRead = @$"
                    UPDATE msg.message
                       SET is_read = true
                     WHERE thread_id = {threadId}
                       AND recipient_user_id = {userId}
                       AND is_read = false;";
                App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlRead, []);

                return new JsonResult(new { ok = true, msg = "Conversation hidden." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message });
            }
        }
    }
}