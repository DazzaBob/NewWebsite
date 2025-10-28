using System.Data;
using Website.App.Database;

namespace Website.App.Operations.Zoning
{
    /// <summary>
    /// Provides validation and maintenance logic for spatial zoning data.
    /// Works with ZONES_BASE and RCI overlays, including:
    /// - Checking if a point is inside a base zone
    /// - Calculating bounding box offsets
    /// - Converting distances to latitude/longitude offsets
    /// </summary>
    internal partial class ZoneManager
    {
        private static class ZoneValidation
        {
            private static readonly double EarthRadiusM = 6_378_137; // meters
            private static readonly string LocationsSchema = Schema.Locations.SchemaName;

            /// <summary>
            /// Finds the first BASE zone containing the given coordinates.
            /// Returns 0 if none found.
            /// </summary>
            internal static int FindBaseZoneID(double longitude, double latitude, int placeId, int? localityId)
            {
                string localityFilter;
                if (localityId.HasValue && localityId > 0)
                    localityFilter = $" AND LOCALITY_ID = {localityId}";
                else
                    localityFilter = " AND LOCALITY_ID IS NULL";

                using DataTable dt = DataAccessManager.GetDataTable(LocationsSchema, Schema.Locations.ZonesBase, $"PLACE_ID = {placeId}{localityFilter} AND " +
                    $"MIN_LATITUDE <= {latitude} AND MAX_LATITUDE >= {latitude} AND " +
                    $"MIN_LONGITUDE <= {longitude} AND MAX_LONGITUDE >= {longitude}"
                );

                if (dt == null || dt.Rows.Count == 0) return 0;
                try
                {
                    return Convert.ToInt32(dt.Rows[0]["ID"]);
                }
                catch (Exception ex)
                {
                    Bootstrap.Logger?.Add($"ZoneValidation.FindBaseZoneID: {ex}", Helper.Logger.LogLevel.Error);
                    return 0;
                }
            }

            /// <summary>
            /// Checks if a point is within the bounding box of a zone.
            /// </summary>
            internal static bool IsPointInZone(double longitude, double latitude, DataRow zoneRow)
            {
                if (zoneRow == null) return false;

                double minLat = Convert.ToDouble(zoneRow["MIN_LATITUDE"]);
                double maxLat = Convert.ToDouble(zoneRow["MAX_LATITUDE"]);
                double minLon = Convert.ToDouble(zoneRow["MIN_LONGITUDE"]);
                double maxLon = Convert.ToDouble(zoneRow["MAX_LONGITUDE"]);

                return latitude >= minLat && latitude <= maxLat && longitude >= minLon && longitude <= maxLon;
            }

            /// <summary>
            /// Converts a radius in meters to latitude offset.
            /// </summary>
            public static double RadiusToLatitudeOffset(double radiusMeters) =>
                (radiusMeters / EarthRadiusM) * (180.0 / Math.PI);

            /// <summary>
            /// Converts a radius in meters to longitude offset at a given latitude.
            /// </summary>
            public static double RadiusToLongitudeOffset(double radiusMeters, double latitude)
            {
                double latRad = latitude * Math.PI / 180.0;
                double radiusAtLat = EarthRadiusM * Math.Cos(latRad);
                return (radiusMeters / radiusAtLat) * (180.0 / Math.PI);
            }
        }
    }
}
