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
            DataRow[] DR = DT.Select($"COUNTRY_ID={countryId} AND NAME={Shared.Sanitize(regionName, true)} AND SHORTCODE={Shared.Sanitize(regionCode, true)}");
            if (DR.Length > 0) return Convert.ToInt32(DR[0]["ID"]);
            lock (LockRehydrate)
            {
                Insert(countryId, regionName, regionCode);
                RehydrateDT();
            }
            DR = DT.Select($"COUNTRY_ID={countryId} AND NAME={Shared.Sanitize(regionName, true)} AND SHORTCODE={Shared.Sanitize(regionCode, true)}");
            if (DR.Length > 0) return Convert.ToInt32(DR[0]["ID"]);

            return -1;
        }
        private static void RehydrateDT()
        {
            using DataTable newDT = DataAccessManager.GetDataTable(Database.Schema.Locations.SchemaName, Schema.Locations.Region);
            lock (LockRehydrate)
            {
                DT.Clear();
                DT.Merge(newDT);
            }
        }
        internal static void Insert(int countryId, string regionName, string regionShortCode)
        {
            double today = DateTime.UtcNow.ToOADate();
            string fields = "COUNTRY_ID, NAME, SHORTCODE, CREATEDOADATE, UPDATEDOADATE";
            string values = $"{countryId}, {Shared.Sanitize(regionName, true)}, {Shared.Sanitize(regionShortCode, true)}, {today}, {today}";

            _ = DataAccessManager.Insert(Schema.Locations.SchemaName, Schema.Locations.Region, fields, values);
        }
    }
}
