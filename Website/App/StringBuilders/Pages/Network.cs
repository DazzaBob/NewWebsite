using System.Text;
using Website.App.Database;

namespace Website.App.StringBuilders.Pages
{
    public static class Network
    {
        // iconsHtml should contain any <i class="..."> tags for the user's roles
        public static string BuildNetworkRow(int userId, string name, string roles, string linkedOn, string initials, string iconsHtml)
        {
            var safeName = Shared.Sanitize(name, false, true);
            var safeRoles = Shared.Sanitize(roles, false, true);
            var safeTime = Shared.Sanitize(linkedOn, false, true);
            var safeInit = Shared.Sanitize(initials, false, true);

            var sb = new StringBuilder();
            sb.Append($"<div class='notif-item' style='display:flex;align-items:center;gap:.5rem;width:100%;' data-id='{userId}' onclick='RemoveLink_prompt({userId}, this);'>")
              .Append("<div class='notif-main' style='display:flex;align-items:center;gap:.5rem;flex:1;'>")
              .Append($"<div class='notif-avatar system' style='width:40px;height:40px;display:flex;align-items:center;justify-content:center;'><div class='avatar' style='width:36px;height:36px;border-radius:50%;display:flex;align-items:center;justify-content:center;background:rgba(255,255,255,.08);border:1px solid rgba(255,255,255,.25);font-weight:700;text-transform:uppercase;line-height:36px;font-size:.9rem;'>{safeInit}</div></div>")
              .Append("<div class='notif-body'>")
              .Append($"<p>{iconsHtml}<strong>{safeName}</strong> {safeRoles}</p>")
              .Append($"<span class='notif-time'>{safeTime}</span>")
              .Append("</div>")
              .Append("</div>")
              .Append("<button type='button' class='btn btn-outline notif-del' style='margin-left:auto;' ")
              .Append($"onclick='NETRLClick({userId}, this.closest(\".notif-item\")); event.stopPropagation();'>")
              .Append("<i class='fa-solid fa-trash'></i>")
              .Append("</button>")
              .Append("</div>");
            return sb.ToString();
        }
    }
}