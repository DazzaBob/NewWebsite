using System.Data;
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
        public static string SafeHtml(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            StringBuilder sb = new(input.Length);
            foreach (char c in input)
            {
                switch (c)
                {
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '&': sb.Append("&amp;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '\'': sb.Append("&#39;"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
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

            }


        }
    }
}
