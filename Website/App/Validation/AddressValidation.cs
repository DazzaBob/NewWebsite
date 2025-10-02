using System.Data;
using System.Text;

namespace Website.App.Validation
{
    public static partial class AddressValidation
    {
        private const string NamespaceClass = "App.Validation.AddressValidation.";
        public static long SaveAddressAndGetId(string payload, App.Helper.Connection locationsConnection, App.Helper.Connection entitiesConnection, long addressTypeId, long userId, bool SetAsDefault, string label = "")
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                string errorMessage = NamespaceClass + "SaveAddressAndGetId: JSON payload is empty.";
                Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Error);
                return -1;
            }
            Mapbox.GeoCoding.V5.LegacyFeatureParser.ExtractedAddress NEA = Mapbox.GeoCoding.V5.LegacyFeatureParser.ExtractFullAddress(payload);

            if (NEA.PlaceName == string.Empty)
            {
                string errorMessage = NamespaceClass + "SaveAddressAndGetId: Unable to extract the Address.";
                Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Error);
                return -1;
            }

            // Lets check to see if this address already exists in the Locations database.
            string streetNumber = Database.Shared.SafeReplace(NEA.AddressNumber);
            string streetName = Database.Shared.SafeReplace(NEA.AddressStreet);
            string postcode = Database.Shared.SafeReplace(NEA.Postcode);

            if (string.IsNullOrWhiteSpace(label))
            {
                // Fallback to if no label was provided.
                string numberPart = string.IsNullOrWhiteSpace(NEA.AddressNumber) ? "" : NEA.AddressNumber + " ";
                label = NEA.AddressNumber + " " + NEA.AddressStreet;
            }

            string whereClause = $"STREET_NUMBER={streetNumber} AND STREET_NAME={streetName} AND POSTCODE={postcode}";
            DataTable dt = App.Database.Shared.GetDataTable(locationsConnection, Database.Schema.Locations.Tables.Address, whereClause, "LASTUSEDOADATE DESC");

            long addressId;
            if (dt.Rows.Count > 0)
            { // ok, so the address exists, does the user have it linked?
                addressId = (long)dt.Rows[0]["ID"];
                _ = App.Database.Shared.Update(locationsConnection, Database.Schema.Locations.Tables.Address, $"LASTUSEDOADATE={DateTime.Today.ToOADate()}", $"ID={addressId}");

                dt = App.Database.Shared.GetDataTable(entitiesConnection, Database.Schema.Entities.Tables.UserAddress, $"USER_ID = {userId} AND ADDRESS_ID={addressId}");
                if (dt.Rows.Count > 0)
                { // user already has this address linked, update the label and return the ID.
                    long currentUserAddressId = (long)dt.Rows[0]["ID"];
                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        string newLabel = Database.Shared.SafeReplace(Database.Shared.SafeHtml(label));
                        App.Database.Shared.Update(entitiesConnection, Database.Schema.Entities.Tables.UserAddress, $"LABEL={newLabel}", $"USER_ID = {userId} AND ADDRESS_ID={currentUserAddressId}");
                    }
                    return currentUserAddressId;
                }
                else
                { // link the user to this address.
                    double today = DateTime.Now.ToOADate();
                    return InsertNewUserAddress(entitiesConnection, userId, addressId, label, today, SetAsDefault);
                }
            }
            else
            { // we need to resolve the address and add it.
                return SaveAddress(NEA.PlaceName, entitiesConnection, locationsConnection, addressTypeId, userId, SetAsDefault, label);
            }
        }
        private static long SaveAddress(string inputaddress, App.Helper.Connection entitiesConnection, App.Helper.Connection locationsConnection, long addressTypeId, long userId, bool IsDefault, string label)
        {
            try
            { // Convert the V5 to V6 so we can get all the information we need.
                var GD = new Mapbox.GeoCoding.V6.GeoDeserializer();
                Mapbox.GeoCoding.V6.ResolvedAddressFeature? RAF = GD.Search(inputaddress);

                if (RAF != null)
                {
                    return SaveAddressCore(RAF, entitiesConnection, locationsConnection, addressTypeId, userId, IsDefault, label);
                }
                else
                {
                    throw new ArgumentException("Unable to resolve the address");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = NamespaceClass + "SaveAddress: Failed to resolve address from legacy input." + Environment.NewLine + ex.Message;
                Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Error);
                return -1;
            }
        }

        /// <summary>
        /// Saves a resolved address to the database and returns its ID.
        /// 
        /// The routine performs the following steps:
        /// 1. Validates the resolved address data.
        /// 2. Checks if the address already exists in the Locations database
        ///    by matching street number, street name, and postcode.
        ///    - If found, updates its "last used" date and returns the existing ID.
        /// 3. If not found, inserts the new address into the Locations database.
        /// 4. Creates or updates the corresponding UserAddress record for the current user.
        /// 5. If requested, marks the address as the user's default by resetting other defaults
        ///    and updating this one.
        /// 6. Returns the existing or newly created address ID.
        /// </summary>
        private static long SaveAddressCore(Mapbox.GeoCoding.V6.ResolvedAddressFeature resolved, App.Helper.Connection entitiesConnection, App.Helper.Connection locationConnection, long addressTypeId, long userId, bool setAsDefault, string label)
        {
            string streetNumber = Database.Shared.SafeReplace(resolved.Address?.Number);
            string streetName = Database.Shared.SafeReplace(resolved.Address?.Name);
            string postcode = Database.Shared.SafeReplace(resolved.Address?.Postcode?.Name);

            int countryId = Database.Locations.Tables.Country.GetId(resolved.Country?.Name ?? "Unknown");
            int regionId = Database.Locations.Tables.Region.GetId(countryId, resolved.Region?.Name ?? "Unknown", resolved.Region?.RegionCode ?? "Unknown");
            int placeId = Database.Locations.Tables.Place.GetId(regionId, resolved.Country?.Name ?? "Unknown", resolved.Place?.Name ?? "Unknown");

            int localityId = -1;
            if (!string.IsNullOrWhiteSpace(resolved.Locality?.Name))
            {
                localityId = Database.Locations.Tables.Locality.GetId(placeId, resolved.Country?.Name ?? "", resolved.Place?.Name ?? "", resolved.Locality?.Name ?? "");
            }

            string fields = "STREET_NUMBER, STREET_NAME, POSTCODE, COUNTRY_ID, REGION_ID, PLACE_ID, LOCALITY_ID, ADDRESS_TYPE_ID, LONGITUDE, LATITUDE, ROUTABLE_LONGITUDE, ROUTABLE_LATITUDE, LASTUSEDOADATE";
            var sb = new StringBuilder();
            sb.Append(streetNumber).Append(", ")
            .Append(streetName).Append(", ")
            .Append(postcode).Append(", ")
            .Append(countryId).Append(", ")
            .Append(regionId).Append(", ")
            .Append(placeId).Append(", ")
            .Append(localityId > 0 ? localityId : "NULL").Append(", ")
            .Append(addressTypeId).Append(", ")
            .Append(resolved.Coords.DisplayLongitude).Append(", ")
            .Append(resolved.Coords.DisplayLatitude).Append(", ")
            .Append(resolved.Coords.RoutableLongitude > 0 ? resolved.Coords.RoutableLongitude : "NULL").Append(", ")
            .Append(resolved.Coords.RoutableLatitude > 0 ? resolved.Coords.RoutableLatitude : "NULL").Append(", ")
            .Append(DateTime.Now.ToOADate());

            long addressId = Database.Shared.Insert(locationConnection, Database.Schema.Locations.Tables.Address, fields, sb.ToString());
            if (addressId <= 0)
            {
                Bootstrap.Logger?.Add($"{NamespaceClass}SaveAddressCore: failed to insert address '{resolved.Address?.Number} {resolved.Address?.Name}'", Helper.Logger.LogLevel.Error);
                return -1;
            }

            string newLabel;
            if (!string.IsNullOrWhiteSpace(label))
            {
                newLabel = Database.Shared.SafeReplace(Database.Shared.SafeHtml(label));
            }
            else
            {
                newLabel = $"{resolved.Address?.Number} {resolved.Address?.Name}";
            }
            double today = DateTime.Now.ToOADate();

            return InsertNewUserAddress(entitiesConnection, userId, addressId, newLabel, today, setAsDefault);
        }
        public static long InsertNewUserAddress(App.Helper.Connection entitiesConnection, long userId, long addressId, string label, double today, bool setAsDefault)
        {
            string newLabel = Database.Shared.SafeReplace(Database.Shared.SafeHtml(label));
            string userFields = "USER_ID, ADDRESS_ID, LABEL, ISDEFAULT, CREATEDOADATE, UPDATEDOADATE, ISACTIVE";
            string userValues = $"{userId}, {addressId}, {newLabel}, {(setAsDefault ? 1 : 0)}, {today}, {today}, 1";
            long userAddressId = Database.Shared.Insert(entitiesConnection, Database.Schema.Entities.Tables.UserAddress, userFields, userValues);

            if (setAsDefault && addressId > 0)
            {
                App.Database.Shared.Update(entitiesConnection, "USER_ADDRESS", "ISDEFAULT=0", $"USER_ID={userId}");
                App.Database.Shared.Update(entitiesConnection, "USER_ADDRESS", "ISDEFAULT=1", $"USER_ID={userId} AND ADDRESS_ID={addressId}");
            }

            return userAddressId;
        }
    }
}