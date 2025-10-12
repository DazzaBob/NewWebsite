using System.Data;
using System.Xml.Linq;
using Website.App.Mapbox.GeoCoding.V6;

namespace Website.App.Database.Locations.Tables
{
    internal static class Place
    {
        internal static object LockRehydrate = new();
        private readonly static DataTable PlaceDT = new();

        internal static DataTable DataTable()
        {
            if (PlaceDT.Rows.Count == 0)
            {
                lock (LockRehydrate)
                {
                    if (PlaceDT.Rows.Count == 0)
                        RehydrateDT();
                }
            }
            return PlaceDT;
        }
        internal static int GetId(int regionId, string countryName, string placeName)
        {
            if (PlaceDT.Rows.Count == 0)
            {
                lock (LockRehydrate)
                {
                    if (PlaceDT.Rows.Count == 0)
                        RehydrateDT();
                }
            }

            DataRow[] rows = PlaceDT.Select($"REGION_ID={regionId} AND NAME={Shared.SafeReplace(placeName)}");
            if (rows.Length > 0) return Convert.ToInt32(rows[0]["ID"]);

            lock (LockRehydrate)
            {
                Insert(regionId, countryName, placeName);
                RehydrateDT();
            }

            rows = PlaceDT.Select($"REGION_ID={regionId} AND NAME={Shared.SafeReplace(placeName)}");
            if (rows.Length > 0) return Convert.ToInt32(rows[0]["ID"]);

            return -1;
        }
        private static void RehydrateDT()
        {
            using DataTable newDT = DataAccessManager.GetDataTable(Schema.Locations.Database, Schema.Locations.Tables.Place);
            lock (LockRehydrate)
            {
                PlaceDT.Clear();
                PlaceDT.Merge(newDT);
            }
        }
        internal static void Insert(int regionId, string countryName, string placeName)
        {
            string inputAddress = $"{placeName} {countryName}".Trim();
            if (string.IsNullOrWhiteSpace(inputAddress)) return;

            GeoDeserializer GD = new();
            ResolvedAddressFeature? RAF = GD.Search(inputAddress);

            // Helpers to safely format numbers or NULL
            static string SqlVal(double? val) => val.HasValue ? val.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "NULL";

            string lat = SqlVal(RAF?.Place?.Coords?.DisplayLatitude);
            string lon = SqlVal(RAF?.Place?.Coords?.DisplayLongitude);
            string minLat = SqlVal(RAF?.Place?.BBox?.MinLatitude);
            string maxLat = SqlVal(RAF?.Place?.BBox?.MaxLatitude);
            string minLon = SqlVal(RAF?.Place?.BBox?.MinLongitude);
            string maxLon = SqlVal(RAF?.Place?.BBox?.MaxLongitude);

            string sql = "INSERT OR IGNORE INTO PLACE (REGION_ID, NAME, LATITUDE, LONGITUDE, MIN_LATITUDE, MAX_LATITUDE, MIN_LONGITUDE, MAX_LONGITUDE) ";
            sql += $"VALUES ({regionId}, {Shared.SafeReplace(placeName)}, {lat}, {lon}, {minLat}, {maxLat}, {minLon}, {maxLon})";
            _ = DataAccessManager.ExecuteNonQuery(Schema.Locations.Database, sql, []);
        }
    }
}
