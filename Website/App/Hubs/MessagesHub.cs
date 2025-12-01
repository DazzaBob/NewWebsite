using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using Website.App.Security;

namespace Website.App.Hubs
{
    public interface IMessagesClient
    {
        // Full inbox snapshot (Initloadmessages + initial Poll semantics)
        Task MessageThreadsSnapshot(MessageThreadsSnapshotDto snapshot);

        // Incremental inbox changes (Poll semantics)
        Task MessageThreadsDelta(MessageThreadsDeltaDto delta);

        // Contacts for "Start new chat"
        Task MessageContactsSnapshot(MessageContactsSnapshotDto snapshot);

        // A new thread created or an existing one resurfaced
        Task MessageThreadCreated(MessageThreadCreatedDto dto);

        // A thread was hidden for this user
        Task MessageThreadHidden(long threadId);

        // Full thread opened (Thread.cshtml.cs behaviour)
        Task MessageThreadOpened(MessageThreadDto thread);

        // Incremental new messages in a thread (ThreadPoll.cshtml.cs behaviour)
        Task MessageThreadDelta(MessageThreadDeltaDto delta);

        // Sent message echo to sender (Send.cshtml.cs behaviour)
        Task MessageSent(MessageItemDto message);

        // Incoming message to recipient
        Task MessageReceived(MessageItemDto message);

        // Generic error for this connection / operation
        Task MessageError(MessageErrorDto error);
    }
    [IgnoreAntiforgeryToken] //[Authorize] // requires cookie auth; PWA must be logged in
    public class MessagesHub : Hub<IMessagesClient>
    {
        private const string Schema = Database.Schema.Messaging.Name;
        // Should:
        //  - resolve current user id
        //  - load top N visible threads
        //  - compute unreadCount + shouldDing (Initloadmessages + Poll initial behaviour)
        //  - send MessageThreadsSnapshot(snapshot) to the caller
        public async Task SubscribeThreads()
        {
            try
            {
                int userId = Context.User.Id();
                if (userId <= 0)
                {
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "AUTH",
                        Message = "Not logged in."
                    });
                    return;
                }

                string html = "";
                long lastThreadId = 0;

                // This SQL and builder call are ported directly from Initloadmessages.cshtml.cs
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

                var snapshot = new MessageThreadsSnapshotDto
                {
                    LastThreadId = lastThreadId,
                    Html = html
                };

                // unread and ding logic (per-user) – exactly as Initloadmessages.cshtml.cs
                string sqlUnread = $"SELECT COUNT(*) FROM msg.message WHERE recipient_user_id = {userId} AND is_read = false;";
                long unreadCount = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar(Schema, sqlUnread, []));
                snapshot.UnreadCount = (int)unreadCount;

