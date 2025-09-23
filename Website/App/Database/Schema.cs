namespace Website.App.Database
{
    internal static class Schema
    {
        internal static class Locations
        {
            internal const string Database = "Locations.db";

            internal static class Tables
            {
                internal const string Country = "COUNTRY";
                internal const string Region = "REGION";
                internal const string Place = "PLACE";
                internal const string Locality = "LOCALITY";
                internal const string AddressType = "ADDRESS_TYPE";
                internal const string Address = "ADDRESS";
            }
        }

        internal static class Entities
        {
            internal const string Database = "Entities.db";

            internal static class Tables
            {
                internal const string User = "USER";
                internal const string UserAddress = "USER_ADDRESS";
                internal const string Roles = "ROLES";
                internal const string UserRole = "USER_ROLE";
                // Add more as needed...
            }
        }
    }
}
