using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Text;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.messages
{
    [IgnoreAntiforgeryToken]
    public class ThreadPollModel : PageModel
    {
        private const string Schema = "msg";

        public class ThreadPollRequest
        {
            public long ThreadId { get; set; }
            public long AfterMessageId { get; set; }
        }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] ThreadPollRequest? req)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" })
                { StatusCode = StatusCodes.Status401Unauthorized };

            if (req == null || req.ThreadId <= 0)
                return new JsonResult(new { ok = false, msg = "Invalid request." });

            try
            {
                long userId = User.Id();
                long threadId = req.ThreadId;
                long afterId = req.AfterMessageId;

                // ensure thread visible for this user

                string sqlVisible = @$"
                    SELECT COUNT(*) 
                    FROM msg.message_thread_user 
                    WHERE user_id = {userId} 
                      AND thread_id = {threadId} 
                      AND is_hidden = false;";

                long visible = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar(Schema, sqlVisible, []));
                if (visible == 0)
                    return new JsonResult(new { ok = false, msg = "Access denied to thread." });

                // prevent full rebuild if client has no baseline
                if (afterId <= 0)
                {
                    string sqlLast = @$"
                        SELECT id 
                        FROM msg.message
                        WHERE thread_id = {threadId}
                          AND (sender_user_id = {userId} OR recipient_user_id = {userId})
                        ORDER BY id DESC
                        LIMIT 1;";
                    object? obj = App.Database.DataAccessManager.ExecuteScalar(Schema, sqlLast, []);
                    long lastId = obj == null || obj == DBNull.Value ? 0 : Convert.ToInt64(obj);
                    return new JsonResult(new { ok = true, html = "", lastMessageId = lastId });
                }

                // pull only new messages (after last seen id)
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
                    JOIN msg.message_thread_user mtu
                      ON mtu.thread_id = m.thread_id
                     AND mtu.user_id = {userId}
                     AND mtu.is_hidden = false
                    LEFT JOIN ent.users u ON u.id = m.sender_user_id
                    WHERE m.thread_id = {threadId}
                      AND m.id > {afterId}
                    ORDER BY m.created_on_oad ASC;";

                DataTable dt = App.Database.DataAccessManager.GetDataTable(Schema, sql, []);
                List<InitloadmessagesModel.MessageItem> items = [];
                long newLastId = afterId;

                foreach (DataRow dr in dt.Rows)
                {
                    long mid = dr["id"] == DBNull.Value ? 0L : Convert.ToInt64(dr["id"]);
                    if (mid > newLastId) newLastId = mid;

                    items.Add(new InitloadmessagesModel.MessageItem
                    {
                        Id = mid,
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

                // mark new inbound messages as read (user currently viewing thread)
                if (newLastId > afterId)
                {
                    string sqlMarkRead = @$"
                        UPDATE msg.message
                           SET is_read = true,
                               read_on_oad = ((EXTRACT(epoch FROM (now() AT TIME ZONE 'utc')) / 86400.0) + 25569.0)
                         WHERE thread_id = {threadId}
                           AND recipient_user_id = {userId}
                           AND is_read = false
                           AND id > {afterId};";
                    App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlMarkRead, []);
                }

                // build html bubbles for all new messages
                var sb = new StringBuilder();
                foreach (var m in items)
                    sb.Append(Website.App.StringBuilders.Pages.Messages.BuildSingleMessageHtml(m));

                return new JsonResult(new
                {
                    ok = true,
                    html = sb.ToString(),
                    lastMessageId = newLastId
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message });
            }
        }
    }
}