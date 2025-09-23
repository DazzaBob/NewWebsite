using System.Data;
using System.Text;

namespace Website.App.Validation
{
    internal partial class AddressValidation : IDisposable
    {
        private readonly int userId;
        private readonly int _addressTypeId;
        private readonly bool IsDefault = false;
        private const string NamespaceClass = "App.Validation.AddressValidation.";
        private bool _disposed;

        internal AddressValidation()
        {
            _addressTypeId = 0; // default or throw if required
            userId = -1;
        }

        internal AddressValidation(int addressTypeID, int userId, bool IsDefault = false) : this()
        {
            _addressTypeId = addressTypeID;
            this.userId = userId;
            this.IsDefault = IsDefault;
        }

        internal int SaveAddressAndGetId(string payload)
        {
            return SaveAddress(payload);
        }

        private int SaveAddress(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                string errorMessage = NamespaceClass + "SaveAddress: JSON payload is empty.";
                Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Error);
                return -1;
            }
            try
            {
                string inputaddress = Mapbox.GeoCoding.V5.LegacyFeatureParser.ExtractFullAddress(payload);

                if (string.IsNullOrWhiteSpace(inputaddress))
                    throw new ArgumentException("Legacy input did not produce a valid address.");

                var GD = new Mapbox.GeoCoding.V6.GeoDeserializer();
                var RAF = GD.Search(inputaddress);

                if (RAF != null)
                {
                    return SaveAddressCore(RAF);
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

        private int SaveAddressCore(Mapbox.GeoCoding.V6.ResolvedAddressFeature ResolvedAddressFeatures)
        {
            if (string.IsNullOrWhiteSpace(ResolvedAddressFeatures.Address?.FullAddress))
            {
                string errorMessage = NamespaceClass + "SaveAddressCore: missing Address.FullAddress";
                Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Error);
                return -1;
            }

            using Helper.Connection LocationConnection = Database.Shared.Connection(Database.Schema.Locations.Database);
            using Helper.Connection EntitiesConnection = Database.Shared.Connection(Database.Schema.Entities.Database);

            string AddressNumber = Database.Shared.SafeReplace(ResolvedAddressFeatures.Address.Number);
            string AddressName = Database.Shared.SafeReplace(ResolvedAddressFeatures.Address.Name);
            string AddressPostcode = Database.Shared.SafeReplace(ResolvedAddressFeatures.Address.Postcode?.Name);

            int UpdateAddressID = -1;

            using DataTable DT = App.Database.Shared.GetDataTable(LocationConnection, Database.Schema.Locations.Tables.Address, $"(STREET_NUMBER = {AddressNumber}) AND (STREET_NAME = {AddressName}) AND (POSTCODE = {AddressPostcode})", "LASTUSEDOADATE DESC");

            if (DT.Rows.Count > 0)
            {
                UpdateAddressID = Convert.ToInt32(DT.Rows[0]["ID"]);
                App.Database.Shared.Update(EntitiesConnection, "USER_ADDRESS", "ISDEFAULT=0", $"USER_ID={userId}");
                App.Database.Shared.Update(EntitiesConnection, "USER_ADDRESS", "ISDEFAULT=1", $"USER_ID={userId} AND ADDRESS_ID={UpdateAddressID}");
                _ = App.Database.Shared.Update(LocationConnection, Database.Schema.Locations.Tables.Address, $"LASTUSEDOADATE={DateTime.Today.ToOADate()}", $"ID={UpdateAddressID}");
                return UpdateAddressID;
            }

            if (UpdateAddressID <= 0)
            {
                int countryId = Database.Locations.Tables.Country.GetId(ResolvedAddressFeatures.Country?.Name ?? "Unknown");
                int regionId = Database.Locations.Tables.Region.GetId(countryId, ResolvedAddressFeatures.Region?.Name ?? "Unknown", ResolvedAddressFeatures.Region?.RegionCode ?? "Unknown");
                int placeId = Database.Locations.Tables.Place.GetId(regionId, ResolvedAddressFeatures.Country?.Name ?? "Unknown", ResolvedAddressFeatures.Place?.Name ?? "Unknown");

                int localityId = -1;
                if (!string.IsNullOrWhiteSpace(ResolvedAddressFeatures.Locality?.Name))
                {
                    localityId = Database.Locations.Tables.Locality.GetId(placeId, ResolvedAddressFeatures.Country?.Name ?? string.Empty, ResolvedAddressFeatures.Place?.Name ?? string.Empty, ResolvedAddressFeatures.Locality?.Name ?? string.Empty);
                }

                string fields = "STREET_NUMBER, STREET_NAME, POSTCODE, COUNTRY_ID, REGION_ID, PLACE_ID, LOCALITY_ID, ADDRESS_TYPE_ID, LONGITUDE, LATITUDE, ROUTABLE_LONGITUDE, ROUTABLE_LATITUDE, LASTUSEDOADATE";
                StringBuilder sb = new();
                sb.Append(AddressNumber).Append(", ");
                sb.Append(AddressName).Append(", ");
                sb.Append(AddressPostcode).Append(", ");
                sb.Append(countryId).Append(", ");
                sb.Append(regionId).Append(", ");
                sb.Append(placeId).Append(", ");
                sb.Append(localityId > 0 ? localityId.ToString() : "NULL").Append(", ");
                sb.Append(_addressTypeId).Append(", ");
                sb.Append(ResolvedAddressFeatures.Coords.DisplayLongitude.ToString()).Append(", ");
                sb.Append(ResolvedAddressFeatures.Coords.DisplayLatitude.ToString()).Append(", ");
                sb.Append(ResolvedAddressFeatures.Coords.RoutableLongitude > 0 ? ResolvedAddressFeatures.Coords.RoutableLongitude.ToString() : "NULL").Append(", ");
                sb.Append(ResolvedAddressFeatures.Coords.RoutableLatitude > 0 ? ResolvedAddressFeatures.Coords.RoutableLatitude.ToString() : "NULL").Append(", ");
                sb.Append(DateTime.Now.ToOADate());

                int InsertAddressID = Database.Shared.Insert(LocationConnection, Database.Schema.Locations.Tables.Address, fields, sb.ToString());
                if (InsertAddressID <= 0)
                {
                    string errorMessage = NamespaceClass + $"SaveAddressCore: failed to insert address '{ResolvedAddressFeatures.Address.Number} {ResolvedAddressFeatures.Address.Name}'";
                    Website.App.Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Error);
                    return -1;
                }
                else
                {
                    fields = "USER_ID, ADDRESS_ID, LABEL, ISDEFAULT, CREATEDOADATE, UPDATEDOADATE";
                    sb.Clear();
                    sb.Append(userId).Append(", ");
                    sb.Append(InsertAddressID).Append(", ");
                    string label = ResolvedAddressFeatures.Address.Number + " " + ResolvedAddressFeatures.Address.Name;
                    sb.Append(Database.Shared.SafeReplace(label)).Append(", ");
                    sb.Append(IsDefault ? "1" : "0").Append(", ");
                    double today = DateTime.Now.ToOADate();
                    sb.Append(today).Append(", ");
                    sb.Append(today);

                    _ = Database.Shared.Insert(EntitiesConnection, Database.Schema.Entities.Tables.UserAddress, fields, sb.ToString());
                    Operations.Zoning.ZoneManager.StartZoning();
                }
                return InsertAddressID;
            }
            else
            {
                if (App.Database.Shared.GetDataTable(EntitiesConnection, Database.Schema.Entities.Tables.UserAddress, $"(USER_ID = {userId}) AND (ADDRESS_ID = {UpdateAddressID})").Rows.Count > 0)
                {
                    _ = App.Database.Shared.Update(EntitiesConnection, Database.Schema.Entities.Tables.UserAddress, "ISDEFAULT=1", $"USER_ID={userId} AND ADDRESS_ID={UpdateAddressID}");
                }
                else
                {
                    StringBuilder sb = new();
                    sb.Append(userId).Append(", ");
                    sb.Append(UpdateAddressID).Append(", ");
                    string label = ResolvedAddressFeatures.Address.Number + " " + ResolvedAddressFeatures.Address.Name;
                    sb.Append(Database.Shared.SafeReplace(label)).Append(", ");
                    sb.Append(IsDefault ? "1" : "0").Append(", ");
                    double today = DateTime.Now.ToOADate();
                    sb.Append(today).Append(", ");
                    sb.Append(today).Append(", 1");

                    _ = App.Database.Shared.Insert(EntitiesConnection, Database.Schema.Entities.Tables.UserAddress, "USER_ID, ADDRESS_ID, LABEL, ISDEFAULT, CREATEDOADATE, UPDATEDOADATE, ISACTIVE", sb.ToString());
                }
                return UpdateAddressID;
            }
        }

        #region IDisposable
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}