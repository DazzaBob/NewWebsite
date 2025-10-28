using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Simplify;
using System.Data;
using Website.App.Database;

namespace Website.App.Operations.Zoning
{
    internal partial class ZoneManager
    {
        private static readonly double EarthRadiusM = 6_378_137; // meters
        /// <summary>
        /// Provides methods for seeding initial Base Zones for Places and Localities.
        /// 
        /// Base Zones are the foundational physical areas representing a Place or Locality. 
        /// They are initially rectangular but can evolve into multi-sided polygons as new
        /// addresses are added.
        /// 
        /// This class should **only be used for initial creation** of zones; dynamic reshaping 
        /// of Base Zones for new addresses is handled elsewhere (see <see cref="ZoneHelpers"/>).
        /// </summary>
        private static class ZoneSeeder
        {
            private static readonly GeometryFactory _gf = new(new PrecisionModel(), 4326);
            private static readonly GeoJsonWriter _gjson = new();

            private const double EarthRadiusMeters = 6378137;
            private static readonly string LocationsSchema = Schema.Locations.SchemaName;

            /// <summary>
            /// Creates a new base zone for a specified Place. 
            /// 
            /// The initial shape is a simple rectangle based on MIN/MAX latitude and longitude
            /// of the Place record. This rectangle is stored as GeoJSON in the ZonesBase table.
            /// 
            /// Returns the ID of the newly created Base Zone, or 0 if the Place ID is invalid.
            /// 
            /// Note: This method does not dynamically adjust the zone if new addresses fall outside.
            /// That logic is handled by <see cref="ZoneHelpers.AssignAddressZones"/>.
            /// </summary>
            internal static int CreatePlaceZoneID(int placeId)
            {
                if (placeId <= 0) ArgumentOutOfRangeException.ThrowIfLessThan(placeId, 1, nameof(placeId));

                DataRow[] DR = DataAccessManager.GetDataTable(LocationsSchema, Schema.Locations.Place, $"ID = {placeId}").Select();
                if (DR.Length == 0) return 0; // we couldnt find any Places that matched with the provided placeid, lets return 0; 

                string shapeJson = ShapeToGeoJson(
                    Convert.ToDouble(DR[0]["MIN_LATITUDE"]),
                    Convert.ToDouble(DR[0]["MAX_LATITUDE"]),
                    Convert.ToDouble(DR[0]["MIN_LONGITUDE"]),
                    Convert.ToDouble(DR[0]["MAX_LONGITUDE"])
                );

                string values = @$"{App.Database.Shared.Sanitize(DR[0]["NAME"], true)}, 
                    {placeId}, NULL, {DR[0]["LATITUDE"]}, {DR[0]["LONGITUDE"]}, 
                    {(DR[0]["MIN_LATITUDE"] == DBNull.Value ? "NULL" : DR[0]["MIN_LATITUDE"])}, 
                    {(DR[0]["MAX_LATITUDE"] == DBNull.Value ? "NULL" : DR[0]["MAX_LATITUDE"])}, 
                    {(DR[0]["MIN_LONGITUDE"] == DBNull.Value ? "NULL" : DR[0]["MIN_LONGITUDE"])}, 
                    {(DR[0]["MAX_LONGITUDE"] == DBNull.Value ? "NULL" : DR[0]["MAX_LONGITUDE"])}, 
                    {DateTime.UtcNow.ToOADate()}, {DateTime.UtcNow.ToOADate()}, '{shapeJson.Replace("'", "''")}'";

                string fields = "NAME, PLACE_ID, LOCALITY_ID, CENTEROID_LATITUDE, CENTEROID_LONGITUDE, MIN_LATITUDE, MAX_LATITUDE, MIN_LONGITUDE, MAX_LONGITUDE, CREATEDOADATE, UPDATEDOADATE, SHAPE_JSON";

                return DataAccessManager.Insert(LocationsSchema, Schema.Locations.ZonesBase, fields, values); // returns the INSERTed ID.
            }

