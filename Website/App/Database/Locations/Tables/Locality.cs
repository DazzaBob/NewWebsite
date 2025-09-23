using System.Data;
using Website.App.Mapbox.GeoCoding.V6;

namespace Website.App.Database.Locations.Tables
{
    internal static class Locality
    {
        internal static object LockRehydrate = new();
        internal static DataTable LocalityDT = new();
        internal static DataTable DataTable(Helper.Connection connection)
        {
            if (LocalityDT.Rows.Count == 0)
            {
                lock (LockRehydrate)
                {
                    if (LocalityDT.Rows.Count == 0)
                        RehydrateDT(connection);
                }
            }
            return LocalityDT;
        }

        internal static int GetId(int placeId, string countryName, string placeName, string localityName)
        {
            using Helper.Connection connection = Shared.Connection(Schema.Locations.Database);

            if (LocalityDT.Rows.Count == 0)
            {
                lock (LockRehydrate)
                {
                    if (LocalityDT.Rows.Count == 0)
                        RehydrateDT(connection);
                }
            }

            DataRow[] rows = LocalityDT.Select($"PLACE_ID={placeId} AND NAME={Shared.SafeReplace(localityName)}");
            if (rows.Length > 0) return Convert.ToInt32(rows[0]["ID"]);

            lock (LockRehydrate)
            {
                Insert(connection, placeId, countryName, placeName, localityName);
                RehydrateDT(connection);
            }

            rows = LocalityDT.Select($"PLACE_ID={placeId} AND NAME={Shared.SafeReplace(localityName)}");
            if (rows.Length > 0) return Convert.ToInt32(rows[0]["ID"]);

            return -1;
        }

        private static void RehydrateDT(Helper.Connection connection)
        {
            DataTable newDT = Shared.GetDataTable(connection, "LOCALITY");
            lock (LockRehydrate)
            {
                LocalityDT.Clear();
                LocalityDT.Merge(newDT);
            }
        }
        internal static void Insert(Helper.Connection connection, int placeId, string countryName, string placeName, string localityName)
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

            connection.ExecuteNonQuery($@"INSERT OR IGNORE INTO LOCALITY 
        (PLACE_ID, NAME, LATITUDE, LONGITUDE, MIN_LATITUDE, MAX_LATITUDE, MIN_LONGITUDE, MAX_LONGITUDE) 
        VALUES ({placeId}, {Shared.SafeReplace(localityName)}, {lat}, {lon}, {minLat}, {maxLat}, {minLon}, {maxLon})");
        }
    }
}
