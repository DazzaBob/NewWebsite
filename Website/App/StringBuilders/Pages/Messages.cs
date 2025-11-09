using System.Net;
using System.Text;
using Website.Pages.homefolder.content.endpoints.messages;

namespace Website.App.StringBuilders.Pages
{
    internal static class Messages
    {
        internal static string BuildInitialHTML(List<InitloadmessagesModel.MessageItem> messages)
        {
            if (messages == null || messages.Count == 0)
                return "<div class='empty-state'><p>No messages yet.</p></div>";

            // Group messages by day label
            var grouped = messages
                .GroupBy(m =>
                    m.CreatedAt.Date == DateTime.UtcNow.Date ? "Today" :
                    m.CreatedAt.Date == DateTime.UtcNow.AddDays(-1).Date ? "Yesterday" :
                    "Earlier")
                .ToDictionary(g => g.Key, g => g.ToList());

            StringBuilder sb = new();
            foreach (var group in grouped)
            {
                sb.Append("<div class='notif-group'>")
                  .Append($"<h4 class='notif-group-title'>{group.Key}</h4>");

                foreach (var m in group.Value.OrderBy(x => x.CreatedAt))
                {
                    string unreadClass = m.Unread ? "unread" : "";
                    string sideClass = m.IsMine ? "msg right" : "msg left";
                    string safeSender = WebUtility.HtmlEncode(m.SenderName);
                    string safeText = WebUtility.HtmlEncode(m.MessageHtml);
                    DateTime created = m.CreatedAt;
                    string formattedTime = created.ToString("ddd d MMM yyyy h:mm tt");
                    string icon = string.IsNullOrWhiteSpace(m.IconClass) ? "fa-comments" : m.IconClass;

                    sb.Append(BuildThreadHeaderHTML(m.Id.ToString(), unreadClass, sideClass, icon, safeSender, safeText, formattedTime));
                }

                sb.AppendLine("</div>");
            }

            return sb.ToString();
        }
        internal static string BuildThreadHeaderHTML(string threadId, string unreadClass, string sideClass, string icon, string sender, string text, string formattedTime)
        {
            var sb = new StringBuilder();
            sb.Append($"<div class='notif-item {unreadClass} {sideClass}' data-id='{threadId}' onclick=\"OpenMessage({threadId});\">")
              .Append($"<div class='notif-avatar'><i class='fa-solid {icon}' aria-hidden='true'></i></div>")
              .Append("<div class='notif-body'>")
              .Append($"<p><strong>{sender}</strong> {text}</p>")
              .Append($"<span class='notif-time'>{formattedTime}</span>")
              .Append("</div>")
              .Append($"<button type='button' class='btn btn-outline notif-del' ")
              .Append($"onclick='DeleteMessage({threadId}, this.closest(\".notif-item\")); event.stopPropagation();'>")
              .Append("<i class='fa-solid fa-trash'></i></button>")
              .Append("</div>");
            return sb.ToString();
        }
        internal static string BuildThreadHtml(List<InitloadmessagesModel.MessageItem> messages)
        {
            if (messages == null || messages.Count == 0)
                return "<div class='empty-thread'><p>No messages in this conversation yet.</p></div>";

            StringBuilder sb = new();

            sb.Append("<div class='chat-thread'>");

            foreach (var m in messages.OrderBy(x => x.CreatedAt))
            {
                sb.Append(BuildSingleMessageHtml(m));
            }

            sb.Append("</div>");
            return sb.ToString();
        }
        internal static string BuildSingleMessageHtml(InitloadmessagesModel.MessageItem message)
        {
            if (message == null)
                return string.Empty;
            string sideClass = message.IsMine ? "msg right" : "msg left";
            string safeSender = WebUtility.HtmlEncode(message.SenderName);
            string safeText = WebUtility.HtmlEncode(message.MessageHtml);
            string icon = string.IsNullOrWhiteSpace(message.IconClass) ? "fa-comments" : message.IconClass;
            string formattedTime = message.CreatedAt.ToString("h:mm tt");
            var sb = new StringBuilder();
            sb.Append($"<div class='notif-item {sideClass}' data-id='{message.Id}'>")
              .Append($"<div class='notif-avatar'><i class='fa-solid {icon}' aria-hidden='true'></i></div>")
              .Append("<div class='notif-body'>")
              .Append($"<p><strong>{safeSender}</strong> {safeText}</p>")
              .Append($"<span class='notif-time'>{formattedTime}</span>")
              .Append("</div>")
              .Append("</div>");
            return sb.ToString();
        }
    }
}