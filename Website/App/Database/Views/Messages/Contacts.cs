using System.Data;

namespace Website.App.Database.Views.Messages
{
    internal static class Contacts
    {
        internal static long SupportId()
        {
            string sqlSupport = @"SELECT u.id FROM ent.users u INNER JOIN ent.user_roles ur ON ur.user_id = u.id INNER JOIN ent.roles r ON r.id = ur.role_id WHERE LOWER(r.name) = 'support' LIMIT 1;";
            object? result = DataAccessManager.ExecuteScalar("pub", sqlSupport, []); // Multi Schema, pick "pub"

            return result == DBNull.Value ? 0L : Convert.ToInt64(result);
        }


    }
}
