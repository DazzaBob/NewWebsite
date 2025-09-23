using Website.App.Database;

namespace App.Validation
{
    /// <summary>
    /// Encapsulates core spatial‐zone operations against the LOC_ZONES schema:
    /// - Locates the zone whose bounding box contains a given point.
    /// - Creates new zone records with explicit geographic extents.
    /// - Updates existing zone bounds when new points fall outside the current box.
    /// - Reads address‐type zoning rules (bounding‐box vs. radius and their max sizes).
    /// - Provides geodesic helpers to translate meter‐based distances into latitude/longitude offsets.
    /// </summary>
    internal sealed class ZoneValidation : IDisposable
    {
        private readonly Website.App.Helper.Connection _connection;
        private bool _disposed;

        internal ZoneValidation(Website.App.Helper.Connection connection) => _connection = connection;

        /// <summary>Return the first zone (ID) whose bbox contains the point.</summary>
        internal int FindContainingZoneID(double longitude, double latitude, int addressTypeId)
        {
            string sql = @$"SELECT ID FROM LOC_ZONES WHERE 
            ADDRESS_TYPE_ID = {addressTypeId} AND (MIN_LATITUDE <= {latitude} AND MAX_LATITUDE >= {latitude}) AND (MIN_LONGITUDE <= {longitude} AND MAX_LONGITUDE >= {longitude})";
            try
            {
                var id = _connection.ExecuteScalar(sql);
                if (id == null || id == DBNull.Value)
                {
                    // No zone found, return 0
                    return 0;
                }
                else
                {
                    return Convert.ToInt32(id);
                }
            }
            catch (Exception ex)
            {
                Website.App.Bootstrap.Logger?.Add($"ZoneValidation.FindContainingZoneID: {ex}", Website.App.Helper.Logger.LogLevel.Error);
                return 0;
            }
        }
        internal int CreateZoneID(string name, int addressTypeId, int placeId, int localityId, double minLat, double maxLat, double minLon, double maxLon)
        {
            string slocalityId = localityId > 0 ? localityId.ToString() : "NULL";
            string sql = @$"INSERT INTO LOC_ZONES (NAME, ADDRESS_TYPE_ID, PLACE_ID, LOCALITY_ID, MIN_LATITUDE, MAX_LATITUDE, MIN_LONGITUDE, MAX_LONGITUDE, CREATED_OADATE, UPDATED_OADATE)
            VALUES ('{Shared.SafeReplace(name)}', {addressTypeId}, {placeId}, {slocalityId}, {minLat}, {maxLat}, {minLon}, {maxLon}, {DateTime.Now.ToOADate()}, {DateTime.Now.ToOADate()}); SELECT last_insert_rowid();";
            try
            {
                var id = _connection.ExecuteScalar(sql);
                if (id == null || id == DBNull.Value)
                {
                    // If the insert failed, return null
                    return 0;
                }
                else
                {
                    return Convert.ToInt32(id);
                }
            }
            catch (Exception ex)
            {
                Website.App.Bootstrap.Logger?.Add($"ZoneValidation.CreateZone: {ex}", Website.App.Helper.Logger.LogLevel.Error);
                return 0;
            }
        }
        internal bool UpdateZoneBounds(string zoneId, double minLat, double maxLat, double minLon, double maxLon)
        {
            string sql = @$"UPDATE LOC_ZONES SET MIN_LATITUDE = {minLat}, MAX_LATITUDE = {maxLat}, MIN_LONGITUDE = {minLon}, MAX_LONGITUDE = {maxLon}, UPDATED_OADATE = {DateTime.Now.ToOADate()} WHERE ID = {zoneId}";
            try
            {
                _connection.ExecuteNonQuery(sql);
                return true;
            }
            catch (Exception ex)
            {
                Website.App.Bootstrap.Logger?.Add($"ZoneValidation.UpdateZoneBounds: {ex}", Website.App.Helper.Logger.LogLevel.Error);
                return false;
            }
        }
        internal bool IsBoundingBox(int addressTypeId)
        {
            string sql = $"SELECT ISBOUNDINGBOX FROM LOC_ADDRESS_TYPE WHERE ID = {addressTypeId}";
            var result = _connection.ExecuteScalar(sql);
            return result != DBNull.Value && result?.ToString() == "1";
        }
        internal double GetMaxBBoxFromAddressType(int addressTypeId)
        {
            string sql = $"SELECT MAX_ZONE_BBOX_SIZE_METERS FROM LOC_ADDRESS_TYPE WHERE ID = {addressTypeId}";
            var r = _connection.ExecuteScalar(sql);
            return r != DBNull.Value && double.TryParse(r?.ToString(), out var v) ? v : 500;
        }
        internal double GetMaxRadiusFromAddressType(int addressTypeId)
        {
            string sql = $"SELECT MAX_ZONE_RADIUS_METERS FROM LOC_ADDRESS_TYPE WHERE ID = {addressTypeId}";
            var r = _connection.ExecuteScalar(sql);
            return r != DBNull.Value && double.TryParse(r?.ToString(), out var v) ? v : 250;
        }

        /* ---------------------------------------------------------
           Geodesic helpers
        --------------------------------------------------------- */
        private readonly double EarthRadiusM = 6_378_137;   // mean radius in meters
        public double RadiusToLatitudeOffset(double radiusMeters) =>
            (radiusMeters / EarthRadiusM) * (180 / Math.PI);
        public double RadiusToLongitudeOffset(double radiusMeters, double latitude)
        {
            double latRad = latitude * Math.PI / 180.0;
            double radiusAtLat = EarthRadiusM * Math.Cos(latRad);
            return (radiusMeters / radiusAtLat) * (180 / Math.PI);
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // no managed resources to free right now
        }
    }
}
