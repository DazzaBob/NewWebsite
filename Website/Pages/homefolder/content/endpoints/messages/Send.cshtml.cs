using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.messages
{
    [IgnoreAntiforgeryToken]
    public class SendModel : PageModel
    {
        private const string Schema = "msg";

        public class SendRequest
        {
            public long ThreadId { get; set; }
            public string? Text { get; set; }
        }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] SendRequest? req)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" })
                { StatusCode = StatusCodes.Status401Unauthorized };

            if (req == null || req.ThreadId <= 0)
                return new JsonResult(new { ok = false, msg = "Invalid request." })
                { StatusCode = StatusCodes.Status400BadRequest };

            string raw = (req.Text ?? string.Empty).Trim();
            if (raw.Length == 0)
                return new JsonResult(new { ok = false, msg = "Message is empty." });
            if (raw.Length > 2000) raw = raw.Substring(0, 2000);

            try
            {
                long userId = User.Id();
                long threadId = req.ThreadId;

                // Ensure the sender has visibility on this thread
                string sqlCheck = $@"
                    SELECT COUNT(*)
                    FROM msg.message_thread_user
                    WHERE user_id = {userId}
                      AND thread_id = {threadId}
                      AND is_hidden = false;";
                long visible = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar(Schema, sqlCheck, []));
                if (visible == 0)
                    return new JsonResult(new { ok = false, msg = "Access denied to this thread." });

                // Resolve recipient (the other visible participant)
                string sqlRecipient = $@"
                    SELECT user_id
                    FROM msg.message_thread_user
                    WHERE thread_id = {threadId}
                      AND user_id <> {userId}
                      AND is_hidden = false
                    LIMIT 1;";
                object? ridObj = App.Database.DataAccessManager.ExecuteScalar(Schema, sqlRecipient, []);
                if (ridObj == null || ridObj == DBNull.Value)
                    return new JsonResult(new { ok = false, msg = "No recipient found in thread." });
                long recipientId = Convert.ToInt64(ridObj);

                // Insert message: unread + undinged for recipient; capture created_on_oad
                string safeText = raw.Replace("'", "''");
                string sqlInsert = $@"
                    INSERT INTO msg.message
                        (thread_id, sender_user_id, recipient_user_id, message_text, is_read, ding)
                    VALUES
                        ({threadId}, {userId}, {recipientId}, '{safeText}', false, false)
                    RETURNING id, created_on_oad;";
                DataTable ins = App.Database.DataAccessManager.GetDataTable(Schema, sqlInsert, []);
                if (ins.Rows.Count == 0)
                    return new JsonResult(new { ok = false, msg = "Message insert failed." });

                long messageId = Convert.ToInt64(ins.Rows[0]["id"]);
                double createdOad = Convert.ToDouble(ins.Rows[0]["created_on_oad"]);

                // Update thread summary using the same OAD as the message row (prevents drift)
                string sqlUpdateThread = $@"
                    UPDATE msg.message_thread
                       SET updated_on_oad = {createdOad},
                           last_message_id = {messageId},
                           last_sender_id = {userId},
                           last_recipient_id = {recipientId}
                     WHERE id = {threadId};";
                App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlUpdateThread, []);

                // Touch both participants’ link rows with the same OAD (keeps ordering consistent)
                string sqlTouchLinks = $@"
                    UPDATE msg.message_thread_user
                       SET is_hidden = false 
                     WHERE thread_id = {threadId}
                       AND user_id IN ({userId}, {recipientId});";
                App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlTouchLinks, []);

                // Build a single bubble (right side = mine) via shared renderer
                object? sn = App.Database.DataAccessManager.ExecuteScalar(
                                "ent", $"SELECT fullname FROM ent.users WHERE id = {userId};", []);

                var msg = new InitloadmessagesModel.MessageItem
                {
                    Id = messageId,
                    ThreadId = threadId,
                    SenderName = (sn == null || sn == DBNull.Value) ? "You" : sn.ToString()!,
                    MessageHtml = raw, // renderer will HtmlEncode, emojis survive
                    IconClass = "fa-comment",
                    CreatedAt = DateTime.FromOADate(createdOad),
                    IsMine = true,
                    Unread = false
                };

                string html = Website.App.StringBuilders.Pages.Messages.BuildSingleMessageHtml(msg);

                // Client must set window.__lastMsgId = messageId to prevent duplicate on next poll.
                return new JsonResult(new { ok = true, html, messageId });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message });
            }
        }
    }
}