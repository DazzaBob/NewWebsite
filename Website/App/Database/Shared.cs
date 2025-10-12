using System.Data;
using System.Net;
using System.Text;

namespace Website.App.Database
{
    public static class Shared
    {
        public static DataTable GetDataTable(Website.App.Helper.Connection connection, string tableName, string where = "", string orderBy = "")
        {
            StringBuilder sql = new($"SELECT * FROM {tableName}");
            if (!string.IsNullOrWhiteSpace(where))
                sql.Append(" WHERE ").Append(where);
            if (!string.IsNullOrWhiteSpace(orderBy))
                sql.Append(" ORDER BY ").Append(orderBy);

            return connection.GetDataTable(sql.ToString());
        }
        public static int Insert(Website.App.Helper.Connection connection, string tableName, string fields, string values)
        {
            string sql = $"INSERT INTO {tableName} ({fields}) VALUES ({values}); SELECT last_insert_rowid();";
            object result = connection.ExecuteScalar(sql);
            return Convert.ToInt32(result);
        }
        public static object Update(Website.App.Helper.Connection connection, string tableName, string setClause, string? whereClause = null)
        {
            StringBuilder sql = new($"UPDATE {tableName} SET {setClause}");
            if (!string.IsNullOrWhiteSpace(whereClause))
                sql.Append(" WHERE ").Append(whereClause);

            return connection.ExecuteNonQuery(sql.ToString());
        }
        public static object? GetScalar(Website.App.Helper.Connection connection, string tableName, string field, string? whereClause = null, string? orderBy = null)
        {
            StringBuilder sql = new($"SELECT {field} FROM {tableName}");
            if (!string.IsNullOrWhiteSpace(whereClause))
                sql.Append(" WHERE ").Append(whereClause);
            if (!string.IsNullOrWhiteSpace(orderBy))
                sql.Append(" ORDER BY ").Append(orderBy);

            sql.Append(" LIMIT 1;"); // only need one value
            return connection.ExecuteScalar(sql.ToString());
        }
        #region Sanitize
        /// <summary>
        /// Safely prepares a value for output or storage by optionally applying HTML and/or SQL sanitization.
        /// </summary>
        /// <param name="input">
        /// The value to sanitize. Can be any object type including strings, numbers, booleans, or dates.
        /// </param>
        /// <param name="forSql">
        /// If <c>true</c>, the result will be escaped for safe use in SQL statements (e.g., single quotes doubled, 
        /// null values returned as <c>"NULL"</c>).
        /// </param>
        /// <param name="forHtml">
        /// If <c>true</c>, the result will be HTML-encoded to prevent script injection or layout corruption when displayed.
        /// </param>
        /// <returns>
        /// A sanitized string safe for the intended output context. 
        /// When <paramref name="forSql"/> is <c>true</c>, the return value is wrapped in single quotes unless it represents a literal <c>NULL</c>.
        /// </returns>
        /// <remarks>
        /// This method replaces both <c>SafeHtml</c> and <c>SafeReplace</c> by consolidating their behavior into one.
        /// It should be used whenever user-provided or variable data is being written to SQL or rendered in HTML.
        /// </remarks>
        public static string Sanitize(object? input, bool forSql = false, bool forHtml = false)
        {
            if (input == null || input == DBNull.Value) return forSql ? "NULL" : string.Empty;
            string result = input switch
            {
                string s => s,
                DateTime dt => dt.ToOADate().ToString(System.Globalization.CultureInfo.InvariantCulture),
                bool b => b ? "1" : "0",
                double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
                decimal m => m.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => Convert.ToString(input, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
            };
            if (forHtml) result = WebUtility.HtmlEncode(result);
            if (forSql)
            {
                if (string.IsNullOrWhiteSpace(result) ||
                    string.Equals(result.Trim(), "NULL", StringComparison.OrdinalIgnoreCase))
                    return "NULL";
                result = $"'{result.Replace("'", "''")}'";
            }
            return result;
        }
        #endregion

        public static string SafeHtml(string? input)
        {
            return string.IsNullOrEmpty(input)
                ? string.Empty
                : WebUtility.HtmlEncode(input);
        }

        public static string SafeReplace(object? value)
        {
            if (value == null || value == DBNull.Value)
                return "NULL";

            if (value is string s)
                return $"'{s.Replace("'", "''")}'";

            if (value is DateTime dt)
                return dt.ToOADate().ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (value is bool b)
                return b ? "1" : "0";

            if (value is double d)
                return d.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (value is float f)
                return f.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (value is decimal m)
                return m.ToString(System.Globalization.CultureInfo.InvariantCulture);

            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL";
        }
        public static Helper.Connection Connection(string dbName)
        {
            string basePath = App.Settings.DatabasePath; // from appsettings.json
            string fullPath = Path.Combine(basePath, dbName);
            return new Helper.Connection($"Data Source={fullPath}");
        }
        public static void EnsureDatabase(string dbName)
        {
            string basePath = App.Settings.DatabasePath;
            string fullPath = Path.Combine(basePath, dbName);
            if (!File.Exists(fullPath))
            {
                if (dbName == Database.Schema.Entities.Database)
                {
                    Database.Entities.CreateDatabase.Begin();
                }
                if (dbName == Database.Schema.Locations.Database)
                {
                    Database.Locations.CreateDatabase.Begin();
                }
                if (dbName == Database.Schema.Operations.Database)
                {
                    Database.Operations.CreateDatabase.Begin();
                }
            }


        }
    }
}
