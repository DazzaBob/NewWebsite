using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.messages
{
    [IgnoreAntiforgeryToken]
    public class CreateThreadModel : PageModel
    {
        private const string MSG = "msg";
        private const string ENT = "ent";

        public sealed class ThreadRequest
        {
            public string ContactId { get; set; } = "";
        }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] ThreadRequest? req)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };

            if (req == null || string.IsNullOrWhiteSpace(req.ContactId))
                return new JsonResult(new { ok = false, msg = "Invalid contact." });

            try
            {
                long me = User.Id();
                long recipientId = 0;          // 0 == no concrete user (System)
                string contactName = "System"; // default for Support-without-user

                // Resolve recipient
                if (req.ContactId.Equals("support", StringComparison.OrdinalIgnoreCase))
                {
                    // Find any user with role 'Support'
                    string sqlSupport = @"
                        SELECT u.id, u.fullname
                        FROM ent.users u
                        JOIN ent.user_roles ur ON ur.user_id = u.id
                        JOIN ent.roles r       ON r.id = ur.role_id
                        WHERE lower(r.name) = 'support'
                        ORDER BY u.id
                        LIMIT 1;";
                    DataTable s = App.Database.DataAccessManager.GetDataTable(ENT, sqlSupport, []);
                    if (s.Rows.Count > 0)
                    {
                        recipientId = Convert.ToInt64(s.Rows[0]["id"]);
                        contactName = s.Rows[0]["fullname"]?.ToString() ?? "Support";
                    }
                    else
                    {
                        // No support user: keep recipientId = 0 and contactName = "System"
                        contactName = "System";
                    }
                }
                else
                {
                    // Numeric user id
                    if (!long.TryParse(req.ContactId, out recipientId))
                        return new JsonResult(new { ok = false, msg = "Invalid recipient id." });

                    // Verify user exists and get name
                    string sqlUser = $"SELECT id, fullname FROM ent.users WHERE id = {recipientId};";
                    DataTable u = App.Database.DataAccessManager.GetDataTable(ENT, sqlUser, []);
                    if (u.Rows.Count == 0)
                        return new JsonResult(new { ok = false, msg = "User not found." });

                    contactName = u.Rows[0]["fullname"]?.ToString() ?? $"User #{recipientId}";
                }

                if (recipientId == me && recipientId != 0)
                    return new JsonResult(new { ok = false, msg = "Cannot chat with yourself." });

                long threadId = 0;

                // If we have a concrete recipient, try to find existing 1:1 thread
                if (recipientId > 0)
                {
                    string sqlFind = $@"
                        SELECT mt.id
                        FROM msg.message_thread mt
                        JOIN msg.message_thread_user a ON a.thread_id = mt.id AND a.user_id = {me}
                        JOIN msg.message_thread_user b ON b.thread_id = mt.id AND b.user_id = {recipientId}
                        LIMIT 1;";
                    object? found = App.Database.DataAccessManager.ExecuteScalar(MSG, sqlFind, []);
                    if (found != null && found != DBNull.Value)
                    {
                        threadId = Convert.ToInt64(found);
                        string sqlUnhide = $@"
                            UPDATE msg.message_thread_user
                               SET is_hidden = false
                             WHERE thread_id = {threadId}
                               AND user_id IN ({me},{recipientId});";
                        App.Database.DataAccessManager.ExecuteNonQuery(MSG, sqlUnhide, []);
                    }
                }

                // Create if none found
                if (threadId == 0)
                {
                    string safeSubject = contactName.Replace("'", "''");
                    string sqlCreate = $@"
                        INSERT INTO msg.message_thread (subject, is_closed, created_on_oad, updated_on_oad)
                        VALUES ('{safeSubject}', false,
                                ((EXTRACT(epoch FROM (now() AT TIME ZONE 'utc'))/86400.0)+25569.0),
                                ((EXTRACT(epoch FROM (now() AT TIME ZONE 'utc'))/86400.0)+25569.0))
                        RETURNING id;";
                    object? tidObj = App.Database.DataAccessManager.ExecuteScalar(MSG, sqlCreate, []);
                    if (tidObj == null || tidObj == DBNull.Value)
                        return new JsonResult(new { ok = false, msg = "Failed to create thread." });

                    threadId = Convert.ToInt64(tidObj);

                    // Link current user
                    string sqlLinkMe = $@"
                        INSERT INTO msg.message_thread_user (thread_id, user_id, is_hidden)
                        VALUES ({threadId}, {me}, false);";
                    App.Database.DataAccessManager.ExecuteNonQuery(MSG, sqlLinkMe, []);

                    // Link recipient only if it exists
                    if (recipientId > 0)
                    {
                        string sqlLinkThem = $@"
                            INSERT INTO msg.message_thread_user (thread_id, user_id, is_hidden)
                            VALUES ({threadId}, {recipientId}, false);";
                        App.Database.DataAccessManager.ExecuteNonQuery(MSG, sqlLinkThem, []);
                    }
                }

                return new JsonResult(new
                {
                    ok = true,
                    threadId,
                    contactName,
                    hasRecipient = (recipientId > 0)
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message });
            }
        }
    }
}
