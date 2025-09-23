using System.Data;
namespace Website.App.Helper
{
    internal static class Shared
    {
        internal static string HashPassword(string password)
        {
            // Placeholder: Replace with your secure hash routine (e.g., BCrypt or SHA256)
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }
        internal static bool VerifyPassword(string inputPassword, string storedHash)
        {
            string inputHash = HashPassword(inputPassword);
            return storedHash == inputHash;
        }
        internal static bool IsDashboard(string path)
        {
            bool StartsWith = path.StartsWith("/driver")
            || path.StartsWith("/store") || path.StartsWith("/customer/dashboard")
            || path.StartsWith("/user/dashboard");

            return StartsWith;
        }
        internal static string XMLDataTable(DataTable DT)
        {
            DT.TableName = "ENT_USER_ADDRESS";

            using var sw = new StringWriter();
            DT.WriteXml(sw, XmlWriteMode.WriteSchema);
            return sw.ToString();
        }
        internal static bool IsValidPhone(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            int start = 0;
            if (input[0] == '+') start = 1; // optional plus

            for (int i = start; i < input.Length; i++)
            {
                if (input[i] < '0' || input[i] > '9')
                    return false;
            }
            return true;
        }
    }
}
