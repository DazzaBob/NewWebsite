using System.Data;

namespace Website.App.Database.Views
{
    public static class UserAddress
    {
        public static DataTable DataTable(long userId = 0, long addressId = 0, long userAddressId = 0)
        {
            string sql = $@"SELECT ua.ID, ua.USER_ID, ua.LABEL, ua.ISDEFAULT, a.ID AS ADDRESS_ID, a.STREET_NUMBER, a.STREET_NAME, l.NAME AS LOCALITY_NAME, p.NAME AS PLACE_NAME
            FROM {Schema.Entities.UserAddress} ua
            LEFT JOIN {Schema.Locations.Address} a ON ua.ADDRESS_ID = a.ID
            LEFT JOIN {Schema.Locations.Locality} l ON a.LOCALITY_ID = l.ID
            LEFT JOIN {Schema.Locations.Place} p ON a.PLACE_ID = p.ID ";

            if (userId > 0 || addressId > 0 || userAddressId > 0)
            {
                string whereclause = "WHERE ";
                if (userId > 0) { whereclause += $"ua.USER_ID = {userId} AND "; }
                if (addressId > 0) { whereclause += $"ua.ADDRESS_ID = {addressId} AND "; }
                if (userAddressId > 0) { whereclause += $"ua.ID = {userAddressId} AND "; }
                if (whereclause.EndsWith(" AND ")) { whereclause = whereclause[..^5]; } // Remove trailing AND

                sql += whereclause;
            }
            using DataTable DT = DataAccessManager.GetDataTable(Schema.Locations.SchemaName, sql, []); // Meh we needed a schemaName, a random one was picked.. Locations is likely to be open.
            return DT;
        }
    }
}