            /// <summary>
            /// Creates a new base zone for a specified Locality within its parent Place.
            /// 
            /// The geometry is derived from the Locality bounding box. If it overlaps the parent 
            /// Place zone, only the portion outside the Place is retained; small overlaps are preserved.
            /// The resulting zone is simplified and stored in ZonesBase.
            /// 
            /// Returns the ID of the newly created Locality Base Zone, or 0 if the Place/Locality is invalid
            /// or the Locality does not belong to the specified Place.
            /// </summary>
            internal static int CreateLocalityZoneID(int placeId, int localityId)
            {
                if (placeId <= 0) ArgumentOutOfRangeException.ThrowIfLessThan(placeId, 1, nameof(placeId));
                if (localityId <= 0) ArgumentOutOfRangeException.ThrowIfLessThan(localityId, 1, nameof(localityId));

                using DataTable DT = DataAccessManager.GetDataTable(LocationsSchema, Schema.Locations.ZonesBase, $"PLACE_ID = {placeId} AND LOCALITY_ID IS NULL");
                if (DT.Rows.Count == 0)
                {
                    Bootstrap.Logger?.Add($"Could not create Locality Zone Id, PlaceId: {placeId} is invalid", Helper.Logger.LogLevel.Error);
                    return 0;
                }
                Geometry placePoly = new GeoJsonReader().Read<Geometry>(DT.Rows[0]["SHAPE_JSON"].ToString());

                // Load locality bbox
                DataRow[] DR = DataAccessManager.GetDataTable(LocationsSchema, Schema.Locations.Locality, $"ID = {localityId}").Select();
                if (DR.Length == 0)
                {
                    Bootstrap.Logger?.Add($"Could not create Locality Zone Id, Locality Id: {localityId} is invalid", Helper.Logger.LogLevel.Error);
                    return 0;
                }

                int localityPlaceId = Convert.ToInt32(DR[0]["PLACE_ID"]);

                // locality must belong to place
                if (localityPlaceId != placeId)
                {
                    Bootstrap.Logger?.Add($"Could not create Locality Zone Id, Locality Id, does not belong to PlaceID: {localityPlaceId}", Helper.Logger.LogLevel.Error);
                    return 0;
                }
                double minLat = Convert.ToDouble(DR[0]["MIN_LATITUDE"]);
                double maxLat = Convert.ToDouble(DR[0]["MAX_LATITUDE"]);
                double minLon = Convert.ToDouble(DR[0]["MIN_LONGITUDE"]);
                double maxLon = Convert.ToDouble(DR[0]["MAX_LONGITUDE"]);

                Envelope env = new(minLon, maxLon, minLat, maxLat);
                Geometry locPoly = _gf.ToGeometry(env);

                // Compute overlap
                Geometry intersection = locPoly.Intersection(placePoly);
                double overlapRatio = locPoly.IsEmpty ? 0.0 : (intersection.Area / locPoly.Area);

                // Determine final shape
                Geometry finalPoly;
                const double INSIDE_THRESHOLD = 0.6;
                if (overlapRatio >= INSIDE_THRESHOLD)
                {
                    finalPoly = locPoly;
                }
                else
                {
                    Geometry diff = locPoly.Difference(placePoly);
                    finalPoly = diff.IsEmpty ? locPoly : diff.Buffer(0.0001).Buffer(-0.0001);
                    finalPoly = DouglasPeuckerSimplifier.Simplify(finalPoly, 0.0005);
                }

                Envelope e = finalPoly.EnvelopeInternal;
                var centroid = finalPoly.Centroid;
                string geo = _gjson.Write(finalPoly);
                double now = DateTime.UtcNow.ToOADate();

                string values = @$"{Shared.Sanitize(DR[0]["NAME"], true)},
                    {placeId}, {localityId}, {centroid.Y}, {centroid.X}, 
                    {(double.IsNaN(e.MinY) ? "NULL" : e.MinY.ToString(System.Globalization.CultureInfo.InvariantCulture))}, 
                    {(double.IsNaN(e.MaxY) ? "NULL" : e.MaxY.ToString(System.Globalization.CultureInfo.InvariantCulture))}, 
                    {(double.IsNaN(e.MinX) ? "NULL" : e.MinX.ToString(System.Globalization.CultureInfo.InvariantCulture))}, 
                    {(double.IsNaN(e.MaxX) ? "NULL" : e.MaxX.ToString(System.Globalization.CultureInfo.InvariantCulture))},
                    {now}, {now}, '{geo.Replace("'", "''")}'";

                string fields = "NAME, PLACE_ID, LOCALITY_ID, CENTEROID_LATITUDE, CENTEROID_LONGITUDE, MIN_LATITUDE, MAX_LATITUDE, MIN_LONGITUDE, MAX_LONGITUDE, CREATEDOADATE, UPDATEDOADATE, SHAPE_JSON";

                return DataAccessManager.Insert(LocationsSchema, Schema.Locations.ZonesBase, fields, values); // returns the INSERTed ZONE_ID
            }
            internal static string ShapeToGeoJson(double minLat, double maxLat, double minLon, double maxLon)
            {
                var env = new Envelope(minLon, maxLon, minLat, maxLat);
                var rect = _gf.ToGeometry(env);
                return _gjson.Write(rect);
            }
        }
        /// <summary>
        /// Provides runtime helper methods for assigning addresses to the correct zones.
        /// 
        /// Responsibilities:
        /// 1. Determine the Base Zone containing a given address. If none exists, optionally
        ///    find the nearest Base Zone or create a temporary fallback.
        /// 2. Determine the RCI overlay zone based on the Base Zone and AddressType.
        /// 3. Update the Address record with both Base and RCI Zone IDs.
        /// 
        /// Note: Base Zones are physical areas; RCI Zones are type-specific overlays on top of
        /// Base Zones, e.g., for residential, commercial, or industrial classification.
        /// </summary>
        private static class ZoneHelpers
        {
            /// <summary>
            /// Assigns the given address to the appropriate Base and RCI zones at runtime.
            /// 
            /// Parameters:
            /// - addressId: the ID of the address being processed
            /// - latitude, longitude: coordinates of the address
            /// - placeId: the parent Place of the address
            /// - localityId: optional Locality of the address
            /// 
            /// Workflow:
            /// 1. Find the Base Zone containing the coordinates.
            /// 2. If the address is outside all Base Zones, assign a fallback or create a temporary zone.
            /// 3. Find the RCI zone overlay for the address within the Base Zone.
            /// 4. Update the Address record with ZONE_BASE_ID and ZONE_RCI_ID.
            /// </summary>
            internal static void AssignAddressZones(int addressId, int addressTypeId, double latitude, double longitude, int placeId, int? localityId)
            {
                int baseZoneId = ZoneValidation.FindBaseZoneID(longitude, latitude, placeId, localityId);

                // If outside base, expand or create temp zone (simplified example)
                if (baseZoneId == 0)
                {
                    // TODO: logic to create temp zone based on proximity to other addresses
                    // For now, assign to nearest place's pioneer zone
                    baseZoneId = FindNearestBaseZone(placeId, latitude, longitude);

                    // Assign RCI overlay
                }
                int rciZoneId = FindOrCreateRciZone(baseZoneId, addressTypeId, latitude, longitude);
                _ = DataAccessManager.Update(LocationsSchema, App.Database.Schema.Locations.Address, $"ZONE_BASE_ID = {baseZoneId}, ZONE_RCI_ID = {rciZoneId}", $"ID = {addressId}");
            }

