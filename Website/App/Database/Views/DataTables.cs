using System.Data;

namespace Website.App.Database.Views
{
    internal static class DataTables
    {
        private static string Schema = "pub"; // Just use the public schema, these a joined tables across schema.
        internal static class UserAddress
        { 
            internal static DataTable DataTable(long userId = 0, long addressId = 0, long userAddressId = 0)
            {
                string sql = $@"SELECT ua.ID, ua.USER_ID, ua.LABEL, ua.ISDEFAULT, a.ID AS ADDRESS_ID, a.STREET_NUMBER, a.STREET_NAME, l.NAME AS LOCALITY_NAME, p.NAME AS PLACE_NAME
                FROM {Database.Schema.Entities.Tables.UserAddress} ua
                LEFT JOIN {Database.Schema.Locations.Tables.Address} a ON ua.ADDRESS_ID = a.ID
                LEFT JOIN {Database.Schema.Locations.Tables.Locality} l ON a.LOCALITY_ID = l.ID
                LEFT JOIN {Database.Schema.Locations.Tables.Place} p ON a.PLACE_ID = p.ID ";

                if (userId > 0 || addressId > 0 || userAddressId > 0)
                {
                    string whereclause = "WHERE ";
                    if (userId > 0) { whereclause += $"ua.USER_ID = {userId} AND "; }
                    if (addressId > 0) { whereclause += $"ua.ADDRESS_ID = {addressId} AND "; }
                    if (userAddressId > 0) { whereclause += $"ua.ID = {userAddressId} AND "; }
                    if (whereclause.EndsWith(" AND ")) { whereclause = whereclause[..^5]; } // Remove trailing AND

                    sql += whereclause;
                }
                using DataTable DT = DataAccessManager.GetDataTable(Schema, sql, []); // Meh we needed a schemaName, a random one was picked.. Locations is likely to be open.
                return DT;
            }
        }
        internal static class UserPaymentMethods
        {
            public static DataTable DataTable(long userId = 0)
            {
                string sql = $@"SELECT up.id, up.display_name, up.brand, up.last4, up.is_active, up.make_default, p.name AS provider_name
                FROM ent.user_payment up LEFT JOIN cfg.payment_type_provider p ON up.provider_id = p.id 
                WHERE up.user_id = {userId} AND up.is_active=true 
                ORDER BY up.make_default DESC, up.id ASC";

                using DataTable DT = DataAccessManager.GetDataTable(Schema, sql, []); // Meh we needed a schemaName, a random one was picked.. Locations is likely to be open.
                return DT;
            }
        }
        internal static class UserPayments
        {
            public static DataTable DataTable(long paymentId, long userId = 0)
            {
                string sql = $@"SELECT up.id AS user_payment_id, up.user_id, up.display_name, up.vault_token, up.brand, up.last4, up.exp_month, up.exp_year, up.provider_id, pprov.code AS provider_code, pprov.name AS provider_name, ptype.code AS payment_type_code, ptype.name AS payment_type_name 
                FROM ent.user_payment up JOIN cfg.payment_type_provider pprov ON pprov.id = up.provider_id JOIN cfg.payment_type ptype ON ptype.id = pprov.type_id 
                WHERE up.id = {paymentId} AND up.user_id = {userId} AND up.is_active = TRUE AND up.is_verified = TRUE AND pprov.is_enabled = TRUE AND ptype.is_enabled = TRUE LIMIT 1;";

                using DataTable DT = DataAccessManager.GetDataTable(Schema, sql, []);
                return DT;
            }
        }
    }
}
