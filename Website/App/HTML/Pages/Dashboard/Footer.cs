using System.Text;

namespace Website.App.HTML.Pages.Dashboard
{
    public static class Footer
    {
        public static string HTML(Helper.Connection EntitiesConnection, long UserId)
        {
            StringBuilder sb = new();
            sb.AppendLine("<footer class=\"dashboard-footer\">");
            // <!-- 1. Home -->
            sb.Append("<a href=\"/Dashboard\" class=\"nav-btn active\">")
            .Append("<i class=\"icon-home\"></i>")
            .Append("<span>Home</span>")
            .AppendLine("</a>");

            // <!-- 2. Dynamic center slot -->
            // @if(Model.HasOrders)
            //{
            //< a asp - page = "/User/Orders" class="nav-btn @(path == "/user/orders" ? "active" : "")">
            // <i class="icon-list"></i>
            // <span>Orders</span>
            // </a>
            // }

            // Switch context. Customer/Vendor/Driver (We might need some JS to update this.
            sb.Append("<a href=\"@Model.DefaultIntentDashboard\" class=\"nav-btn\">")
            .Append("<i class=\"icon-briefcase\"></i>")
            .Append("<span>Order Now!</span>")
            .AppendLine("</a>");

            // < !--3.Profile-- >
            sb.Append("<a href=\"/User/Profile\" class=\"nav-btn active\">")
            .Append("<i class=\"icon-user\"></i>")
            .Append("<span>Profile</span>")
            .AppendLine("</a>");

            // < !--4.Tools-- >
            sb.Append("<a href=\"/User/Tools\" class=\"nav-btn active\">")
            .Append("<i class=\"icon-wrench\"></i>")
            .Append("<span>Tools</span>")
            .AppendLine("</a>");

            sb.AppendLine("</footer>");

            return sb.ToString();
        }
    }
}
