using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Text;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.messages
{
    [IgnoreAntiforgeryToken]
    public class PollModel : PageModel
    {
        private const string Schema = "msg";

        public class PollRequest
        {
            public bool IsInitial { get; set; }
            public long LastThreadId { get; set; }
        }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] PollRequest? req)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };

            try
            {
                long userId = User.Id();
                bool isInitial = req?.IsInitial ?? false;
                long lastThreadId = req?.LastThreadId ?? 0;
                bool shouldDing = false;

                // Total unread (only for threads visible to this user)
                string sqlUnread = @$"
                    SELECT COUNT(*)
                    FROM msg.message m
                    JOIN msg.message_thread_user mtu
                      ON mtu.thread_id = m.thread_id
                     AND mtu.user_id = {userId}
                     AND mtu.is_hidden = false
                    WHERE m.recipient_user_id = {userId}
                      AND m.is_read = false;";
                long unreadCount = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar(Schema, sqlUnread, []));

                // Initial pass: only return unread + (optionally) ding; no HTML or toggles
                if (isInitial)
                {
                    string sqlUndinged = @$"
                        SELECT COUNT(*)
                        FROM msg.message m
                        JOIN msg.message_thread_user mtu
                          ON mtu.thread_id = m.thread_id
                         AND mtu.user_id = {userId}
                         AND mtu.is_hidden = false
                        WHERE m.recipient_user_id = {userId}
                          AND m.is_read = false
                          AND m.ding = false;";
                    long undingedCount = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar(Schema, sqlUndinged, []));
                    shouldDing = undingedCount > 0;

                    if (shouldDing)
                    {
                        string sqlMarkInit = @$"
                            UPDATE msg.message
                               SET ding = true
                             WHERE recipient_user_id = {userId}
                               AND is_read = false
                               AND ding = false
                               AND thread_id IN (
                                   SELECT thread_id
                                   FROM msg.message_thread_user
                                   WHERE user_id = {userId} AND is_hidden = false
                               );";
                        App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlMarkInit, []);
                    }

                    return new JsonResult(new
                    {
                        ok = true,
                        unreadCount,
                        shouldDing,
                        lastThreadId,
                        html = "",
                        unreadThreadIds = Array.Empty<long>(),
                        readThreadIds = Array.Empty<long>()
                    });
                }

                // 1) New threads (id > lastThreadId) that are visible to this user
                string sqlNewThreads = @$"
                    SELECT
                        mt.id,
                        mt.subject AS title,
                        'fa-comments' AS icon_class,
                        COALESCE(u.fullname, 'System') AS sender_name,
                        lm.message_text AS last_message_preview,
                        mt.updated_on_oad AS last_message_time
                    FROM msg.message_thread mt
                    JOIN msg.message_thread_user mtu
                      ON mtu.thread_id = mt.id
                     AND mtu.user_id = {userId}
                     AND mtu.is_hidden = false
                    LEFT JOIN msg.message lm
                      ON lm.id = mt.last_message_id
                    LEFT JOIN ent.users u
                      ON u.id = mt.last_sender_id
                    WHERE mt.id > {lastThreadId}
                    ORDER BY mt.id DESC;";
                DataTable newThreads = App.Database.DataAccessManager.GetDataTable(Schema, sqlNewThreads, []);

                // 2) Existing threads (<= lastThreadId): compute unread state now (visible only)
                string sqlExistingStatus = @$"
                    SELECT
                        m.thread_id AS id,
                        SUM(CASE WHEN (m.is_read = false AND m.recipient_user_id = {userId}) THEN 1 ELSE 0 END) AS unreadCount
                    FROM msg.message m
                    JOIN msg.message_thread_user mtu
                      ON mtu.thread_id = m.thread_id
                     AND mtu.user_id = {userId}
                     AND mtu.is_hidden = false
                    WHERE m.thread_id <= {lastThreadId}
                    GROUP BY m.thread_id;";
                DataTable existingStatus = App.Database.DataAccessManager.GetDataTable(Schema, sqlExistingStatus, []);

                // Build HTML only for NEW threads (prepend on client); mark them visually as unread
                string html = string.Empty;
                long newMaxThreadId = lastThreadId;
                if (newThreads.Rows.Count > 0)
                {
                    var sb = new StringBuilder();
                    foreach (DataRow row in newThreads.Rows)
                    {
                        string threadId = row["id"].ToString()!;
                        string unreadClass = "unread";
                        string sideClass = "left";
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

                    long maxNew = newThreads.AsEnumerable()
                        .Select(r => r["id"] == DBNull.Value ? 0L : Convert.ToInt64(r["id"]))
                        .DefaultIfEmpty(lastThreadId)
                        .Max();
                    if (maxNew > newMaxThreadId) newMaxThreadId = maxNew;
                }

                // Build toggle lists for existing threads
                List<long> unreadThreadIds = [];
                List<long> readThreadIds = [];

                foreach (DataRow row in existingStatus.Rows)
                {
                    long tid = row["id"] == DBNull.Value ? 0L : Convert.ToInt64(row["id"]);
                    long ucnt = row["unreadCount"] == DBNull.Value ? 0L : Convert.ToInt64(row["unreadCount"]);
                    if (ucnt > 0) unreadThreadIds.Add(tid); else readThreadIds.Add(tid);
                }

                // Ding logic:
                // - Ding when there are brand new threads (with unseen messages for this user)
                // - Or when existing visible threads have unread messages that weren't dinged yet
                if (newThreads.Rows.Count > 0)
                {
                    shouldDing = true;

                    // Mark all unread-in-new-threads as dinged for this recipient to avoid repeat dings
                    string ids = string.Join(",", newThreads.AsEnumerable().Select(r => Convert.ToInt64(r["id"])));
                    if (!string.IsNullOrWhiteSpace(ids))
                    {
                        string sqlDingNew = @$"
                            UPDATE msg.message
                               SET ding = true
                             WHERE thread_id IN ({ids})
                               AND recipient_user_id = {userId}
                               AND is_read = false
                               AND ding = false;";
                        App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlDingNew, []);
                    }
                }
                else
                {
                    if (unreadThreadIds.Count > 0)
                    {
                        string ids = string.Join(",", unreadThreadIds);
                        string sqlDingExisting = @$"
                            UPDATE msg.message
                               SET ding = true
                             WHERE thread_id IN ({ids})
                               AND recipient_user_id = {userId}
                               AND is_read = false
                               AND ding = false;";
                        int affected = (int)App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlDingExisting, []);
                        shouldDing = affected > 0;
                    }
                }

                return new JsonResult(new
                {
                    ok = true,
                    unreadCount,
                    shouldDing,
                    lastThreadId = newMaxThreadId,
                    html,                  // HTML for NEW thread headers only (prepend client-side)
                    unreadThreadIds,       // mark these as unread in UI
                    readThreadIds          // clear unread state for these
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message });
            }
        }
    }
}