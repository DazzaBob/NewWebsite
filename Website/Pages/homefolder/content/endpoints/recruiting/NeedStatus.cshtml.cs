using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Database;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.recruiting
{
    [IgnoreAntiforgeryToken]
    public class NeedStatusModel : PageModel
    {
        public sealed class Input
        {
            public int? ZoneBaseId { get; set; }
        }
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] Input? input)
        {
            if (!User.IsAuthorised()) return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = 401 };
            if (input == null) return new JsonResult(new { ok = false, msg = "Invalid payload." }) { StatusCode = 400 };
            try
            {
                int userId = User.Id();

                // Is THIS user an approved driver?
                bool driverApproved = false;
                try
                {
                    DataTable drv = DataAccessManager.GetDataTable(Schema.Entities.Name, Schema.Entities.Tables.Driver, $"id = {userId}", string.Empty);
                    if (drv.Rows.Count > 0)
                    {
                        var r = drv.Rows[0];
                        bool hasApproved = r["approved_oad"] != DBNull.Value;
                        bool withdrawn = r["approvalwithdrawn_oad"] != DBNull.Value;
                        driverApproved = hasApproved && !withdrawn;
                    }
                }
                catch
                {
                    driverApproved = false;
                }

                int zoneBaseId = input.ZoneBaseId ?? ResolveUserZoneBaseId(userId);
                if (zoneBaseId <= 0)
                    return new JsonResult(new { ok = true, show = false, reason = "no_zone", driverApproved });

                // Active drivers now (online_to_oad IS NULL)
                string sql = @$"SELECT COUNT(*) 
                FROM ent.driver d
                JOIN ent.user_address ua 
                  ON ua.user_id = d.id 
                 AND ua.isdefault = true 
                 AND ua.isactive = true
                JOIN loc.address a 
                  ON a.id = ua.address_id 
                WHERE a.zone_base_id = {zoneBaseId}
                  AND d.isactive = true
                  AND d.approved_oad IS NOT NULL
                  AND d.approvalwithdrawn_oad IS NULL
                  AND d.last_drove_oad >= {DateTime.UtcNow.ToOADate() - 30}";

                var activeObj = DataAccessManager.GetScalar("pub", sql); // use "pub" on cross schema queries.
                int active = ToInt(activeObj) ?? 0;

                // Capacity rows for this zone
                DataTable cap = DataAccessManager.GetDataTable(Schema.Operations.Name, Schema.Operations.Tables.ZoneCapacity, $"zone_base_id = {zoneBaseId}", string.Empty);
                if (cap.Rows.Count == 0) return new JsonResult(new { ok = true, show = false, reason = "no_capacity", driverApproved, zoneBaseId, stats = new { activeDrivers = active } });
                if (cap.Rows.Count > 1) return new JsonResult(new { ok = true, show = false, reason = "ambiguous_capacity", driverApproved, zoneBaseId, stats = new { activeDrivers = active, rows = cap.Rows.Count } });

                var row = cap.Rows[0];
                int target = ToInt(row["target"]) ?? 0;

                bool show = active <= target;
                string reason = show ? "recruiting" : "at_target";

                return new JsonResult(new { ok = true, show, reason, zoneBaseId, driverApproved, stats = new { activeDrivers = active, target } });
            }
            catch (Exception ex)
            {
                App.Bootstrap.Logger?.Add("NeedStatus: " + ex.Message, App.Helper.Logger.LogLevel.Error);
                return new JsonResult(new { ok = false, msg = "Internal server error." })
                { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
        private static int ResolveUserZoneBaseId(int userId)
        {
            // Default address → loc.address.zone_base_id
            var addrIdObj = DataAccessManager.GetScalar(Schema.Entities.Name, Schema.Entities.Tables.UserAddress, "address_id", $"user_id = {userId} AND isdefault = true", null);
            int? addrId = ToInt(addrIdObj);
            if (addrId is int aid && aid > 0)
            {
                var zbObj = DataAccessManager.GetScalar(Schema.Locations.Name, Schema.Locations.Tables.Address, "zone_base_id", $"id = {aid}", null);
                int? zb = ToInt(zbObj);
                if (zb is int z && z > 0) return z;
            }
            return 0;
        }
        private static int? ToInt(object? o)
        {
            if (o == null || o is DBNull) return null;
            if (o is int i) return i;
            if (o is long l) return checked((int)l);
            if (o is decimal m) return (int)m;
            if (int.TryParse(o.ToString(), out var v)) return v;
            return null;
        }
    }
}