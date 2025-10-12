using System.Data;

namespace Website.App.Database.Locations.Tables
{
    internal static class Country
    {
        internal static object LockRehydrate = new();
        private readonly static DataTable DT = new();
        internal static int GetId(string countryName)
        {
            if (DT.Rows.Count == 0)
            {
                lock (LockRehydrate)
                {
                    RehydrateDT();
                }

            }

            DataRow[] DR = DT.Select($"NAME={Shared.SafeReplace(countryName)}");
            if (DR.Length > 0) return Convert.ToInt32(DR[0]["ID"]);

            lock (LockRehydrate)
            {
                Insert(countryName);
                RehydrateDT();
            }
            DR = DT.Select($"NAME={Shared.SafeReplace(countryName)}");
            if (DR.Length > 0) return Convert.ToInt32(DR[0]["ID"]);

            return -1;
        }
        private static void RehydrateDT()
        {
            using DataTable newDT = DataAccessManager.GetDataTable(Schema.Locations.Database, Schema.Locations.Tables.Country);
            lock (LockRehydrate)
            {
                DT.Clear();
                DT.Merge(newDT);
            }
        }
        internal static void Insert(string countryName)
        {
            double today = DateTime.UtcNow.ToOADate();
            _ = App.Database.DataAccessManager.ExecuteNonQuery(Schema.Locations.Database, $"INSERT OR IGNORE INTO COUNTRY(NAME, CREATEDOADATE, UPDATEDOADATE) VALUES({Shared.SafeReplace(countryName)}, {today}, {today} )", []);
        }
    }

}
