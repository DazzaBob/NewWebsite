using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using System.Data;
using System.Text;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.notifications
{
    [IgnoreAntiforgeryToken]
    public class InitloadnotificationsModel : PageModel
    {
        // local record representing one notification row
        internal class NotificationItem
        {
            public long Id { get; set; }
            public string Type { get; set; } = "";
            public string SubType { get; set; } = "";
            public string Sender { get; set; } = "";
            public string Text { get; set; } = "";
            public string IconClass { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public bool Unread { get; set; }
        }
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost()
        {
            if (!User.IsAuthorised()) return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            try
            {
                List<NotificationItem> notifications = LoadNotifications();
                string html = App.StringBuilders.Pages.Notifications.BuildHtml(notifications);
                return new JsonResult(new { ok = true, html });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, message = ex.Message });
            }
        }
        private List<NotificationItem> LoadNotifications()
        {
            List<NotificationItem> list = [];
            string sql = @$"SELECT n.id, n.type_code AS type, COALESCE(nt.name, '') AS subtype, COALESCE(u.fullname, 'System') AS sender, n.title AS text, n.created_on_oad, n.icon_class, NOT n.is_read AS unread 
            FROM msg.notification n LEFT JOIN msg.notification_type nt ON nt.code = n.type_code LEFT JOIN ent.users u ON u.id = n.sender_user_id 
            WHERE n.recipient_user_id = {User.Id()} 
            ORDER BY n.created_on_oad DESC 
            LIMIT 50;";
            foreach (DataRow DR in App.Database.DataAccessManager.GetDataTable("pub", sql, []).Select(""))
            {
                var item = new NotificationItem
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
            return list;
        }
    }
}