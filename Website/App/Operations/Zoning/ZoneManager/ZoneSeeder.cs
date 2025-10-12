using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Simplify;
using System.Data;
using System.Text;

namespace Website.App.Operations.Zoning
{
    internal partial class ZoneManager
    {
        internal static class ZoneSeeder
        {
            private static readonly GeometryFactory _gf = new(new PrecisionModel(), 4326);
            private static readonly GeoJsonWriter _gjson = new();

            /// <summary>
            /// Creates a rectangular pioneer zone for a place.
            /// </summary>
            internal static void CreatePioneerPlaceZone(Helper.Connection connection, int placeId)
            {
                ArgumentNullException.ThrowIfNull(connection);

                if (placeId <= 0)
                    ArgumentOutOfRangeException.ThrowIfLessThan(placeId, 1, nameof(placeId));

                DataRow[] DR = Database.Locations.Tables.Place.DataTable().Select($"ID = {placeId}");
                if (DR.Length == 0)
                {
                    return;
                }

                StringBuilder SBValues = new();
                SBValues.Append($"{Database.Shared.SafeReplace(DR[0]["NAME"])}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(placeId)}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(null)}, "); // LOCALITY_ID is NULL
                SBValues.Append($"{Database.Shared.SafeReplace(DR[0]["LATITUDE"])}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(DR[0]["LONGITUDE"])}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(DR[0]["MIN_LATITUDE"]) ?? "NULL"}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(DR[0]["MAX_LATITUDE"]) ?? "NULL"}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(DR[0]["MIN_LONGITUDE"]) ?? "NULL"}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(DR[0]["MAX_LONGITUDE"]) ?? "NULL"}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(DateTime.Now.ToOADate())}, "); // CREATED
                SBValues.Append($"{Database.Shared.SafeReplace(DateTime.Now.ToOADate())}, "); // UPDATED
                SBValues.Append($"{Database.Shared.SafeReplace(ShapeToGeoJson(
                    Convert.ToDouble(DR[0]["MIN_LATITUDE"]),
                    Convert.ToDouble(DR[0]["MAX_LATITUDE"]),
                    Convert.ToDouble(DR[0]["MIN_LONGITUDE"]),
                    Convert.ToDouble(DR[0]["MAX_LONGITUDE"])
                ))}, "); // SHAPE_JSON
                SBValues.Append("0, 1, 1, 0"); // IS_RURAL, AUTO_GROW, IS_PIONEER_ZONE, IS_RURAL_CONFIRMED

                _ = Database.Shared.Insert(connection, "ZONES",
                    "NAME, PLACE_ID, LOCALITY_ID, CENTEROID_LATITUDE, CENTEROID_LONGITUDE, MIN_LATITUDE, MAX_LATITUDE, MIN_LONGITUDE, MAX_LONGITUDE, CREATEDOADATE, UPDATEDOADATE, SHAPE_JSON, IS_RURAL, AUTO_GROW, IS_PIONEER_ZONE, IS_RURAL_CONFIRMED",
                    SBValues.ToString()
                );
            }

            /// <summary>
            /// Creates a pioneer zone for a locality attached to a place.
            /// </summary>
            internal static void CreatePioneerLocalityZone(Helper.Connection connection, int placeId, int localityId)
            {
                if (connection == null) ArgumentNullException.ThrowIfNull(connection);
                if (placeId <= 0) ArgumentOutOfRangeException.ThrowIfLessThan(placeId, 1, nameof(placeId));
                if (localityId <= 0) ArgumentOutOfRangeException.ThrowIfLessThan(localityId, 1, nameof(localityId));

                DataTable DT = Database.Shared.GetDataTable(connection, "ZONES", $"PLACE_ID = {placeId} AND LOCALITY_ID IS NULL");
                if (DT.Rows.Count == 0) return; // place seed missing

                Geometry placePoly = new GeoJsonReader().Read<Geometry>(DT.Rows[0]["SHAPE_JSON"].ToString());

                // Load locality bbox
                DataRow[] DR = Database.Locations.Tables.Locality.DataTable(connection).Select($"ID = {localityId}");
                if (DR.Length == 0)
                {
                    return;
                }

                int localityPlaceId = Convert.ToInt32(DR[0]["PLACE_ID"]);
                if (localityPlaceId != placeId)
                {
                    return; // locality must belong to place
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
                double now = DateTime.Now.ToOADate();

                StringBuilder SBValues = new();
                SBValues.Append($"{Database.Shared.SafeReplace(DR[0]["NAME"])}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(placeId)}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(localityId)}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(centroid.Y)}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(centroid.X)}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(e.MinY)}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(e.MaxY)}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(e.MinX)}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(e.MaxX)}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(now)}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(now)}, ");
                SBValues.Append($"{Database.Shared.SafeReplace(geo)}, ");
                SBValues.Append("0, 1, 1, 0");

                _ = Database.Shared.Insert(connection,
                    "ZONES",
                    "NAME, PLACE_ID, LOCALITY_ID, CENTEROID_LATITUDE, CENTEROID_LONGITUDE, MIN_LATITUDE, MAX_LATITUDE, MIN_LONGITUDE, MAX_LONGITUDE, CREATEDOADATE, UPDATEDOADATE, SHAPE_JSON, IS_RURAL, AUTO_GROW, IS_PIONEER_ZONE, IS_RURAL_CONFIRMED",
                    SBValues.ToString()
                );
            }

            private static string ShapeToGeoJson(double minLat, double maxLat, double minLon, double maxLon)
            {
                var env = new Envelope(minLon, maxLon, minLat, maxLat);
                var rect = _gf.ToGeometry(env);
                return _gjson.Write(rect);
            }
        }
    }
}
