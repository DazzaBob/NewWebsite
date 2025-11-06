using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Text;
using Website.Pages.homefolder.content.endpoints.notifications;
using static System.Formats.Asn1.AsnWriter;

namespace Website.App.StringBuilders.Pages
{
    internal static class Notifications
    {
        internal static string BuildHtml(List<InitloadnotificationsModel.NotificationItem> notifications)
        {
            var grouped = notifications
                .GroupBy(n => n.CreatedAt.Date == DateTime.UtcNow.Date ? "Today" : n.CreatedAt.Date == DateTime.UtcNow.AddDays(-1).Date ? "Yesterday" : "Earlier")
                .ToDictionary(g => g.Key, g => g.ToList());

            StringBuilder sb = new();
            foreach (var group in grouped)
            {
                sb.Append("<div class='notif-group'>")
                  .Append($"<h4 class='notif-group-title'>{group.Key}</h4>");
                foreach (var n in group.Value.OrderByDescending(x => x.CreatedAt))
                {
                    string unreadClass = n.Unread ? "unread" : "";
                    string safeSender = WebUtility.HtmlEncode(n.Sender);
                    string safeText = WebUtility.HtmlEncode(n.Text);
                    DateTime created = n.CreatedAt;
                    string formattedTime = created.ToString("ddd d MMM yyyy h:mm tt");

                    sb.Append($"<div class='notif-item {unreadClass}' data-id='{n.Id}' data-type='{n.Type}' data-subtype='{n.SubType}' onclick='MarkNotificationRead({n.Id}, this);'>")
                      .Append($"<div class='notif-avatar system'><i class='fa-solid {n.IconClass}' aria-hidden='true'></i></div>")
                      .Append("<div class='notif-body'>")
                      .Append($"<p><strong>{safeSender}</strong> {safeText}</p>")
                      .Append($"<span class='notif-time'>{formattedTime}</span>")
                      .Append("</div>")
                      .Append($"<button type='button' class='btn btn-outline notif-del' onclick='DeleteNotification({n.Id}, this.closest(\".notif-item\")); event.stopPropagation();'><i class='fa-solid fa-trash'></i></button>")
                      .Append("</div>");
                }
                sb.AppendLine("</div>");
            }
            return sb.ToString();
        }
    }
}