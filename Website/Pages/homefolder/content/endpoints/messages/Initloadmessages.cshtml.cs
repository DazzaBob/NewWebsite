using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Text;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.messages
{
    [IgnoreAntiforgeryToken]
    public class InitloadmessagesModel : PageModel
    {
        private const string Schema = "msg";

        internal class MessageItem
        {
            public long Id { get; set; }
            public long ThreadId { get; set; }
            public string SenderName { get; set; } = "";
            public string MessageHtml { get; set; } = "";
            public string IconClass { get; set; } = "fa-comments";
            public DateTime CreatedAt { get; set; }
            public bool IsMine { get; set; }
            public bool Unread { get; set; }
        }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost()
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" })
                { StatusCode = StatusCodes.Status401Unauthorized };

            try
            {
                long userId = User.Id();
                long lastThreadId = 0;
                string html = string.Empty;

                // Threads visible to this user (joined via msg.message_thread_user)
                string sql = @$"
                    SELECT
                        mt.id,
                        mt.subject AS title,
                        'fa-comments' AS icon_class,
                        COALESCE(u.fullname, 'System') AS sender_name,
                        lm.message_text AS last_message_preview,
                        mt.updated_on_oad AS last_message_time,
                        COALESCE(uc.unread_count, 0) AS unreadCount
                    FROM msg.message_thread mt
                    JOIN msg.message_thread_user mtu
                      ON mtu.thread_id = mt.id
                     AND mtu.user_id = {userId}
                     AND mtu.is_hidden = false
                    LEFT JOIN msg.message lm
                      ON lm.id = mt.last_message_id
                    LEFT JOIN ent.users u
                      ON u.id = mt.last_sender_id
                    LEFT JOIN (
                        SELECT thread_id, COUNT(*) AS unread_count
                        FROM msg.message
                        WHERE recipient_user_id = {userId} AND is_read = false
                        GROUP BY thread_id
                    ) uc
                      ON uc.thread_id = mt.id
                    ORDER BY mt.updated_on_oad DESC
                    LIMIT 50;";

                DataTable table = App.Database.DataAccessManager.GetDataTable(Schema, sql, []);

                if (table.Rows.Count > 0)
                {
                    lastThreadId = table.AsEnumerable()
                        .Select(r => r["id"] == DBNull.Value ? 0L : Convert.ToInt64(r["id"]))
                        .Max();

                    var sb = new StringBuilder();
                    foreach (DataRow row in table.Rows)
                    {
                        string threadId = row["id"].ToString()!;
                        string unreadClass = Convert.ToInt64(row["unreadCount"]) > 0 ? "unread" : "";
                        string sideClass = "left"; // for headers, not per-message alignment
                        string icon = row["icon_class"]?.ToString() ?? "fa-comments";
                        string sender = row["sender_name"]?.ToString() ?? "Unknown";
                        string text = row["last_message_preview"]?.ToString() ?? "";
                        string formattedTime = "";

                        if (row["last_message_time"] != DBNull.Value)
                        {
                            double oad = Convert.ToDouble(row["last_message_time"]);
                            formattedTime = DateTime.FromOADate(oad).ToString("HH:mm");
                        }

                        sb.Append(Website.App.StringBuilders.Pages.Messages.BuildThreadHeaderHTML(
                            threadId, unreadClass, sideClass, icon, sender, text, formattedTime));
                    }

                    html = sb.ToString();
                }

                // unread and ding logic (per-user)
                string sqlUnread = $"SELECT COUNT(*) FROM msg.message WHERE recipient_user_id = {userId} AND is_read = false;";
                long unreadCount = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar(Schema, sqlUnread, []));

                string sqlUndinged = $"SELECT COUNT(*) FROM msg.message WHERE recipient_user_id = {userId} AND is_read = false AND ding = false;";
                long undingedCount = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar(Schema, sqlUndinged, []));
                bool shouldDing = undingedCount > 0;

                if (shouldDing)
                {
                    string sqlMarkDing = @$"
                        UPDATE msg.message 
                        SET ding = true 
                        WHERE recipient_user_id = {userId} AND is_read = false AND ding = false;";
                    App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlMarkDing, []);
                }

                return new JsonResult(new { ok = true, html, lastThreadId, unreadCount, shouldDing });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, message = ex.Message });
            }
        }
    }
}