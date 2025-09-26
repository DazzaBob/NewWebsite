using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
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


        public int UserId { get; private set; } = 0;
        public DataTable UserRolesDT { get; private set; } = new DataTable();
        public string? ErrorMessage { get; set; } = string.Empty;
        [ViewData]
        public string MapboxPublicToken { get; set; } = string.Empty;


        [BindProperty]
        [Required(ErrorMessage = "Please select a valid address type.")]

        [ValidateNever]
        public SelectList AddressTypeOptions { get; set; } = new(Enumerable.Empty<SelectListItem>());
        public int AddressTypeID { get; set; }
        [BindProperty(SupportsGet = false)]
        [ValidateNever]

        public string AddressSearch { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please select a valid address.")]
        public required string MapboxAddressJSON { get; set; }

        public IActionResult OnGet()
        {
            if (!User?.Identity?.IsAuthenticated ?? false) return RedirectToPage("/User/Login");

            UserId = User?.Id() ?? 0;
            if (UserId == 0) return RedirectToPage("/Logout");

            MapboxPublicToken = App.Settings.MapboxToken;
            var (list, error) = App.Helper.Table.AddressType.GetAddressTypeOptions(); // Load the address types for the dropdown
            AddressTypeOptions = list;
            ErrorMessage = error;

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
            Microsoft.Data.Sqlite.SqliteParameter[] roleParams = [new Microsoft.Data.Sqlite.SqliteParameter("@UserId", UserId)];
            UserRolesDT = connection.GetDataTable(sql, roleParams);

            return Page();
        }
        public DataTable GetLocationModalDataTable()
        {
            // 1. Fetch user addresses from Entities.db
            using App.Helper.Connection entityConn = App.Database.Shared.Connection(App.Database.Schema.Entities.Database);
            DataTable userAddresses = App.Database.Shared.GetDataTable(entityConn, App.Database.Schema.Entities.Tables.UserAddress, $"USER_ID={UserId}");

            if (userAddresses.Rows.Count == 0) return userAddresses; // nothing to join

            // 2. Build a comma-separated list of ADDRESS_IDs to fetch location info
            string addressIds = string.Join(",", userAddresses.Rows.Cast<DataRow>().Select(r => r["ADDRESS_ID"].ToString()));

            // 3. Fetch all locations in one query from Locations.db
            using App.Helper.Connection locationConn = App.Database.Shared.Connection(App.Database.Schema.Locations.Database);
            string sql = $@"SELECT a.ID AS ADDRESS_ID, a.STREET_NUMBER, a.STREET_NAME, l.NAME AS LOCALITY_NAME, p.NAME AS PLACE_NAME 
                FROM ADDRESS a LEFT JOIN LOCALITY l ON a.LOCALITY_ID = l.ID LEFT JOIN PLACE p ON a.PLACE_ID = p.ID 
                WHERE a.ID IN ({addressIds})";
            DataTable locationData = locationConn.GetDataTable(sql);

            // 4. Merge location info into userAddresses DataTable
            userAddresses.Columns.Add("STREET_NUMBER", typeof(string));
            userAddresses.Columns.Add("STREET_NAME", typeof(string));
            userAddresses.Columns.Add("LOCALITY_NAME", typeof(string));
            userAddresses.Columns.Add("PLACE_NAME", typeof(string));

            foreach (DataRow userRow in userAddresses.Rows)
            {
                int addrId = Convert.ToInt32(userRow["ADDRESS_ID"]);
                DataRow? locRow = locationData.Rows.Cast<DataRow>().FirstOrDefault(r => Convert.ToInt32(r["ADDRESS_ID"]) == addrId);

                if (locRow != null)
                {
                    userRow["STREET_NUMBER"] = locRow["STREET_NUMBER"];
                    userRow["STREET_NAME"] = locRow["STREET_NAME"];
                    userRow["LOCALITY_NAME"] = locRow["LOCALITY_NAME"];
                    userRow["PLACE_NAME"] = locRow["PLACE_NAME"];
                }
            }
            return userAddresses;
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
