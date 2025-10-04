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
                internal const string UserRoles = "USER_ROLES";

                internal const string Driver = "DRIVER";
                internal const string DriverLicenceClass = "DRIVER_LICENCE_CLASS";
                internal const string DriverLicenceEndorsements = "DRIVER_LICENCE_ENDORSEMENTS";
                internal const string DriverLicenceEndorsementType = "DRIVER_LICENCE_ENDORSEMENT_TYPE";
                internal const string DriverDocuments = "DRIVER_DOCUMENTS";
                internal const string DriverVehicles = "DRIVER_VEHICLES";

                internal const string VehicleClass = "VEHICLE_CLASS";
                internal const string VehicleType = "VEHICLE_TYPE";
            }
        }
        internal static class Operations
        {
            internal const string Database = "Operations.db";
            internal static class Tables
            {
                internal const string Quotes = "QUOTES";
                internal const string QuoteStatus = "QUOTE_STATUS";
                internal const string QuoteRejectionReason = "QUOTE_REJECTION_REASON";

                internal const string RatePolicy = "RATE_POLICY";

                internal const string JobType = "JOB_TYPE";
                internal const string JobSubType = "JOB_SUBTYPE";
                internal const string JobSubTypeOption = "JOB_SUBTYPE_OPTION";

                internal const string Orders = "ORDERS";
                internal const string OrderItems = "ORDER_ITEMS";
                internal const string OrderTemplates = "ORDER_TEMPLATES";

                internal const string Invoices = "INVOICES";
                internal const string InvoiceOrders = "INVOICE_ORDERS";
                internal const string Transactions = "TRANSACTIONS";
            }
        }
        internal static class LiveOps
        {
            internal const string Database = "LiveOps.db";
            internal static class Tables
            {
                internal const string DriverStatusType = "DRIVER_STATUS_TYPE";
                internal const string DriverStatus = "DRIVER_STATUS";
                internal const string DriverStatusHistory = "DRIVER_STATUS_HISTORY";
                internal const string DriverLocationHistory = "DRIVER_LOCATION_HISTORY";
                internal const string DriverHeartBeat = "DRIVER_HEARTBEAT";
            }
        }
    }
}
