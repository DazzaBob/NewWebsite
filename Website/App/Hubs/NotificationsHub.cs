using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Data;
using Website.App.Security;
using Website.App.StringBuilders.Pages;
using Website.Pages.homefolder.content.endpoints.notifications;

namespace Website.App.Hubs
{
    public interface INotificationsClient
    {
        Task NotificationsSnapshot(NotificationsSnapshotDto snapshot);
        Task NotificationChanged(NotificationChangedDto change);
    }

    [IgnoreAntiforgeryToken] // [Authorize] // PWA must be logged in
    public class NotificationsHub : Hub<INotificationsClient>
    {
        // Called by PWA client when notifications modal opens
        public async Task SubscribeNotifications()
        {
            long userId = GetCurrentUserId(); // TODO: same as existing endpoints

            // Optional: group per user (if you want multi-connection fanout)
            await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());

            // Initial snapshot – you decide what "initial" means when you implement
            var snapshot = await BuildSnapshotAsync(userId, isInitial: true);
            await Clients.Caller.NotificationsSnapshot(snapshot);
        }

        // Optional: explicitly unsubscribe, or do all cleanup in OnDisconnectedAsync
        public async Task UnsubscribeNotifications()
        {
            long? userId = GetCurrentUserIdOrNull();
            if (userId == null) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId.Value.ToString());
        }

        // Client requests a manual refresh (e.g. pull-to-refresh)
        public async Task RequestNotificationsSnapshot()
        {
            long userId = GetCurrentUserId(); // TODO: same as existing endpoints

            var snapshot = await BuildSnapshotAsync(userId, isInitial: false);
            await Clients.Caller.NotificationsSnapshot(snapshot);
        }

        // Mark one notification as read
        public async Task MarkNotificationRead(long notificationId)
        {
            long userId = GetCurrentUserId();

            string sql = $"UPDATE msg.notification SET is_read = true, ding = true WHERE id = {notificationId} AND recipient_user_id = {userId};";
            long affected = App.Database.DataAccessManager.ExecuteNonQuery("msg", sql, []);
            if (affected <= 0) return;

            int unreadCount = await GetUnreadCountAsync(userId);
            bool shouldDing = await ComputeShouldDingAsync(userId, isInitial: false);

            var change = new NotificationChangedDto
            {
                Type = "read",
                Item = null,
                Id = notificationId,
                UnreadCount = unreadCount,
                ShouldDing = shouldDing
            };

            await Clients.Group(userId.ToString()).NotificationChanged(change);
        }

        // Delete/clear one notification
        public async Task DeleteNotification(long notificationId)
        {
            long userId = GetCurrentUserId();

            string sql = $"DELETE FROM msg.notification WHERE id = {notificationId} AND recipient_user_id = {userId};";
            long affected = App.Database.DataAccessManager.ExecuteNonQuery("msg", sql, []);
            if (affected <= 0) return;

            int unreadCount = await GetUnreadCountAsync(userId);
            bool shouldDing = await ComputeShouldDingAsync(userId, isInitial: false);

            var change = new NotificationChangedDto
            {
                Type = "deleted",
                Item = null,
                Id = notificationId,
                UnreadCount = unreadCount,
                ShouldDing = shouldDing
            };

            await Clients.Group(userId.ToString()).NotificationChanged(change);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            long? userId = GetCurrentUserIdOrNull();
            if (userId != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId.Value.ToString());
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ----------------------------------------------------
        // Snapshot / poll skeleton – plug your existing logic in
        // ----------------------------------------------------
        private async Task<NotificationsSnapshotDto> BuildSnapshotAsync(long userId, bool isInitial)
        {
            // TODO: PORT from Initloadnotifications.cshtml.cs:
            //  - Load notifications for this user using the same SQL / method you already have.
            //  - Shape: List<InitloadnotificationsModel.NotificationItem>

            List<InitloadnotificationsModel.NotificationItem> items =
                await LoadNotificationsAsync(userId);

            // Use your existing builder – DO NOT change this:
            string html = Notifications.BuildHtml(items);

            // TODO: PORT from Poll.cshtml.cs:
            //  - unreadCount, shouldDing, ding update logic.
            int unreadCount = await GetUnreadCountAsync(userId);
            bool shouldDing = await ComputeShouldDingAsync(userId, isInitial);

            // If you want to keep Items purely for future DTO-based rendering, you can
            // map your NotificationItem rows into NotificationItemDto here, or leave it empty.
            var dtoItems = new List<NotificationItemDto>();

            return new NotificationsSnapshotDto
            {
                UnreadCount = unreadCount,
                ShouldDing = shouldDing,
                Html = html,
                Items = dtoItems
            };
        }
        // ----------------------------------------------------
        // PLACEHOLDER METHODS – you wire these to your DB
        // ----------------------------------------------------
        private Task<List<InitloadnotificationsModel.NotificationItem>> LoadNotificationsAsync(long userId)
        {
            List<InitloadnotificationsModel.NotificationItem> list = new();
            string sql = @$"SELECT n.id, n.type_code AS type, COALESCE(nt.name, '') AS subtype, COALESCE(u.fullname, 'System') AS sender, n.title AS text, n.created_on_oad, n.icon_class, NOT n.is_read AS unread 
            FROM msg.notification n LEFT JOIN msg.notification_type nt ON nt.code = n.type_code LEFT JOIN ent.users u ON u.id = n.sender_user_id 
            WHERE n.recipient_user_id = {userId} 
            ORDER BY n.created_on_oad DESC 
            LIMIT 50;";
            foreach (DataRow DR in App.Database.DataAccessManager.GetDataTable("pub", sql, []).Select(""))
            {
                var item = new InitloadnotificationsModel.NotificationItem
                {
                    Id = DR["id"] == DBNull.Value ? 0L : Convert.ToInt64(DR["id"]),
                    Type = DR["type"]?.ToString() ?? string.Empty,
                    SubType = DR["subtype"]?.ToString() ?? string.Empty,
                    Sender = DR["sender"]?.ToString() ?? string.Empty,
                    Text = DR["text"]?.ToString() ?? string.Empty,
                    IconClass = DR["icon_class"]?.ToString() ?? string.Empty,
                    CreatedAt = DR["created_on_oad"] == DBNull.Value ? DateTime.UtcNow : DateTime.FromOADate(Convert.ToDouble(DR["created_on_oad"])),
                    Unread = DR["unread"] != DBNull.Value && Convert.ToBoolean(DR["unread"])
                };
                list.Add(item);
            }
            return Task.FromResult(list);
        }
        private Task<int> GetUnreadCountAsync(long userId)
        {
            string sqlUnread = $"SELECT COUNT(*) FROM msg.notification WHERE recipient_user_id = {userId} AND is_read = false;";
            long unreadCount = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar("msg", sqlUnread, []));
            return Task.FromResult((int)unreadCount);
        }
        private Task<bool> ComputeShouldDingAsync(long userId, bool isInitial)
        {
            bool shouldDing;

            if (isInitial)
            {
                string sqlUnread = $"SELECT COUNT(*) FROM msg.notification WHERE recipient_user_id = {userId} AND is_read = false;";
                long unreadCount = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar("msg", sqlUnread, []));
                shouldDing = unreadCount > 0;
            }
            else
            {
                string sqlUndinged = $"SELECT COUNT(*) FROM msg.notification WHERE recipient_user_id = {userId} AND ding = false;";
                long undingedCount = Convert.ToInt64(App.Database.DataAccessManager.ExecuteScalar("msg", sqlUndinged, []));
                shouldDing = undingedCount > 0;
            }

            if (shouldDing)
            {
                string sqlUpdate = $"UPDATE msg.notification SET ding = true WHERE recipient_user_id = {userId} AND ding = false;";
                App.Database.DataAccessManager.ExecuteNonQuery("msg", sqlUpdate, []);
            }

            return Task.FromResult(shouldDing);
        }
        private long GetCurrentUserId()
        {
            // Mirror the auth check from your partials
            if (Context.User == null || !Context.User.IsAuthorised()) throw new HubException("Unauthorized");
            return Context.User.Id();
        }
        private long? GetCurrentUserIdOrNull()
        {
            if (Context.User == null || !Context.User.IsAuthorised()) return null;
            return Context.User.Id();
        }
    }

    // --------------------------------------------------------
    // DTOs – extended to carry HTML for the PWA notifications
    // --------------------------------------------------------
    public sealed class NotificationsSnapshotDto
    {
        public int UnreadCount { get; set; }
        public bool ShouldDing { get; set; }

        // Added so the hub can send your formatted notifications HTML to the PWA.
        public string Html { get; set; } = string.Empty;

        public List<NotificationItemDto> Items { get; set; } = new();
    }
    public sealed class NotificationItemDto
    {
        public long Id { get; set; }
        public bool IsUnread { get; set; }
        public double CreatedOad { get; set; }  // OADATE
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Category { get; set; }
        public long? JobId { get; set; }
    }
    public sealed class NotificationChangedDto
    {
        // "added" | "read" | "deleted" (you can extend this if needed)
        public string Type { get; set; } = string.Empty;

        // Optional – only needed if you ever want to push a new item’s data;
        // for "read"/"deleted" the client can just use Id.
        public NotificationItemDto? Item { get; set; }

        public long? Id { get; set; }
        public int UnreadCount { get; set; }
        public bool ShouldDing { get; set; }
    }
}