                string sqlUndinged = $"SELECT COUNT(*) FROM msg.message WHERE recipient_user_id = {userId} AND is_read = false AND ding = false;";
                long undingedCount = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar(Schema, sqlUndinged, []));
                bool shouldDing = undingedCount > 0;
                snapshot.ShouldDing = shouldDing;

                if (shouldDing)
                {
                    string sqlMarkDing = @$"
                        UPDATE msg.message 
                        SET ding = true 
                        WHERE recipient_user_id = {userId} AND is_read = false AND ding = false;";
                    App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlMarkDing, []);
                }

                await Clients.Caller.MessageThreadsSnapshot(snapshot);
            }
            catch (Exception ex)
            {
                await Clients.Caller.MessageError(new MessageErrorDto
                {
                    Code = "SUBSCRIBE_THREADS_FAILED",
                    Message = ex.Message
                });
            }
        }

        // Optional: clean up server-side state; can be a no-op if you only rely on disconnect.
        public async Task UnsubscribeThreads()
        {
            // TODO: if you maintain any per-connection state, clear it here
            await Task.CompletedTask;
        }

        // Explicit snapshot request (manual refresh button etc.)
        public async Task RequestThreadsSnapshot()
        {
            // TODO: same semantics as SubscribeThreads, but called on demand
            await Task.CompletedTask;
        }

        // Socket version of messages/Poll.cshtml.cs:
        // Given lastThreadId, compute:
        //  - new threads (id > lastThreadId)
        //  - unreadThreadIds / readThreadIds
        //  - unreadCount + shouldDing
        // Then send MessageThreadsDelta(delta) to the caller.
        public async Task RequestThreadsDelta(long lastThreadId)
        {
            try
            {
                if (Context.User == null || !Context.User.IsAuthorised())
                {
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "UNAUTHORIZED",
                        Message = "Unauthorized"
                    });
                    return;
                }

                long userId = Context.User.Id();
                long lastIdLocal = lastThreadId;
                bool shouldDing = false;

                // -----------------------------
                // Total unread (visible threads only)
                // -----------------------------
                string sqlUnread = @$"
            SELECT COUNT(*)
            FROM msg.message m
            JOIN msg.message_thread_user mtu
              ON mtu.thread_id = m.thread_id
             AND mtu.user_id = {userId}
             AND mtu.is_hidden = false
            WHERE m.recipient_user_id = {userId}
              AND m.is_read = false;";

                long unreadCount = Convert.ToInt64(
                    App.Database.DataAccessManager.ExecuteScalar(Schema, sqlUnread, [])
                );

                // -----------------------------
                // 1) New threads (id > lastThreadId)
                // -----------------------------
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
            WHERE mt.id > {lastIdLocal}
            ORDER BY mt.id DESC;";

                DataTable newThreads = App.Database.DataAccessManager.GetDataTable(Schema, sqlNewThreads, []);

                // -----------------------------
                // 2) Existing threads (<= lastThreadId): unread/zero-unread
                // -----------------------------
                string sqlExistingStatus = @$"
            SELECT
                m.thread_id AS id,
                SUM(
                    CASE
                        WHEN (m.is_read = false AND m.recipient_user_id = {userId})
                        THEN 1 ELSE 0
                    END
                ) AS unreadCount
            FROM msg.message m
            JOIN msg.message_thread_user mtu
              ON mtu.thread_id = m.thread_id
             AND mtu.user_id = {userId}
             AND mtu.is_hidden = false
            WHERE m.thread_id <= {lastIdLocal}
            GROUP BY m.thread_id;";

                DataTable existingStatus = App.Database.DataAccessManager.GetDataTable(Schema, sqlExistingStatus, []);

                var delta = new MessageThreadsDeltaDto
                {
                    LastThreadId = lastIdLocal,
                    UnreadCount = (int)unreadCount,
                    ShouldDing = false
                };

                // -----------------------------
                // Build NewThreads summaries + compute new LastThreadId
                // -----------------------------
                if (newThreads.Rows.Count > 0)
                {
                    long newMaxThreadId = lastIdLocal;

                    foreach (DataRow row in newThreads.Rows)
                    {
                        long tid = row["id"] == DBNull.Value ? 0L : Convert.ToInt64(row["id"]);
                        if (tid > newMaxThreadId) newMaxThreadId = tid;

                        double lastOad = 0.0;
                        if (row["last_message_time"] != DBNull.Value)
                            lastOad = Convert.ToDouble(row["last_message_time"]);

                        var summary = new MessageThreadSummaryDto
                        {
                            ThreadId = tid,
                            Label = row["title"]?.ToString() ?? string.Empty,
                            Preview = row["last_message_preview"]?.ToString() ?? string.Empty,
                            IsUnread = true, // new threads are unread for this user
                            LastMessageOad = lastOad
                        };

                        delta.NewThreads.Add(summary);
                    }

                    delta.LastThreadId = newMaxThreadId;
                }

                // -----------------------------
                // Build unread/read toggle lists for existing threads
                // -----------------------------
                foreach (DataRow row in existingStatus.Rows)
                {
                    long tid = row["id"] == DBNull.Value ? 0L : Convert.ToInt64(row["id"]);
                    if (tid <= 0) continue;

                    long ucnt = row["unreadCount"] == DBNull.Value ? 0L : Convert.ToInt64(row["unreadCount"]);
                    if (ucnt > 0)
                        delta.UnreadThreadIds.Add(tid);
                    else
                        delta.ReadThreadIds.Add(tid);
                }

                // -----------------------------
                // Ding logic – identical semantics to PollModel
                // -----------------------------
                if (newThreads.Rows.Count > 0)
                {
                    // Brand new threads → single ding and mark their messages as dinged
                    string ids = string.Join(
                        ",",
                        newThreads.AsEnumerable()
                                  .Select(r => Convert.ToInt64(r["id"]))
                                  .Where(id => id > 0)
                    );

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

                    shouldDing = true;
                }
                else if (delta.UnreadThreadIds.Count > 0)
                {
                    // No new threads, but existing visible threads now have unread messages
                    string ids = string.Join(",", delta.UnreadThreadIds);
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

                delta.ShouldDing = shouldDing;

                await Clients.Caller.MessageThreadsDelta(delta);
            }
            catch (Exception ex)
            {
                await Clients.Caller.MessageError(new MessageErrorDto
                {
                    Code = "THREADS_DELTA_FAILED",
                    Message = ex.Message
                });
            }
        }

        // Socket version of Contacts.cshtml.cs.
        // Should send MessageContactsSnapshot(contacts) to the caller.
        public async Task GetContacts()
        {
            // TODO: port logic from Contacts.cshtml.cs
            await Task.CompletedTask;
        }

        // Socket version of CreateThread.cshtml.cs.
        // contactId can be "support" or numeric (user id).
        // Should:
        //  - resolve recipient
        //  - prevent self-chat
        //  - reuse or create thread
        //  - send MessageThreadCreated(dto) to caller
        public async Task CreateThread(string contactId)
        {
            // TODO: port logic from CreateThread.cshtml.cs
            await Task.CompletedTask;
        }

        // Socket version of HideThread.cshtml.cs.
        // Should:
        //  - verify visibility
        //  - mark is_hidden = true for this user
        //  - mark unread in that thread as read
        //  - send MessageThreadHidden(threadId) to caller
        public async Task HideThread(long threadId)
        {
            try
            {
                if (Context.User == null || !Context.User.IsAuthorised())
                {
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "UNAUTHORIZED",
                        Message = "Unauthorized"
                    });
                    return;
                }

                if (threadId <= 0)
                {
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "INVALID_THREAD",
                        Message = "Invalid thread."
                    });
                    return;
                }

                int userId = Context.User.Id();

                // Verify visibility for this user
                string sqlVisible = $@"
                SELECT COUNT(*)
                FROM msg.message_thread_user
                WHERE thread_id = {threadId}
                  AND user_id = {userId}
                  AND is_hidden = false;";

                long visible = Convert.ToInt64(
                    App.Database.DataAccessManager.ExecuteScalar(Schema, sqlVisible, [])
                );

                if (visible == 0)
                {
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "ACCESS_DENIED",
                        Message = "You no longer have access to this conversation."
                    });
                    return;
                }

                // Mark the link as hidden for this user
                string sqlHide = $@"
                UPDATE msg.message_thread_user
                   SET is_hidden = true
                 WHERE thread_id = {threadId}
                   AND user_id = {userId};";
                App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlHide, []);

                // Mark this user's unread inbound messages in that thread as read
                string sqlMarkRead = $@"
                UPDATE msg.message
                   SET is_read = true,
                       read_on_oad = {DateTime.Now.ToOADate()}
                 WHERE thread_id = {threadId}
                   AND recipient_user_id = {userId}
                   AND is_read = false;";
                App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlMarkRead, []);

                // Notify caller so UI can remove the thread
                await Clients.Caller.MessageThreadHidden(threadId);
            }
            catch (Exception ex)
            {
                await Clients.Caller.MessageError(new MessageErrorDto
                {
                    Code = "HIDE_THREAD_FAILED",
                    Message = ex.Message
                });
            }
        }

        // Socket version of Thread.cshtml.cs.
        // Should:
        //  - verify visibility
        //  - load all messages in the thread
        //  - mark unread inbound messages as read
        //  - send MessageThreadOpened(threadDto) to caller
        public async Task OpenThread(long threadId)
        {
            try
            {
                int userId = Context.User.Id();
                if (userId <= 0)
                {
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "AUTH",
                        Message = "Not logged in."
                    });
                    return;
                }

                // -----------------------------
                // 1. Validate user visibility
                // -----------------------------
                string sqlVisible = @$"
                SELECT COUNT(*)
                FROM msg.message_thread_user
                WHERE thread_id = {threadId}
                AND user_id = {userId}
                AND is_hidden = false;";

                long vis = Convert.ToInt64(
                    App.Database.DataAccessManager.ExecuteScalar(Schema, sqlVisible, [])
                );

                if (vis == 0)
                {
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "ACCESS_DENIED",
                        Message = "You no longer have access to this conversation."
                    });
                    return;
                }

                // -----------------------------
                // 2. Load ALL messages in thread
                // -----------------------------
                string sql = @$"
                SELECT 
                    m.id,
                    m.thread_id,
                    COALESCE(u.fullname, 'Unknown') AS sender_name,
                    m.message_text AS message_html,
                    m.icon_class,
                    m.created_on_oad,
                    (CASE WHEN m.sender_user_id = {userId} THEN true ELSE false END) AS is_mine,
                    (CASE WHEN m.is_read = false AND m.recipient_user_id = {userId} THEN true ELSE false END) AS unread
                FROM msg.message m
                LEFT JOIN ent.users u ON u.id = m.sender_user_id
                WHERE m.thread_id = {threadId}
                ORDER BY m.created_on_oad ASC;";

                DataTable table = App.Database.DataAccessManager.GetDataTable(Schema, sql, []);

                var threadDto = new MessageThreadDto
                {
                    ThreadId = threadId,
                    Label = "",   // we fill this below
                    LastMessageId = 0
                };

                foreach (DataRow row in table.Rows)
                {
                    long mid = Convert.ToInt64(row["id"]);
                    threadDto.LastMessageId = mid;

                    threadDto.Messages.Add(new MessageItemDto
                    {
                        Id = mid,
                        ThreadId = threadId,
                        Text = row["message_html"]?.ToString() ?? "",
                        IsMine = Convert.ToBoolean(row["is_mine"]),
                        IsUnread = Convert.ToBoolean(row["unread"]),
                        SentOad = row["created_on_oad"] == DBNull.Value ? 0.0 : Convert.ToDouble(row["created_on_oad"]),
                        SenderName = row["sender_name"]?.ToString() ?? "",
                        IconClass = row["icon_class"]?.ToString() ?? ""
                    });
                }

                // -----------------------------
                // 3. Load thread label (subject)
                // -----------------------------
                string sqlTitle = $"SELECT subject FROM msg.message_thread WHERE id = {threadId} LIMIT 1;";
                object? title = App.Database.DataAccessManager.ExecuteScalar(Schema, sqlTitle, []);
                threadDto.Label = title?.ToString() ?? "(no subject)";

                // -----------------------------
                // 4. Mark inbound unread messages as read
                // -----------------------------
                string sqlMarkRead = @$"
                UPDATE msg.message
                SET is_read = true,
                    read_on_oad = {DateTime.Now.ToOADate()}
                WHERE thread_id = {threadId}
                AND recipient_user_id = {userId}
                AND is_read = false;";
                App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlMarkRead, []);

                // -----------------------------
                // 5. Push thread to caller
                // -----------------------------
                await Clients.Caller.MessageThreadOpened(threadDto);
            }
            catch (Exception ex)
            {
                await Clients.Caller.MessageError(new MessageErrorDto
                {
                    Code = "THREAD_OPEN_FAILED",
                    Message = ex.Message
                });
            }
        }


        // Socket version of ThreadPoll.cshtml.cs.
        // afterMessageId <= 0:
        //  - only send lastMessageId (MessageThreadDelta with empty messages)
        // afterMessageId > 0:
        //  - send new messages as MessageThreadDelta
        //  - mark inbound as read
        public async Task RequestThreadDelta(long threadId, long afterMessageId)
        {
            try
            {
                if (Context.User == null || !Context.User.IsAuthorised())
                {
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "UNAUTHORIZED",
                        Message = "Unauthorized"
                    });
                    return;
                }

                if (threadId <= 0)
                {
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "INVALID_REQUEST",
                        Message = "Invalid thread."
                    });
                    return;
                }

                long userId = Context.User.Id();

                // -----------------------------
                // 1. Ensure thread visible for this user
                // -----------------------------
                string sqlVisible = @$"
            SELECT COUNT(*) 
            FROM msg.message_thread_user 
            WHERE user_id = {userId} 
              AND thread_id = {threadId} 
              AND is_hidden = false;";

                long visible = Convert.ToInt64(
                    App.Database.DataAccessManager.ExecuteScalar(Schema, sqlVisible, [])
                );

                if (visible == 0)
                {
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "ACCESS_DENIED",
                        Message = "Access denied to thread."
                    });
                    return;
                }

                // -----------------------------
                // 2. No baseline: return only lastMessageId (no messages)
                // -----------------------------
                if (afterMessageId <= 0)
                {
                    string sqlLast = @$"
                SELECT id 
                FROM msg.message
                WHERE thread_id = {threadId}
                  AND (sender_user_id = {userId} OR recipient_user_id = {userId})
                ORDER BY id DESC
                LIMIT 1;";

                    object? obj = App.Database.DataAccessManager.ExecuteScalar(Schema, sqlLast, []);
                    long lastId = (obj == null || obj == DBNull.Value) ? 0L : Convert.ToInt64(obj);

                    var delta = new MessageThreadDeltaDto
                    {
                        ThreadId = threadId,
                        LastMessageId = lastId,
                        Messages = new List<MessageItemDto>()
                    };

                    await Clients.Caller.MessageThreadDelta(delta);
                    return;
                }

                // -----------------------------
                // 3. Pull only new messages (id > afterMessageId)
                // -----------------------------
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
              AND m.id > {afterMessageId}
            ORDER BY m.created_on_oad ASC;";

                DataTable dt = App.Database.DataAccessManager.GetDataTable(Schema, sql, []);

                var messages = new List<MessageItemDto>();
                long newLastId = afterMessageId;

                foreach (DataRow dr in dt.Rows)
                {
                    long mid = dr["id"] == DBNull.Value ? 0L : Convert.ToInt64(dr["id"]);
                    if (mid > newLastId) newLastId = mid;

                    double sentOad = 0.0;
                    if (dr["created_on_oad"] != DBNull.Value)
                        sentOad = Convert.ToDouble(dr["created_on_oad"]);

                    messages.Add(new MessageItemDto
                    {
                        Id = mid,
                        ThreadId = dr["thread_id"] == DBNull.Value ? 0L : Convert.ToInt64(dr["thread_id"]),
                        SenderName = dr["sender_name"]?.ToString() ?? string.Empty,
                        Text = dr["message_html"]?.ToString() ?? string.Empty,
                        IconClass = dr["icon_class"]?.ToString() ?? "fa-comments",
                        SentOad = sentOad,
                        IsMine = dr["is_mine"] != DBNull.Value && Convert.ToBoolean(dr["is_mine"]),
                        IsUnread = dr["unread"] != DBNull.Value && Convert.ToBoolean(dr["unread"])
                    });
                }

                // -----------------------------
                // 4. Mark new inbound messages as read
                // -----------------------------
                if (newLastId > afterMessageId)
                {
                    string readOad = DateTime.UtcNow
                        .ToOADate()
                        .ToString(System.Globalization.CultureInfo.InvariantCulture);

                    string sqlMarkRead = @$"
                UPDATE msg.message
                   SET is_read = true,
                       read_on_oad = {readOad}
                 WHERE thread_id = {threadId}
                   AND recipient_user_id = {userId}
                   AND is_read = false
                   AND id > {afterMessageId};";

                    App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlMarkRead, []);
                }

                // -----------------------------
                // 5. Send delta
                // -----------------------------
                var threadDelta = new MessageThreadDeltaDto
                {
                    ThreadId = threadId,
                    LastMessageId = newLastId,
                    Messages = messages
                };

                await Clients.Caller.MessageThreadDelta(threadDelta);
            }
            catch (Exception ex)
            {
                await Clients.Caller.MessageError(new MessageErrorDto
                {
                    Code = "THREAD_DELTA_FAILED",
                    Message = ex.Message
                });
            }
        }

        public async Task SendQuickMessage(long jobId, string message)
        {
            #region Validate
            if (Context.User == null || !Context.User.IsAuthorised())
            {
                await Clients.Caller.MessageError(new MessageErrorDto
                {
                    Code = "UNAUTHORIZED",
                    Message = "Unauthorized"
                });
                return;
            }
            if (jobId <= 0)
            {
                await Clients.Caller.MessageError(new MessageErrorDto
                {
                    Code = "INVALID_REQUEST",
                    Message = "Invalid job."
                });
                return;
            }
            string raw = (message ?? string.Empty).Trim();
            if (raw.Length == 0)
            {
                await Clients.Caller.MessageError(new MessageErrorDto
                {
                    Code = "EMPTY_MESSAGE",
                    Message = "Message is empty."
                });
                return;
            }
            #endregion

            try
            {
                long senderId = Context.User.Id();

                // 1) Ensure job exists and resolve recipient
                using DataTable jobDt = App.Database.DataAccessManager.GetDataTable(App.Database.Schema.Operations.Name, App.Database.Schema.Operations.Tables.Job, $"id = {jobId}");
                if (jobDt == null || jobDt.Rows.Count == 0)
                {
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "JOB_NOT_FOUND",
                        Message = "Job not found."
                    });
                    return;
                }

                long recipientId = Convert.ToInt64(jobDt.Rows[0]["user_id"]);
                var ok = App.Messages.SendMessage.Execute(senderId, jobId, raw);
                if (!ok)
                {
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "INSERT_FAILED",
                        Message = "Failed to insert message."
                    });
                    return;
                }

                // 3) Fetch most-recent message for this job (the one we just inserted)
                using DataTable dt = Database.DataAccessManager.GetDataTable(Database.Schema.Messaging.Name, Database.Schema.Messaging.Tables.Message, $"job_id={jobId}", "id DESC");
                if (dt == null || dt.Rows.Count == 0)
                {
                    // Shouldn't happen but handle gracefully
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "MSG_NOT_FOUND",
                        Message = "Inserted message not found."
                    });
                    return;
                }

                var row = dt.Rows[0];
                long messageId = row["id"] == DBNull.Value ? 0L : Convert.ToInt64(row["id"]);
                long threadId = row["thread_id"] == DBNull.Value ? 0L : Convert.ToInt64(row["thread_id"]);
                double createdOad = row["created_on_oad"] == DBNull.Value ? 0.0 : Convert.ToDouble(row["created_on_oad"]);
                string iconClass = row["icon_class"]?.ToString() ?? "fa-comment";
                string text = row["message_text"]?.ToString() ?? "";

                // 4) Resolve sender display name
                object? sn = App.Database.DataAccessManager.ExecuteScalar(Database.Schema.Entities.Name, $"SELECT fullname FROM {Database.Schema.Entities.Tables.Users} WHERE id = {senderId};", []);
                string senderName = (sn == null || sn == DBNull.Value) ? "You" : sn.ToString()!;

                // 5) Build DTOs (sender echo and recipient payload)
                MessageItemDto senderDto = new MessageItemDto {Id = messageId, ThreadId = threadId, SenderName = senderName, Text = text, IconClass = iconClass, SentOad = createdOad, IsMine = true, IsUnread = false};
                MessageItemDto recipientDto = new MessageItemDto {Id = messageId, ThreadId = threadId, SenderName = senderName, Text = text, IconClass = iconClass, SentOad = createdOad, IsMine = false, IsUnread = true};

                // 6) Push to sender (echo)
                await Clients.Caller.MessageSent(senderDto);

                // 7) Push to recipient if connected (use user mapping)
                //    Note: Clients.User expects the user identifier that IUserIdProvider exposes; your code uses recipientId as ent.users.id
                await Clients.User(recipientId.ToString()).MessageReceived(recipientDto);
            }
            catch (Exception ex)
            {
                await Clients.Caller.MessageError(new MessageErrorDto
                {
                    Code = "EXCEPTION",
                    Message = ex.Message
                });
            }
        }

        // Socket version of Send.cshtml.cs.
        // Should:
        //  - validate visibility
        //  - resolve recipient
        //  - insert message (is_read=false, ding=false)
        //  - update thread summary
        //  - unhide participants
        //  - send MessageSent to caller
        //  - send MessageReceived to recipient's group
        public async Task SendMessage(long threadId, string text)
        {
            // Socket version of Send.cshtml.cs
            if (Context.User == null || !Context.User.IsAuthorised())
            {
                await Clients.Caller.MessageError(new MessageErrorDto
                {
                    Code = "UNAUTHORIZED",
                    Message = "Unauthorized"
                });
                return;
            }

            if (threadId <= 0)
            {
                await Clients.Caller.MessageError(new MessageErrorDto
                {
                    Code = "INVALID_REQUEST",
                    Message = "Invalid thread."
                });
                return;
            }

            string raw = (text ?? string.Empty).Trim();
            if (raw.Length == 0)
            {
                await Clients.Caller.MessageError(new MessageErrorDto
                {
                    Code = "EMPTY_MESSAGE",
                    Message = "Message is empty."
                });
                return;
            }
            if (raw.Length > 2000)
                raw = raw.Substring(0, 2000);

            try
            {
                long userId = Context.User.Id();
                long threadIdLocal = threadId;

                // Ensure the sender has visibility on this thread
                string sqlCheck = $@"
            SELECT COUNT(*)
            FROM msg.message_thread_user
            WHERE user_id = {userId}
              AND thread_id = {threadIdLocal}
              AND is_hidden = false;";
                long visible = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar(Schema, sqlCheck, []));
                if (visible == 0)
                {
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "ACCESS_DENIED",
                        Message = "Access denied to this thread."
                    });
                    return;
                }

                // Resolve recipient (the other visible participant)
                string sqlRecipient = $@"
            SELECT user_id
            FROM msg.message_thread_user
            WHERE thread_id = {threadIdLocal}
              AND user_id <> {userId}
              AND is_hidden = false
            LIMIT 1;";
                object? ridObj = App.Database.DataAccessManager.ExecuteScalar(Schema, sqlRecipient, []);
                if (ridObj == null || ridObj == DBNull.Value)
                {
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "NO_RECIPIENT",
                        Message = "No recipient found in thread."
                    });
                    return;
                }
                long recipientId = Convert.ToInt64(ridObj);

                // Insert message: unread + undinged for recipient; capture created_on_oad
                string safeText = raw.Replace("'", "''");
                string sqlInsert = $@"
            INSERT INTO msg.message
                (thread_id, sender_user_id, recipient_user_id, message_text, is_read, ding)
            VALUES
                ({threadIdLocal}, {userId}, {recipientId}, '{safeText}', false, false)
            RETURNING id, created_on_oad;";
                DataTable ins = App.Database.DataAccessManager.GetDataTable(Schema, sqlInsert, []);
                if (ins.Rows.Count == 0)
                {
                    await Clients.Caller.MessageError(new MessageErrorDto
                    {
                        Code = "INSERT_FAILED",
                        Message = "Message insert failed."
                    });
                    return;
                }

                long messageId = Convert.ToInt64(ins.Rows[0]["id"]);
                double createdOad = Convert.ToDouble(ins.Rows[0]["created_on_oad"]);

                // Update thread summary using the same OAD as the message row (prevents drift)
                string sqlUpdateThread = $@"
            UPDATE msg.message_thread
               SET updated_on_oad = {createdOad},
                   last_message_id = {messageId},
                   last_sender_id = {userId},
                   last_recipient_id = {recipientId}
             WHERE id = {threadIdLocal};";
                App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlUpdateThread, []);

                // Touch both participants’ link rows and unhide
                string sqlTouchLinks = $@"
            UPDATE msg.message_thread_user
               SET is_hidden = false
             WHERE thread_id = {threadIdLocal}
               AND user_id IN ({userId}, {recipientId});";
                App.Database.DataAccessManager.ExecuteNonQuery(Schema, sqlTouchLinks, []);

                // Build DTO for sender
                object? sn = App.Database.DataAccessManager.ExecuteScalar(
                    "ent", $"SELECT fullname FROM ent.users WHERE id = {userId};", []);

                var senderDto = new MessageItemDto
                {
                    Id = messageId,
                    ThreadId = threadIdLocal,
                    SenderName = (sn == null || sn == DBNull.Value) ? "You" : sn.ToString()!,
                    Text = raw,
                    IconClass = "fa-comment",
                    SentOad = createdOad,
                    IsMine = true,
                    IsUnread = false
                };

                await Clients.Caller.MessageSent(senderDto);

                // Build DTO for recipient
                var recipientDto = new MessageItemDto
                {
                    Id = messageId,
                    ThreadId = threadIdLocal,
                    SenderName = (sn == null || sn == DBNull.Value) ? "You" : sn.ToString()!,
                    Text = raw,
                    IconClass = "fa-comment",
                    SentOad = createdOad,
                    IsMine = false,
                    IsUnread = true
                };

                // Push to recipient's user channel (assuming IUserIdProvider maps to ent.users.id)
                await Clients.User(recipientId.ToString()).MessageReceived(recipientDto);
            }
            catch (Exception ex)
            {
                await Clients.Caller.MessageError(new MessageErrorDto
                {
                    Code = "EXCEPTION",
                    Message = ex.Message
                });
            }
        }
    }
    public sealed class MessageThreadSummaryDto
    {
        public long ThreadId { get; set; }
        public string Label { get; set; } = string.Empty;       // "Support", "Customer X", etc.
        public string Preview { get; set; } = string.Empty;     // Last message preview
        public bool IsUnread { get; set; }
        public double LastMessageOad { get; set; }              // OADATE, from updated_on_oad / created_on_oad
    }
    public sealed class MessageThreadsSnapshotDto
    {
        public int UnreadCount { get; set; }                    // total unread messages for this user
        public bool ShouldDing { get; set; }                    // matches legacy ding logic
        public long LastThreadId { get; set; }                  // max thread id in this snapshot

        // For PWA: full HTML for the thread list, built by the same helper the Website uses.
        public string Html { get; set; } = string.Empty;

        // Optional: keep DTO list if you want later
        public List<MessageThreadSummaryDto> Threads { get; set; } = new();
    }
    public sealed class MessageThreadsDeltaDto
    {
        public long LastThreadId { get; set; }                  // new max thread id after applying delta
        public int UnreadCount { get; set; }
        public bool ShouldDing { get; set; }

        public List<MessageThreadSummaryDto> NewThreads { get; set; } = new();  // threads with id > oldLastThreadId
        public List<long> UnreadThreadIds { get; set; } = new();                // threads that now have unread
        public List<long> ReadThreadIds { get; set; } = new();                  // threads that now have 0 unread
    }
    public sealed class MessageItemDto
    {
        public long Id { get; set; }
        public long ThreadId { get; set; }

        public string Text { get; set; } = string.Empty;        // message_text from DB (NOT HTML)
        public bool IsMine { get; set; }                        // is_mine from the legacy query
        public bool IsUnread { get; set; }                      // unread flag before marking read

        public double SentOad { get; set; }                     // created_on_oad
        public string SenderName { get; set; } = string.Empty;  // sender_name
        public string IconClass { get; set; } = string.Empty;   // icon_class (if you still want it)
    }
    public sealed class MessageThreadDto
    {
        public long ThreadId { get; set; }
        public string Label { get; set; } = string.Empty;       // subject / contact label
        public long LastMessageId { get; set; }

        public List<MessageItemDto> Messages { get; set; } = new();
    }
    public sealed class MessageThreadDeltaDto
    {
        public long ThreadId { get; set; }
        public long LastMessageId { get; set; }
        public List<MessageItemDto> Messages { get; set; } = new(); // new messages only (ThreadPoll behaviour)
    }
    public sealed class MessageContactDto
    {
        public string Id { get; set; } = string.Empty;          // "support" or numeric string for user id
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;        // icon code from Contacts endpoint
    }
    public sealed class MessageContactsSnapshotDto
    {
        public List<MessageContactDto> Contacts { get; set; } = new();
    }
    public sealed class MessageThreadCreatedDto
    {
        public long ThreadId { get; set; }
        public string Label { get; set; } = string.Empty;       // contact/display name
        public bool HasRecipient { get; set; }                  // true if other user exists, false for "system-only"
    }
    public sealed class MessageErrorDto
    {
        public string Code { get; set; } = string.Empty;        // e.g. "ACCESS_DENIED", "INVALID_CONTACT"
        public string Message { get; set; } = string.Empty;     // user-friendly error
    }
}