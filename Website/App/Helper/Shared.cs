namespace Website.App.Helper.Enums
{
    public enum JobStatus 
    {
        // Pre-driver / payment lifecycle
        Draft = 10,   // DRAFT
        PendingPayment = 11,   // PENDING_PAYMENT
        PaymentAuthorized = 12,   // PAYMENT_AUTHORIZED
        PaymentCaptured = 13,   // PAYMENT_CAPTURED
        Scheduled = 14,   // SCHEDULED
        ReadyForAllocation = 15,   // READY_FOR_ALLOCATION

        // Existing “core” statuses (from cfg.job_status)
        Created = 1,    // CREATED
        PendingAllocation = 2,    // PENDING_ALLOCATION
        Allocating = 3,    // ALLOCATING
        Assigned = 4,    // ASSIGNED

        // Driver flow – coarse + detailed stages
        EnRoute = 5,    // EN_ROUTE (generic / legacy)
        EnRoutePickup = 16,   // EN_ROUTE_PICKUP
        AtPickup = 17,   // AT_PICKUP
        EnRouteDropoff = 18,   // EN_ROUTE_DROPOFF
        AtDropoff = 19,   // AT_DROPOFF
        PickedUp = 6,    // PICKED_UP

        // Terminal
        Delivered = 7,    // DELIVERED
        Cancelled = 8,    // CANCELLED
        Failed = 9     // FAILED
    }
}
namespace Website.App.Helper
{
    internal static class Shared
    {
        internal static string BuildJobRef(int countryId, int regionId, int placeId, int locationId, long jobId)
        {
            // Sum the numeric IDs. This is equivalent to "01+0A+12C+03" because those hex
            // representations are just these same integers.
            var prefixValue = countryId + regionId + placeId + locationId;

            // Convert the sum to hex. Choose padding width based on how big you expect the sums to get.
            // "X3" gives you 3 hex digits, "X4" gives 4, etc.
            var prefixHex = prefixValue.ToString("X3");  // e.g. 314 -> "13A" → "013A" if you use X4

            // JobID block – 6-digit decimal here
            var jobPart = jobId.ToString("D6");          // 123 -> "000123"
            return $"{prefixHex}-{jobPart}";
        }
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