            /// <summary>
            /// Returns the nearest Base Zone for a given coordinate when no containing zone exists.
            /// Uses haversine distance from the Base Zone centroid for proximity calculation.
            /// </summary>
            private static int FindNearestBaseZone(int placeId, double latitude, double longitude)
            {
                try
                {
                    // simplified nearest search using bounding boxes
                    using DataTable dt = DataAccessManager.GetDataTable(Schema.Locations.SchemaName, Schema.Locations.ZonesBase, $"PLACE_ID = {placeId}");
                    if (dt.Rows.Count == 0) return 0;

                    double minDistance = double.MaxValue;
                    int nearestId = 0;
                    foreach (DataRow row in dt.Rows)
                    {
                        double centerLat = Convert.ToDouble(row["CENTEROID_LATITUDE"]);
                        double centerLon = Convert.ToDouble(row["CENTEROID_LONGITUDE"]);
                        double dist = HaversineDistance(latitude, longitude, centerLat, centerLon);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            nearestId = Convert.ToInt32(row["ID"]);
                        }
                    }
                    return nearestId;
                }
                catch
                {
                    return 0;
                }
            }

            /// <summary>
            /// Finds an existing RCI zone for the given Base zone and AddressType containing the point,
            /// or creates a new one if missing.
            /// </summary>
            /// <param name="baseZoneId">ID of the Base zone.</param>
            /// <param name="addressTypeId">Address type ID (used for RCI overlay).</param>
            /// <param name="latitude">Latitude of the point.</param>
            /// <param name="longitude">Longitude of the point.</param>
            /// <returns>ID of the existing or newly created RCI zone.</returns>
            private static int FindOrCreateRciZone(int baseZoneId, int addressTypeId, double latitude, double longitude)
            {
                using DataTable dt = DataAccessManager.GetDataTable(LocationsSchema, Schema.Locations.ZonesRCI, $"BASE_ZONE_ID = {baseZoneId} AND ADDRESS_TYPE_ID = {addressTypeId}"
                );
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        // Use precise geometry check rather than bounding box only
                        if (ZoneValidation.IsPointInZone(longitude, latitude, row))
                            return Convert.ToInt32(row["ID"]);
                    }
                }

