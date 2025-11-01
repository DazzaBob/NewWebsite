using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Website.App.Database;

namespace Website.App.Operations.Zoning
{
    /// <summary>
    /// Manages Base and RCI zones in relation to addresses.
    /// Handles:
    /// - Validation of coordinates within zones.
    /// - Base zone fallback and reshaping.
    /// - RCI zone creation and merging by AddressType.
    /// </summary>
    /// This class is only ever referenced internally... this is not for PUBLIC API use.
    internal static partial class ZoneManager
    {
        private static readonly GeometryFactory _gf = new(new PrecisionModel(), 4326);
        private static readonly GeoJsonWriter _gjson = new();
        private static readonly GeoJsonReader _greader = new();
        private static readonly string LocationsSchema = Schema.Locations.Name;
        /// <summary>
        /// Handles a new address insertion:
        /// - Finds or creates a Base zone.
        /// - Finds or creates an RCI zone for the Base zone + AddressType.
        /// - Returns IDs for both zones.
        /// </summary>
        /// public static void AssignZonesForAddress(int addressId, double latitude, double longitude, int placeId, int? localityId)
        internal static void AssignZonesForAddress(int addressId, int AddressTypeId, double latitude, double longitude, int placeId, int? localityId)
        {
            ZoneHelpers.AssignAddressZones(addressId, AddressTypeId, latitude, longitude, placeId, localityId);
        }
        internal static int CreatePlaceZoneID(int placeId)
        {
            return ZoneSeeder.CreatePlaceZoneID(placeId);
        }
        internal static int CreateLocalityZoneID(int localityId, int placeZoneId)
        {
            return ZoneSeeder.CreateLocalityZoneID(localityId, placeZoneId);
        }
    }
}