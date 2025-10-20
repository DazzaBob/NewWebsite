using System.Net;

namespace Website.App.Database
{
    public static class Shared
    {
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
    }
}
