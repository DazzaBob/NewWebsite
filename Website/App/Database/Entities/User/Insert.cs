namespace Website.App.Database.Entities.User
{
    internal static class Insert
    {
        private static readonly string NamespaceClass = "App.Database.Entities.User.Insert.";

        public static int Id(App.Helper.Connection connection, string fullName, string email, string phone, string passwordHash)
        {
            try
            {
                // SQLite: Insert row and return last inserted ID
                string cmd = @"INSERT INTO ENT_USER (FULLNAME, EMAIL, PHONE, PASSWORDHASH) 
                               VALUES (@FullName, @Email, @Phone, @PasswordHash);
                               SELECT last_insert_rowid();";

                Microsoft.Data.Sqlite.SqliteParameter[] parameters =
                [
                    new Microsoft.Data.Sqlite.SqliteParameter("@FullName", fullName ?? (object)DBNull.Value),
                    new Microsoft.Data.Sqlite.SqliteParameter("@Email", email ?? (object)DBNull.Value),
                    new Microsoft.Data.Sqlite.SqliteParameter("@Phone", phone ?? (object)DBNull.Value),
                    new Microsoft.Data.Sqlite.SqliteParameter("@PasswordHash", passwordHash)
                ];

                int userID = Convert.ToInt32(connection.ExecuteScalar(cmd, parameters));
                return userID;
            }
            catch (Exception ex)
            {
                string errorMessage = NamespaceClass + $"Id: Failed to insert User into table." + Environment.NewLine + ex.Message;
                Website.App.Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Error);
                return -1;
            }
        }
    }
}