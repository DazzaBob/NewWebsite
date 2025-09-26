using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Security;

namespace Website.Pages.User.Address.EndPoints
{
    public class ListPartialModel(IAddressService addressService) : PageModel
    {
        private readonly IAddressService _addressService = addressService;

        // Combined data from Entities + Locations
        public DataTable Addresses { get; private set; } = new DataTable();

        public IActionResult OnGet()
        {
            if (!User.IsAuthorised()) return BadRequest();

            // Disable caching
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";

            int userId = User.Id();
            Addresses = _addressService.GetFullAddressesDataTable(userId);

            return Page();
        }
    }

    public interface IAddressService
    {
        DataTable GetFullAddressesDataTable(int userId);
    }

    public sealed class AddressService : IAddressService
    {
        public DataTable GetFullAddressesDataTable(int userId)
        {
            // 1. Fetch user addresses from Entities.db
            using App.Helper.Connection entityConn = App.Database.Shared.Connection(App.Database.Schema.Entities.Database);
            DataTable userAddresses = App.Database.Shared.GetDataTable(entityConn, App.Database.Schema.Entities.Tables.UserAddress, $"USER_ID={userId}");

            if (userAddresses.Rows.Count == 0)
                return userAddresses; // nothing to join

            // 2. Build a comma-separated list of ADDRESS_IDs to fetch location info
            string addressIds = string.Join(",", userAddresses.Rows.Cast<DataRow>().Select(r => r["ADDRESS_ID"].ToString()));

            // 3. Fetch all locations in one query from Locations.db
            using App.Helper.Connection locationConn = App.Database.Shared.Connection(App.Database.Schema.Locations.Database);
            string sql = $@"SELECT a.ID AS ADDRESS_ID, a.STREET_NUMBER, a.STREET_NAME, l.NAME AS LOCALITY_NAME, p.NAME AS PLACE_NAME 
                FROM ADDRESS a LEFT JOIN LOCALITY l ON a.LOCALITY_ID = l.ID LEFT JOIN PLACE p ON a.PLACE_ID = p.ID 
                WHERE a.ID IN ({addressIds})
            ";
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
    }
}