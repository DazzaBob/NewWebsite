using System.Data;
using Website.App.Mapbox.GeoCoding.V6;

namespace Website.App.Database.Locations.Tables
{
    internal static class Locality
    {
        internal static object LockRehydrate = new();
        private readonly static DataTable LocalityDT = new();
        internal static DataTable DataTable(Helper.Connection connection)
        {
            if (LocalityDT.Rows.Count == 0)
            {
                lock (LockRehydrate)
                {
                    if (LocalityDT.Rows.Count == 0)
                        RehydrateDT();
                }
            }
            return LocalityDT;
        }

        internal static int GetId(int placeId, string countryName, string placeName, string localityName)
        {
            if (LocalityDT.Rows.Count == 0)
            {
                lock (LockRehydrate)
                {
                    if (LocalityDT.Rows.Count == 0)
                        RehydrateDT();
                }
            }

            DataRow[] rows = LocalityDT.Select($"PLACE_ID={placeId} AND NAME={Shared.SafeReplace(localityName)}");
            if (rows.Length > 0) return Convert.ToInt32(rows[0]["ID"]);

            lock (LockRehydrate)
            {
                Insert(placeId, countryName, placeName, localityName);
                RehydrateDT();
            }

            rows = LocalityDT.Select($"PLACE_ID={placeId} AND NAME={Shared.SafeReplace(localityName)}");
            if (rows.Length > 0) return Convert.ToInt32(rows[0]["ID"]);

            return -1;
        }

        private static void RehydrateDT()
        {
            DataTable newDT = DataAccessManager.GetDataTable(Schema.Locations.Database, Schema.Locations.Tables.Locality);
            lock (LockRehydrate)
            {
                LocalityDT.Clear();
                LocalityDT.Merge(newDT);
            }
        }
        internal static void Insert(int placeId, string countryName, string placeName, string localityName)
        {
            string inputAddress = $"{localityName}, {placeName} {countryName}".Trim();
            if (string.IsNullOrWhiteSpace(inputAddress)) return;

            GeoDeserializer GD = new();
            ResolvedAddressFeature? RAF = GD.Search(inputAddress);

            // Helpers to safely format numbers or NULL
            static string SqlVal(double? val) => val.HasValue ? val.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "NULL";

            string lat = SqlVal(RAF?.Locality?.Coords?.DisplayLatitude);
            string lon = SqlVal(RAF?.Locality?.Coords?.DisplayLongitude);
            string minLat = SqlVal(RAF?.Locality?.BBox?.MinLatitude);
            string maxLat = SqlVal(RAF?.Locality?.BBox?.MaxLatitude);
            string minLon = SqlVal(RAF?.Locality?.BBox?.MinLongitude);
            string maxLon = SqlVal(RAF?.Locality?.BBox?.MaxLongitude);

            string sql = "INSERT OR IGNORE INTO LOCALITY (PLACE_ID, NAME, LATITUDE, LONGITUDE, MIN_LATITUDE, MAX_LATITUDE, MIN_LONGITUDE, MAX_LONGITUDE) ";
            sql += $"VALUES ({placeId}, {Shared.SafeReplace(localityName)}, {lat}, {lon}, {minLat}, {maxLat}, {minLon}, {maxLon})";
            _ = DataAccessManager.ExecuteNonQuery(Schema.Locations.Database, sql, []);
        }
    }
}
