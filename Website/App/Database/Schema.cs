namespace Website.App.Database
{
    internal static class Schema
    {
        internal static class All
        {
            internal const string SchemaName = "public";
        }
        internal static class Locations
        {
            internal const string SchemaName = "loc";
            internal const string Country = "loc.country";
            internal const string Region = "loc.region";
            internal const string Place = "loc.place";
            internal const string Locality = "loc.locality";
            internal const string AddressType = "loc.address_types";
            internal const string Address = "loc.address";
            internal const string ZonesBase = "loc.zones_base";
            internal const string ZonesRCI = "loc.zones_rci";
            internal const string ZonesLive = "loc.zones_live";
        }
        internal static class Entities
        {
            internal const string SchemaName = "ent";
            internal const string Users = "ent.users";
            internal const string UserAddress = "ent.user_address";

            internal const string Roles = "ent.roles";
            internal const string UserRoles = "ent.user_roles";

            internal const string Driver = "ent.driver";
            internal const string DriverLicenceClass = "ent.driver_licence_class";
            internal const string DriverLicenceEndorsements = "ent.driver_licence_endorsements";
            internal const string DriverLicenceEndorsementType = "ent.driver_licence_endorsement_type";
            internal const string DriverDocuments = "ent.driver_documents";
            internal const string DriverVehicles = "ent.driver_vehicles";

            internal const string VehicleClass = "ent.vehicle_class";
            internal const string VehicleType = "ent.vehicle_type";
        }
        internal static class Operations
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
        internal static class LiveOps
        {
            internal const string DriverStatusType = "DRIVER_STATUS_TYPE";
            internal const string DriverStatus = "DRIVER_STATUS";
            internal const string DriverStatusHistory = "DRIVER_STATUS_HISTORY";
            internal const string DriverLocationHistory = "DRIVER_LOCATION_HISTORY";
            internal const string DriverHeartBeat = "DRIVER_HEARTBEAT";
        }
    }
}
