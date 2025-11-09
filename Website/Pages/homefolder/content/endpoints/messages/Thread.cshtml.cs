using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.messages
{
    [IgnoreAntiforgeryToken]
    public class ThreadModel : PageModel
    {
        private const string Schema = "msg";

        public sealed class ThreadRequest
        {
            public long ThreadId { get; set; }
        }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] ThreadRequest? req)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" })
                { StatusCode = StatusCodes.Status401Unauthorized };

            if (req == null || req.ThreadId <= 0)
                return new JsonResult(new { ok = false, msg = "Invalid thread id." });

            try
            {
                long userId = User.Id();
                long threadId = req.ThreadId;

                // Guard: user must have visibility to this thread (not hidden)
                string sqlVisible = @$"
                    SELECT COUNT(*)
                    FROM msg.message_thread_user mtu
                    WHERE mtu.user_id = {userId}
                      AND mtu.thread_id = {threadId}
                      AND mtu.is_hidden = false;";
                long visible = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar(Schema, sqlVisible, []));
                if (visible == 0)
                    return new JsonResult(new { ok = false, msg = "Access denied." });

                // Pull messages for this thread (chronological)
                string sql = @$"
                    SELECT 
                        m.id,
                        m.thread_id,
                        COALESCE(u.fullname, 'System') AS sender_name,
                        m.message_text AS message_html,
                        COALESCE(m.icon_class, 'fa-comments') AS icon_class,
                        m.created_on_oad,
                        (m.sender_user_id = {userId}) AS is_mine,
                        (m.recipient_user_id = {userId} AND m.is_read = false) AS unread
                    FROM msg.message m
                    LEFT JOIN ent.users u ON u.id = m.sender_user_id
                    WHERE m.thread_id = {threadId}
                    ORDER BY m.created_on_oad ASC;";

                DataTable dt = App.Database.DataAccessManager.GetDataTable(Schema, sql, []);
                List<InitloadmessagesModel.MessageItem> messages = [];

                foreach (DataRow dr in dt.Rows)
                {
                    messages.Add(new InitloadmessagesModel.MessageItem
                    {
                        Id = dr["id"] == DBNull.Value ? 0L : Convert.ToInt64(dr["id"]),
                        ThreadId = dr["thread_id"] == DBNull.Value ? 0L : Convert.ToInt64(dr["thread_id"]),
                        SenderName = dr["sender_name"]?.ToString() ?? "",
                        MessageHtml = dr["message_html"]?.ToString() ?? "",
                        IconClass = dr["icon_class"]?.ToString() ?? "fa-comments",
                        CreatedAt = dr["created_on_oad"] == DBNull.Value
                            ? DateTime.UtcNow
                            : DateTime.FromOADate(Convert.ToDouble(dr["created_on_oad"])),
                        IsMine = dr["is_mine"] != DBNull.Value && Convert.ToBoolean(dr["is_mine"]),
                        Unread = dr["unread"] != DBNull.Value && Convert.ToBoolean(dr["unread"])
                    });
                }

                string html = Website.App.StringBuilders.Pages.Messages.BuildThreadHtml(messages);
                long lastMessageId = messages.Count > 0 ? messages.Max(m => m.Id) : 0;

                // Mark any of THIS user's unread in this (visible) thread as read
                string sqlRead = @$"
                    UPDATE msg.message
                       SET is_read = true,
                           read_on_oad = ((EXTRACT(epoch FROM (now() AT TIME ZONE 'utc')) / 86400.0) + 25569.0)
                     WHERE thread_id = {threadId}
                       AND recipient_user_id = {userId}
                       AND is_read = false;";
                App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlRead, []);

                return new JsonResult(new { ok = true, html, lastMessageId });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message });
            }
        }
    }
}