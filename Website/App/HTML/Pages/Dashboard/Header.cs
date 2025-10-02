using System.Data;
using System.Text;

namespace Website.App.HTML.Pages.Dashboard
{
    public static class Header
    {
        public static string HTML(Helper.Connection EntitiesConnection, long UserId)
        {
            StringBuilder sb = new();
            sb.AppendLine("<header class=\"site-header\"><div class=\"header-container\">")
            .Append("<div class=\"logo\" style=\"flex:0 0 auto; margin-right:2rem;\">")
            .Append("<a href=\"/Index\"><img src=\"/assets/images/logo_96.png\" alt=\"\" height=\"40\" style=\"display:block;\" /></a>")
            .AppendLine("</div>");

            sb.Append("<nav class=\"nav-links\">")
            .Append("<a href=\"/User/Dashboard\">Dashboard</a>")
            .Append("<a href=\"/Account/Settings\">Settings</a>")
            .Append("<a href=\"/User/Logout\" class=\"btn btn-outline\">Logout</a>")
            .AppendLine("</nav>");

            sb.AppendLine("<script>")
            .AppendLine("window.addEventListener('pageshow', e => { if (e.persisted) location.reload(); });")
            .AppendLine("</script>");

            sb.Append("<div class=\"top-bar\">")
            .Append("<button class=\"menu-btn\" aria-label=\"Open Menu\" onclick=\"document.body.classList.toggle('menu-open')\">")
            .Append("<i class=\"fas fa-bars\" ></i>")
            .Append("</button>")
            .Append("<div class=\"title\">Dashboard</div>")
            .Append("<button class=\"settings-btn\" aria-label=\"Settings\">")
            .Append("<i class=\"fas fa-cog\"></i>")
            .Append("</button>")
            .AppendLine("</div>");

            sb.Append("<div class=\"pill-bar\">")
            .Append("<button id=\"addressToggle\" class=\"pill\" onclick=\"document.getElementById('addressmodal').classList.add('show');\">")
            .Append("<i class=\"fa-solid fa-home\"></i>");

            using DataTable DT = Database.Shared.GetDataTable(EntitiesConnection, Database.Schema.Entities.Tables.UserAddress, $"USER_ID={UserId} AND ISDEFAULT=1");
            var label = "Select Address";
            if (DT.Rows.Count > 0)
            {
                label = DT.Rows[0]["LABEL"] as string;
                if (!string.IsNullOrWhiteSpace(label)) label = label.Trim();
            }
            sb.Append(label)
            .Append("&nbsp;<i class=\"fa-solid fa-chevron-down dropdown-icon\"></i>");

            sb.Append("</button>");

            sb.Append("<button id=\"useCurrentLocation\" class=\"pill\" style=\"margin-left:auto\" onclick=\"OpenLocationModal();\">")
            .Append("<i class=\"fa-solid fa-map-marker-alt\"></i>Current location</button>")
            .AppendLine("</div>");

            sb.Append("<nav class=\"side-menu\" aria-label=\"Side navigation\">")
            .Append("<ul>")
            .Append("<li><a href=\"/User/Dashboard\"><i class=\"fa-solid fa-gauge\"></i>Dashboard</a></li>")
            .Append("<li><a href=\"/User/Dashboard/Orders\"><i class=\"fa-solid fa-list-check\"></i>Orders</a></li>")
            .Append("<li><a href=\"/User/Dashboard/Addresses\"><i class=\"fa-solid fa-location-dot\"></i>Addresses</a></li>")
            .Append("<li><a href=\"/User/Dashboard/Settings\"><i class=\"fa-solid fa-sliders\"></i>Settings</a></li>")
            .Append("<li><a href=\"/Legal/Terms\"><i class=\"fa-solid fa-scale-balanced\"></i>Terms</a></li>")
            .Append("<li><a href=\"/User/Logout\"><i class=\"fa-solid fa-right-from-bracket\"></i>Logout</a></li>")
            .Append("</ul>")
            .AppendLine("</nav>");

            sb.AppendLine("<div class=\"menu-backdrop\" aria-hidden=\"true\"></div>");
            sb.AppendLine("</div></header>");

            return sb.ToString();
        }
    }
}