                // No existing RCI zone contains the point — create a new one
                const double defaultRadiusMeters = 50; // small initial radius
                double latOffset = ZoneValidation.RadiusToLatitudeOffset(defaultRadiusMeters);
                double lonOffset = ZoneValidation.RadiusToLongitudeOffset(defaultRadiusMeters, latitude);

                double minLat = latitude - latOffset;
                double maxLat = latitude + latOffset;
                double minLon = longitude - lonOffset;
                double maxLon = longitude + lonOffset;

                string shapeJson = ZoneSeeder.ShapeToGeoJson(minLat, maxLat, minLon, maxLon);
                double now = DateTime.UtcNow.ToOADate();

                string values = $"{baseZoneId},{addressTypeId},{minLat},{maxLat},{minLon},{maxLon},{latitude},{longitude},'{shapeJson.Replace("'", "''")}',NULL,'Mapbox',{now}, {now}";
                string fields = "BASE_ZONE_ID,ADDRESS_TYPE_ID,MIN_LATITUDE,MAX_LATITUDE,MIN_LONGITUDE,MAX_LONGITUDE,CENTEROID_LATITUDE,CENTEROID_LONGITUDE,SHAPE_JSON,DENSITY_LEVEL,SOURCE,CREATEDOADATE,UPDATEDOADATE";

                return DataAccessManager.Insert(LocationsSchema, Schema.Locations.ZonesRCI, fields, values);
            }

            /// <summary>
            /// Computes the haversine distance in meters between two geographic coordinates.
            /// </summary>
            private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
            {
                double dLat = (lat2 - lat1) * Math.PI / 180.0;
                double dLon = (lon2 - lon1) * Math.PI / 180.0;
                double rLat1 = lat1 * Math.PI / 180.0;
                double rLat2 = lat2 * Math.PI / 180.0;

                double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                           Math.Cos(rLat1) * Math.Cos(rLat2) *
                           Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                return EarthRadiusM * c;
            }
        }
    }
}
