namespace Website.App.Database
{
    internal static class Schema
    {
        internal static class Public
        {
            internal const string Name = "pub";
        }
        internal static class Locations
        {
            internal const string Name = "loc";
            internal static class Tables
            {
                internal const string Country = $"{Name}.country";
                internal const string Region = $"{Name}.region";
                internal const string Place = $"{Name}.place";
                internal const string Locality = $"{Name}.locality";
                internal const string AddressType = $"{Name}.address_types";
                internal const string Address = $"{Name}.address";
                internal const string ZonesBase = $"{Name}.zones_base";
                internal const string ZonesRCI = $"{Name}.zones_rci";
                internal const string ZonesLive = $"{Name}.zones_live";
            }
        }
        internal static class Entities
        {
            internal const string Name = "ent";
            internal static class Tables
            {
                internal const string Users = $"{Name}.users";
                internal const string UserAddress = $"{Name}.user_address";
                internal const string Roles = $"{Name}.roles";
                internal const string UserRoles = $"{Name}.user_roles";
                internal const string Driver = $"{Name}.driver";
                internal const string DriverLicenceClass = $"{Name}.driver_licence_class";
                internal const string DriverDocuments = $"{Name}.driver_documents";
                internal const string DriverVehicles = $"{Name}.driver_vehicles";

                internal const string UserPayment = $"{Name}.user_payment";
            }
        }
        internal static class Config
        {
            internal const string Name = "cfg";
            internal static class Tables
            {
                internal const string DriverLicenceEndorsements = $"{Name}.driver_licence_endorsements";
                internal const string DriverLicenceEndorsementType = $"{Name}.driver_licence_endorsement_type";
                internal const string DropoffType = $"{Name}.dropoff_type";
                internal const string PackageSize = $"{Name}.package_size";
                internal const string PackageType = $"{Name}.package_type";
                internal const string PackageTypeCategory = $"{Name}.package_type_category";
                internal const string Modes = $"{Name}.modes";
                internal const string RatePolicies = $"{Name}.rate_policies";
                internal const string VehicleClass = $"{Name}.vehicle_class";
                internal const string VehicleType = $"{Name}.vehicle_type";

                internal const string AllocationStatus = $"{Name}.allocation_status";
                internal const string JobEventCode = $"{Name}.job_event_code";
                internal const string JobStatus = $"{Name}.job_status";
                internal const string OfferState = $"{Name}.offer_state";
                internal const string PaymentType = $"{Name}.payment_type";
                internal const string PaymentTypeProvider = $"{Name}.payment_type_provider";
                internal const string PaymentTypeProviderStatus = $"{Name}.payment_type_provider_status";
            }
        }
        internal static class Operations
        {
            internal const string Name = "ops";
            internal static class Tables
            {
                internal const string Job = $"{Name}.job";
                internal const string JobEvent = $"{Name}.job_event";
                internal const string JobTask = $"{Name}.job_task";
                internal const string ParticipantAssignment = $"{Name}.participant_assignment";
                internal const string ParticipantOffer = $"{Name}.participant_offer";
            }
        }
        internal static class Accounting
        {
            internal const string Name = "acc";

            internal static class Tables
            {
                internal const string Currency = $"{Name}.currency";
                internal const string AccountType = $"{Name}.account_type";
                internal const string Account = $"{Name}.account";
                internal const string TaxCode = $"{Name}.tax_code";
                internal const string Period = $"{Name}.period";
                internal const string Journal = $"{Name}.journal";
                internal const string JournalLine = $"{Name}.journal_line";
                internal const string SubledgerType = $"{Name}.subledger_type";
                internal const string SubledgerLink = $"{Name}.subledger_link";

                // helper views (not tables, but keep them addressable)
                internal const string ViewTrialBalance = $"{Name}.trial_balance";
                internal const string ViewJournalBalance = $"{Name}.journal_balance";
                internal const string ViewGstSummary = $"{Name}.gst_summary";
            }
        }
        internal static class Messaging
        {
            internal const string Name = "msg";
            internal static class Tables
            {
                internal const string MessageType = $"{Name}.message_type";
                internal const string MessageThread = $"{Name}.message_thread";
                internal const string MessageThreadUser = $"{Name}.message_thread_user";
                internal const string Message = $"{Name}.message";

                internal const string Notification = $"{Name}.notification";
                internal const string NotificationEvent = $"{Name}.notification_event";
                internal const string NotificationType = $"{Name}.notification_type";
            }
        }
    }
}
