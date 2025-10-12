using System.Data;

namespace Website.App.Database.Locations.Tables
{
    internal class Region
    {
        internal static object LockRehydrate = new();
        private static readonly DataTable DT = new();
        internal static int GetId(int countryId, string regionName, string regionCode)
        {
            if (DT.Rows.Count == 0)
            {
                lock (LockRehydrate)
                {
                    if (DT.Rows.Count == 0) // double-check inside
                        RehydrateDT();
                }

            }

            DataRow[] DR = DT.Select($"COUNTRY_ID={countryId} AND NAME={App.Database.Shared.SafeReplace(regionName)} AND SHORTCODE={App.Database.Shared.SafeReplace(regionCode)}");
            if (DR.Length > 0) return Convert.ToInt32(DR[0]["ID"]);

            lock (LockRehydrate)
            {
                Insert(countryId, regionName, regionCode);
                RehydrateDT();
            }
            DR = DT.Select($"COUNTRY_ID={countryId} AND NAME={App.Database.Shared.SafeReplace(regionName)} AND SHORTCODE={App.Database.Shared.SafeReplace(regionCode)}");
            if (DR.Length > 0) return Convert.ToInt32(DR[0]["ID"]);

            return -1;
        }
        private static void RehydrateDT()
        {
            using DataTable newDT = DataAccessManager.GetDataTable(Database.Schema.Locations.Database, Schema.Locations.Tables.Region);
            lock (LockRehydrate)
            {
                DT.Clear();
                DT.Merge(newDT);
            }
        }
        internal static void Insert(int countryId, string regionName, string regionShortCode)
        {
            double today = DateTime.UtcNow.ToOADate();
            string sql = "INSERT OR IGNORE INTO REGION(COUNTRY_ID, NAME, SHORTCODE, CREATEDOADATE, UPDATEDOADATE) ";
            sql += $"VALUES({countryId}, {Shared.SafeReplace(regionName)}, {Shared.SafeReplace(regionShortCode)}, {today}, {today})";

            _ = DataAccessManager.ExecuteNonQuery(Schema.Locations.Database, sql, []);
        }
    }
}
