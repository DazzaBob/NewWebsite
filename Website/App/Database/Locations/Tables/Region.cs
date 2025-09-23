using System.Data;

namespace Website.App.Database.Locations.Tables
{
    internal class Region
    {
        internal static object LockRehydrate = new();
        internal static DataTable DT = new();
        internal static int GetId(int countryId, string regionName, string regionCode)
        {
            using Helper.Connection Connection = Shared.Connection(Schema.Locations.Database);
            if (DT.Rows.Count == 0)
            {
                lock (LockRehydrate)
                {
                    if (DT.Rows.Count == 0) // double-check inside
                        RehydrateDT(Connection);
                }

            }

            DataRow[] DR = DT.Select($"COUNTRY_ID={countryId} AND NAME={App.Database.Shared.SafeReplace(regionName)} AND SHORTCODE={App.Database.Shared.SafeReplace(regionCode)}");
            if (DR.Length > 0) return Convert.ToInt32(DR[0]["ID"]);

            lock (LockRehydrate)
            {
                Insert(Connection, countryId, regionName, regionCode);
                RehydrateDT(Connection);
            }
            DR = DT.Select($"COUNTRY_ID={countryId} AND NAME={App.Database.Shared.SafeReplace(regionName)} AND SHORTCODE={App.Database.Shared.SafeReplace(regionCode)}");
            if (DR.Length > 0) return Convert.ToInt32(DR[0]["ID"]);

            return -1;
        }
        private static void RehydrateDT(Helper.Connection connection)
        {
            DataTable newDT = Shared.GetDataTable(connection, "REGION");
            lock (LockRehydrate)
            {
                DT.Clear();
                DT.Merge(newDT);
            }
        }
        internal static void Insert(Helper.Connection connection, int countryId, string regionName, string regionShortCode)
        {
            double today = DateTime.Now.ToOADate();
            connection.ExecuteNonQuery($@"
            INSERT OR IGNORE INTO REGION(COUNTRY_ID, NAME, SHORTCODE, CREATEDOADATE, UPDATEDOADATE) 
            VALUES({countryId}, {App.Database.Shared.SafeReplace(regionName)}, {App.Database.Shared.SafeReplace(regionShortCode)}, {today}, {today})");
        }
    }
}
