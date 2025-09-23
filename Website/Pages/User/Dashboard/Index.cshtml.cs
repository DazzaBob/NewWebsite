using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Security;

namespace Website.Pages.User.Dashboard
{
    public class IndexModel : PageModel
    {
        // 1. Account Balance
        public decimal AccountBalance { get; private set; }

        // 2. Recent Activity
        public List<ActivityItem> RecentActivity { get; private set; } = [];

        // 3. Upcoming Tasks
        public List<TaskItem> UpcomingTasks { get; private set; } = [];

        // 4. Recommended Services
        public List<ServiceRecommendation> RecommendedServices { get; private set; } = [];

        // 5. Promotions & Offers
        public List<Promotion> ActivePromotions { get; private set; } = [];

        // 6. Recent Orders
        public List<OrderSummary> RecentOrders { get; private set; } = [];

        // 6a. Has Orders flag
        public bool HasOrders => RecentOrders?.Count > 0;

        // 7. Intent Switcher
        public bool HasDefaultIntent { get; private set; }
        public string DefaultIntentDashboard { get; private set; } = "/User/SelectIntent";
        public string DefaultIntentLabel { get; private set; } = "Select Intent";

        // 8. Notifications & Alerts
        public List<Notification> Notifications { get; private set; } = [];

        public DataTable UserRolesDT { get; private set; } = new DataTable();


        public IActionResult OnGet()
        {
            if (!User?.Identity?.IsAuthenticated ?? false) return RedirectToPage("/User/Login");

            var userID = User?.Id();
            if (userID == null)

                AccountBalance = 0.00m; //* fetch balance for userID
            RecentActivity = []; // fetch recent activity */
            UpcomingTasks = []; // fetch upcoming tasks */ 
            RecommendedServices = []; // fetch recommendations
            ActivePromotions = []; // fetch promotions
            RecentOrders = []; // fetch orders

            HasDefaultIntent = false; // determine default intent
            if (HasDefaultIntent)
            {
                DefaultIntentDashboard = ""; // e.g. "/Customer/Dashboard"
                DefaultIntentLabel = ""; // e.g. "Customer"
            }
            Notifications = []; // fetch notifications

            using App.Helper.Connection connection = App.Database.Shared.Connection(App.Database.Schema.Entities.Database);
            string sql = $@"SELECT ur.*, r.NAME, r.DESCRIPTION, r.ROUTE, r.ICONCLASS FROM USER_ROLES ur 
            INNER JOIN ROLES r ON ur.ROLE_ID = r.ID WHERE ur.USER_ID = @UserId AND ur.ISACTIVE = 1";
            Microsoft.Data.Sqlite.SqliteParameter[] roleParams = [new Microsoft.Data.Sqlite.SqliteParameter("@UserId", userID)];
            UserRolesDT = connection.GetDataTable(sql, roleParams);

            return Page();
        }

        public class ActivityItem
        {
            public DateTime Timestamp { get; set; }
            public string Description { get; set; } = string.Empty;
        }

        public class TaskItem
        {
            public DateTime DueDate { get; set; }
            public string Summary { get; set; } = string.Empty;
        }

        public class ServiceRecommendation
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class Promotion
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public DateTime ExpiresOn { get; set; }
        }

        public class OrderSummary
        {
            public int Id { get; set; }
            public string Number { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }

        public class Notification
        {
            public string Message { get; set; } = string.Empty;
        }
    }
}
