using System.Data;
using System.Text;

namespace Website.App.Validation
{
    public static partial class AddressValidation
    {
        private const string NamespaceClass = "App.Validation.AddressValidation.";
        private const string ADSLD = App.Database.Schema.Locations.SchemaName;
        private const string ADSED = App.Database.Schema.Locations.SchemaName;
        public static long SaveAddressAndGetId(string payload, long addressTypeId, long userId, bool SetAsDefault, string label = "")
        {
            if (string.IsNullOrWhiteSpace(payload)) { LogError("SaveAddressAndGetId: JSON payload is empty."); return -1; }

            try
            {
                Mapbox.GeoCoding.V5.LegacyFeatureParser.ExtractedAddress? nea = TryExtractLegacyAddress(payload);
                if (nea == null || string.IsNullOrWhiteSpace(nea.PlaceName)) { LogError("SaveAddressAndGetId: Unable to extract the Address."); return -1; }

                string effectiveLabel = BuildEffectiveLabel(label, nea);

                DataRow? addressRow = FindAddressRow(nea.AddressNumber, nea.AddressStreet, nea.Postcode);
                if (addressRow != null)
                {
                    long addressId = Convert.ToInt64(addressRow["ID"]);
                    UpdateAddressLastUsed(addressId);

                    long? existingUserAddressId = GetUserAddressId(userId, addressId);
                    if (existingUserAddressId.HasValue)
                    {
                        if (!string.IsNullOrWhiteSpace(label))
                            UpdateUserAddressLabel(existingUserAddressId.Value, effectiveLabel);

                        return existingUserAddressId.Value;
                    }
                    else
                    {
                        double today = DateTime.UtcNow.ToOADate();
                        return InsertNewUserAddress(userId, addressId, effectiveLabel, today, SetAsDefault);
                    }
                }
                else
                {
                    return SaveAddress(nea.PlaceName, addressTypeId, userId, SetAsDefault, effectiveLabel);
                }
            }
            catch (Exception ex)
            {
                LogError($"SaveAddressAndGetId: Unhandled error. {ex.Message}\n{ex.StackTrace}");
                return -1;
            }
        }
        private static Mapbox.GeoCoding.V5.LegacyFeatureParser.ExtractedAddress? TryExtractLegacyAddress(string payload)
        {
            try
            {
                return Mapbox.GeoCoding.V5.LegacyFeatureParser.ExtractFullAddress(payload);
            }
            catch (Exception ex)
            {
                LogError($"TryExtractLegacyAddress: exception parsing payload. {ex.Message}");
                return null;
            }
        }
        private static string BuildEffectiveLabel(string providedLabel, Mapbox.GeoCoding.V5.LegacyFeatureParser.ExtractedAddress nea)
        {
            if (!string.IsNullOrWhiteSpace(providedLabel))
                return providedLabel;

            string numberPart = string.IsNullOrWhiteSpace(nea.AddressNumber) ? "" : nea.AddressNumber + " ";
            return $"{numberPart}{nea.AddressStreet}".Trim();
        }
        private static DataRow? FindAddressRow(string streetNumber, string streetName, string postcode)
        {
            string sn = Database.Shared.Sanitize(streetNumber, forSql: true);
            string sname = Database.Shared.Sanitize(streetName, true, true);
            string pc = Database.Shared.Sanitize(postcode, forSql: true);

            string whereClause = $"STREET_NUMBER={sn} AND STREET_NAME={sname} AND POSTCODE={pc}";
            using DataTable dt = Database.DataAccessManager.GetDataTable(ADSLD, Database.Schema.Locations.Address, whereClause, "LASTUSEDOADATE DESC");

            return (dt != null && dt.Rows.Count > 0) ? dt.Rows[0] : null;
        }
        private static void UpdateAddressLastUsed(long addressId)
        {
            try
            {
                _ = Database.DataAccessManager.Update(ADSLD, Database.Schema.Locations.Address, $"LASTUSEDOADATE={DateTime.UtcNow.ToOADate()}", $"ID={addressId}");
            }
            catch (Exception ex)
            {
                LogError($"UpdateAddressLastUsed: failed for Address ID {addressId}. {ex.Message}");
            }
        }
        private static long? GetUserAddressId(long userId, long addressId)
        {
            using DataTable data = Database.DataAccessManager.GetDataTable(ADSED, Database.Schema.Entities.UserAddress, $"USER_ID = {userId} AND ADDRESS_ID = {addressId}");
            if (data != null && data.Rows.Count > 0)
                return Convert.ToInt64(data.Rows[0]["ID"]);
            return null;
        }
        private static void UpdateUserAddressLabel(long userAddressId, string newLabel)
        {
            try
            {
                string safeLabel = Database.Shared.Sanitize(newLabel, true, true);
                _ = Database.DataAccessManager.Update(ADSED, Database.Schema.Entities.UserAddress, $"LABEL={safeLabel}", $"ID={userAddressId}");
            }
            catch (Exception ex)
            {
                Bootstrap.Logger?.Add($"UpdateUserAddressLabel: failed for UserAddress ID {userAddressId}. {ex.Message}", Helper.Logger.LogLevel.Error);
            }
        }
        private static long SaveAddress(string inputaddress, long addressTypeId, long userId, bool IsDefault, string label)
        {
            try
            {
                var GD = new Mapbox.GeoCoding.V6.GeoDeserializer();
                Mapbox.GeoCoding.V6.ResolvedAddressFeature? RAF = GD.Search(inputaddress);

                if (RAF != null)
                    return SaveAddressCore(RAF, addressTypeId, userId, IsDefault, label);

                throw new ArgumentException("Unable to resolve the address");
            }
            catch (Exception ex)
            {
                LogError("SaveAddress: Failed to resolve address from legacy input. " + ex.Message);
                return -1;
            }
        }
        /// <summary>
        /// Inserts a resolved address into the database, associates it with a user, 
        /// and performs automatic zone (Base + RCI) assignment.
        ///
        /// This method:
        /// 1. Sanitizes and resolves location hierarchy entities (Country, Region, Place, Locality).
        /// 2. Inserts a new address record with geographic coordinates and metadata.
        /// 3. Links the address to the specified user, creating a user-address entry.
        /// 4. Invokes <see cref="ZoneManager.AssignZonesForAddress"/> to classify the address 
        ///    within its Base and RCI zones based on latitude/longitude.
        /// 
        /// Fallback behavior:
        /// - If DisplayLatitude/Longitude are null, routable coordinates are used.
        /// - If coordinates are unavailable, zone assignment is skipped.
        /// 
        /// Returns the new UserAddressId if successful, or -1 if the insert fails.
        /// </summary>
        /// <param name="resolved">The resolved address feature from Mapbox containing address, place, and coordinate data.</param>
        /// <param name="addressTypeId">The address type (e.g., residential, commercial, etc.) used for RCI zoning.</param>
        /// <param name="userId">The user ID to associate the address with.</param>
        /// <param name="setAsDefault">Whether to mark this address as the user’s default.</param>
        /// <param name="label">Optional user-facing label for the address entry.</param>
        /// <returns>The ID of the new user-address record, or -1 if the address insert fails.</returns>
        private static long SaveAddressCore(Mapbox.GeoCoding.V6.ResolvedAddressFeature resolved, long addressTypeId, long userId, bool setAsDefault, string label)
        {
            string streetNumber = Database.Shared.Sanitize(resolved.Address?.Number, true, true);
            string streetName = Database.Shared.Sanitize(resolved.Address?.Name, true, true);
            string postcode = Database.Shared.Sanitize(resolved.Address?.Postcode?.Name, true, true);

            int countryId = Database.Locations.Tables.Country.GetId(resolved.Country?.Name ?? "Unknown");
            int regionId = Database.Locations.Tables.Region.GetId(countryId, resolved.Region?.Name ?? "Unknown", resolved.Region?.RegionCode ?? "Unknown");
            int placeId = Database.Locations.Tables.Place.GetId(regionId, resolved.Country?.Name ?? "Unknown", resolved.Place?.Name ?? "Unknown");

            int localityId = -1;
            if (!string.IsNullOrWhiteSpace(resolved.Locality?.Name))
                localityId = Database.Locations.Tables.Locality.GetId(placeId, resolved.Country?.Name ?? "", resolved.Place?.Name ?? "", resolved.Locality?.Name ?? "");

            string fields = "STREET_NUMBER, STREET_NAME, POSTCODE, COUNTRY_ID, REGION_ID, PLACE_ID, LOCALITY_ID, ADDRESS_TYPE_ID, LONGITUDE, LATITUDE, ROUTABLE_LONGITUDE, ROUTABLE_LATITUDE, LASTUSEDOADATE";
            var sb = new StringBuilder();
            double lat = resolved.Coords.DisplayLatitude ?? double.NaN;
            double lon = resolved.Coords.DisplayLongitude ?? double.NaN;
            if (double.IsNaN(lat) || double.IsNaN(lon))
            {
                lat = resolved.Coords.RoutableLatitude ?? double.NaN;
                lon = resolved.Coords.RoutableLongitude ?? double.NaN;
            }

            sb.Append($"{streetNumber}, {streetName}, {postcode}, {countryId}, {regionId}, {placeId}, ")
              .Append(localityId > 0 ? localityId : "NULL").Append(", ")
              .Append(addressTypeId).Append(", ")
              .Append(resolved.Coords.DisplayLongitude).Append(", ")
              .Append(resolved.Coords.DisplayLatitude).Append(", ")
              .Append(resolved.Coords.RoutableLongitude > 0 ? resolved.Coords.RoutableLongitude : "NULL").Append(", ")
              .Append(resolved.Coords.RoutableLatitude > 0 ? resolved.Coords.RoutableLatitude : "NULL").Append(", ")
              .Append(DateTime.UtcNow.ToOADate());

            long addressId = Database.DataAccessManager.Insert(ADSLD, Database.Schema.Locations.Address, fields, sb.ToString());
            if (addressId <= 0)
            {
                LogError($"SaveAddressCore: failed to insert address '{resolved.Address?.Number} {resolved.Address?.Name}'");
                return -1;
            }

            string newLabel = string.IsNullOrWhiteSpace(label) ? $"{resolved.Address?.Number} {resolved.Address?.Name}" : label;
            double today = DateTime.UtcNow.ToOADate();

            try
            {
                long newUserAddressId = InsertNewUserAddress(userId, addressId, newLabel, today, setAsDefault);

                if (!double.IsNaN(lat) && !double.IsNaN(lon))
                {
                    App.Operations.Zoning.ZoneManager.AssignZonesForAddress((int)addressId, (int)addressTypeId, lat, lon, placeId, localityId);
                }

                return newUserAddressId;
            }
            catch (Exception ex)
            {
                LogError($"SaveAddressCore: Insert or zoning failure for '{resolved.Address?.Name}'. {ex.Message}");
                return -1;
            }
        }
        public static long InsertNewUserAddress(long userId, long addressId, string label, double today, bool setAsDefault)
        {
            string newLabel = Database.Shared.Sanitize(label, true, true);
            string userFields = "USER_ID, ADDRESS_ID, LABEL, ISDEFAULT, ISACTIVE, CREATEDOADATE, UPDATEDOADATE";
            string userValues = $"{userId}, {addressId}, {newLabel}, {setAsDefault}, true, {today}, {today}";

            try
            {
                long userAddressId = Database.DataAccessManager.Insert(ADSED, Database.Schema.Entities.UserAddress, userFields, userValues);

                if (setAsDefault && addressId > 0)
                {
                    _ = Database.DataAccessManager.Update(ADSED, Database.Schema.Entities.UserAddress, "ISDEFAULT=false", $"USER_ID={userId}");
                    _ = Database.DataAccessManager.Update(ADSED, Database.Schema.Entities.UserAddress, "ISDEFAULT=true", $"USER_ID={userId} AND ADDRESS_ID={addressId}");
                }

                return userAddressId;
            }
            catch (Exception ex)
            {
                LogError($"InsertNewUserAddress: failed for user {userId}, address {addressId}. {ex.Message}");
                return -1;
            }
        }
        private static void LogError(string message)
        {
            Bootstrap.Logger?.Add(NamespaceClass + message, Helper.Logger.LogLevel.Error);
        }
    }
}
