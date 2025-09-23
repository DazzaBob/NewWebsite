using System.Data;

namespace Website.App.Database.Locations.Tables
{
    internal static class Country
    {
        internal static object LockRehydrate = new();
        internal static DataTable DT = new();
        internal static int GetId(string countryName)
        {
            using Helper.Connection Connection = Shared.Connection(Schema.Locations.Database);
            if (DT.Rows.Count == 0)
            {
                lock (LockRehydrate)
                {
                    RehydrateDT(Connection);
                }

            }

            DataRow[] DR = DT.Select($"NAME={Shared.SafeReplace(countryName)}");
            if (DR.Length > 0) return Convert.ToInt32(DR[0]["ID"]);

            lock (LockRehydrate)
            {
                Insert(Connection, countryName);
                RehydrateDT(Connection);
            }
            DR = DT.Select($"NAME={Shared.SafeReplace(countryName)}");
            if (DR.Length > 0) return Convert.ToInt32(DR[0]["ID"]);

            return -1;
        }
        private static void RehydrateDT(Helper.Connection connection)
        {
            DataTable newDT = Shared.GetDataTable(connection, "COUNTRY");
            lock (LockRehydrate)
            {
                DT.Clear();
                DT.Merge(newDT);
            }
        }
        internal static void Insert(Helper.Connection Connection, string countryName)
        {
            double today = DateTime.Now.ToOADate();
            Connection.ExecuteNonQuery($"INSERT OR IGNORE INTO COUNTRY(NAME, CREATEDOADATE, UPDATEDOADATE) VALUES({App.Database.Shared.SafeReplace(countryName)}, {today}, {today} )");
        }
    }

}
