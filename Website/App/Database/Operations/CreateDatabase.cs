using System.Text;

namespace Website.App.Database.Operations
{
    internal static class CreateDatabase
    {
        internal static void Begin()
        {
            using Helper.Connection connection = Database.Shared.Connection(Schema.Operations.Database);
            QuoteStatus(connection);
            QuoteRejectionReason(connection);

            RatePolicy(connection);

            JobType(connection);
            JobSubType(connection);
            JobSubTypeOption(connection);

            Quotes(connection);
            Orders(connection);
            OrderItems(connection);
            OrderTemplates(connection);
            Invoices(connection);
            InvoiceOrders(connection);
            Transactions(connection);
        }

        #region Lookup Tables
        private static void QuoteStatus(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS QUOTE_STATUS (")
            .AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,")
            .AppendLine("  NAME TEXT NOT NULL UNIQUE,")
            .AppendLine("  DESCRIPTION TEXT,")
            .AppendLine("  ISVISIBLE INTEGER NOT NULL DEFAULT 1,")
            .AppendLine("  ISTERMINAL INTEGER NOT NULL DEFAULT 0,")
            .AppendLine("  SORTORDER INTEGER NOT NULL")
            .AppendLine(")");
            connection.ExecuteNonQuery(sb.ToString());

            string insertline = "INSERT OR IGNORE INTO QUOTE_STATUS(ID, NAME, DESCRIPTION, ISVISIBLE, ISTERMINAL, SORTORDER) ";
            sb.Clear(); sb.AppendLine(insertline).AppendLine("VALUES (0,\"Draft\", \"Created, not yet shown to user\",1,0,10)");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(insertline).AppendLine("VALUES (1,\"Offered\",\"Offered to user\",1,0,20)");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(insertline).AppendLine("VALUES (2,\"Accepted\",\"Approved for conversion\",1,0,30)");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(insertline).AppendLine("VALUES (3,\"Rejected\",\"Declined by user\",1,1,40)");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(insertline).AppendLine("VALUES (4,\"Expired\",\"Timed out\",1,1,50)");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(insertline).AppendLine("VALUES (5,\"ConvertedToJob\",\"Accepted, job created\",1,1,35)");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(insertline).AppendLine("VALUES (6,\"Archived\",\"No longer active\",1,1,90)");
            connection.ExecuteNonQuery(sb.ToString());
        }
        private static void QuoteRejectionReason(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS QUOTE_REJECTION_REASON (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  NAME TEXT NOT NULL UNIQUE,");
            sb.AppendLine("  DESCRIPTION TEXT,");
            sb.AppendLine("  ISSYSTEM INTEGER NOT NULL DEFAULT 1,");
            sb.AppendLine("  SORTORDER INTEGER NOT NULL,");
            sb.AppendLine("  ISRETRYABLE INTEGER NOT NULL DEFAULT 0");
            sb.AppendLine(")");
            connection.ExecuteNonQuery(sb.ToString());

            string insertline = "INSERT OR IGNORE INTO QUOTE_REJECTION_REASON(ID, NAME, DESCRIPTION, ISSYSTEM, SORTORDER, ISRETRYABLE) ";
            sb.Clear(); sb.AppendLine(insertline).AppendLine("VALUES (1,\"Manual Decline\",\"User declined manually\",1,10,1)");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(insertline).AppendLine("VALUES (2,\"Job Cancelled\",\"Accepted quote cancelled job\",1,20,1)");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(insertline).AppendLine("VALUES (3,\"Price To High\",\"Declined for pricing\",1,30,1)");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(insertline).AppendLine("VALUES (4,\"Details Changed\",\"Quote invalid due to changed details\",1,40,1)");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(insertline).AppendLine("VALUES (5,\"Superseded\",\"Replaced by newer quote\",1,50,0)");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(insertline).AppendLine("VALUES (6,\"Admin Override\",\"Rejected by admin\",1,60,0)");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(insertline).AppendLine("VALUES (7,\"Auto Invalidated\",\"System auto-invalidated\",1,70,0)");
            connection.ExecuteNonQuery(sb.ToString());
        }
        private static void RatePolicy(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS RATE_POLICY (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  NAME TEXT NOT NULL UNIQUE,");
            sb.AppendLine("  DESCRIPTION TEXT,");
            sb.AppendLine("  CREATEDOADATE REAL NOT NULL,");
            sb.AppendLine("  UPDATEDOADATE REAL NOT NULL");
            sb.AppendLine(")");
            connection.ExecuteNonQuery(sb.ToString());
        }
        private static void JobType(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS JOB_TYPE (")
              .AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,")
              .AppendLine("  NAME TEXT NOT NULL UNIQUE,")
              .AppendLine("  DESCRIPTION TEXT,")
              .AppendLine("  CREATEDOADATE REAL NOT NULL,")
              .AppendLine("  UPDATEDOADATE REAL NOT NULL")
              .AppendLine(")");
            connection.ExecuteNonQuery(sb.ToString());

            double today = DateTime.Today.ToOADate();
            string insertLine = "INSERT OR IGNORE INTO JOB_TYPE(ID, NAME, DESCRIPTION, CREATEDOADATE, UPDATEDOADATE) ";
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (1, \"Delivery\", \"Parcel or item delivery\", {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (2, \"Ride Share\", \"Passenger transport\", {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (3, \"Property Maintenance\", \"Internal and external property maintenance\", {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());
        }
        private static void JobSubType(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS JOB_SUBTYPE (")
              .AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,")
              .AppendLine("  JOB_TYPE_ID INTEGER NOT NULL,")
              .AppendLine("  NAME TEXT NOT NULL,")
              .AppendLine("  DESCRIPTION TEXT,")
              .AppendLine("  PARENT_ID INTEGER NULL,")
              .AppendLine("  CREATEDOADATE REAL NOT NULL,")
              .AppendLine("  UPDATEDOADATE REAL NOT NULL,")
              .AppendLine("  FOREIGN KEY(JOB_TYPE_ID) REFERENCES JOB_TYPE(ID),")
              .AppendLine("  FOREIGN KEY(PARENT_ID) REFERENCES JOB_SUBTYPE(ID)")
              .AppendLine(")");
            connection.ExecuteNonQuery(sb.ToString());

            double today = DateTime.Today.ToOADate();
            string insertLine = "INSERT OR IGNORE INTO JOB_SUBTYPE(ID, JOB_TYPE_ID, NAME, DESCRIPTION, PARENT_ID, CREATEDOADATE, UPDATEDOADATE) ";

            // Delivery sizes
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (1, 1, \"DeliverySize\", \"Size of parcel(s)\", NULL, {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());

            // Ride Share passenger count
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (2, 2, \"PassengerCount\", \"Number of passengers\", NULL, {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());

            // Property Maintenance hierarchy
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (3, 3, \"Indoor\", \"Indoor tasks\", NULL, {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (4, 3, \"Outdoor\", \"Outdoor tasks\", NULL, {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());

            // Indoor Cleaning subtypes
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (5, 3, \"Cleaning\", \"Indoor cleaning tasks\", 3, {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (6, 3, \"Painting\", \"Indoor painting tasks\", 3, {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (7, 3, \"PaperHanging\", \"Indoor paper hanging\", 3, {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());

            // Leaf cleaning tasks
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (8, 3, \"Carpets/Rugs\", \"Carpet and rug cleaning\", 5, {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (9, 3, \"Surfaces\", \"Surface cleaning\", 5, {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (10, 3, \"Ovens\", \"Oven cleaning\", 5, {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (11, 3, \"Vacuuming\", \"Vacuuming tasks\", 5, {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());

            // Outdoor maintenance
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (12, 3, \"Gardening\", \"Outdoor gardening\", 4, {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (13, 3, \"LawnMowing\", \"Lawn mowing tasks\", 4, {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());
        }
        private static void JobSubTypeOption(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS JOB_SUBTYPE_OPTION (")
              .AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,")
              .AppendLine("  JOB_SUBTYPE_ID INTEGER NOT NULL,")
              .AppendLine("  NAME TEXT NOT NULL,")
              .AppendLine("  VALUE INTEGER NULL,")
              .AppendLine("  DESCRIPTION TEXT,")
              .AppendLine("  CREATEDOADATE REAL NOT NULL,")
              .AppendLine("  UPDATEDOADATE REAL NOT NULL,")
              .AppendLine("  FOREIGN KEY(JOB_SUBTYPE_ID) REFERENCES JOB_SUBTYPE(ID)")
              .AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());

            double today = DateTime.Today.ToOADate();
            string insertLine = "INSERT OR IGNORE INTO JOB_SUBTYPE_OPTION(JOB_SUBTYPE_ID, NAME, VALUE, DESCRIPTION, CREATEDOADATE, UPDATEDOADATE) ";

            // Delivery parcel sizes
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (1, 'SMALL', 1, '1 Bag or Up to (1 Kg)', {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (1, 'MEDIUM', 2, 'Up to 2 Bags or (2–5 Kg)', {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (1, 'LARGE', 3, 'Between 3 and 5 Bags (5–10 Kg)', {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());
            sb.Clear(); sb.AppendLine(insertLine + $"VALUES (1, 'OVERSIZE', 4, '5 or more Bags (10+ Kg)', {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());

            // Ride Share passengers
            for (int i = 1; i <= 8; i++)
            {
                sb.Clear(); sb.AppendLine(insertLine + $"VALUES (2, '{i} Passenger{(i > 1 ? "s" : "")}', {i}, 'Passenger count option', {today}, {today})");
                connection.ExecuteNonQuery(sb.ToString());
            }
        }
        #endregion

        #region Operations Tables
        private static void Quotes(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS QUOTES (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  USER_ID INTEGER NOT NULL,");
            sb.AppendLine("  CREATEDBY_USER_ID INTEGER NOT NULL,");
            sb.AppendLine("  PICKUP_ADDRESS_ID INTEGER NOT NULL,");
            sb.AppendLine("  DROPOFF_ADDRESS_ID INTEGER NOT NULL,");
            sb.AppendLine("  DISTANCE_METRES REAL NOT NULL,");
            sb.AppendLine("  DURATION_SECONDS REAL NOT NULL,");
            sb.AppendLine("  CUSTOMER_PRICE REAL,");
            sb.AppendLine("  MARKUP_MULTIPLIER REAL,");
            sb.AppendLine("  DRIVER_PAYOUT REAL,");
            sb.AppendLine("  RATE_POLICY_ID INTEGER,");
            sb.AppendLine("  QUOTE_STATUS_ID INTEGER NOT NULL,");
            sb.AppendLine("  PICKUP_TYPE_ID INTEGER NOT NULL,");
            sb.AppendLine("  VEHICLE_TYPE_ID INTEGER NOT NULL,");
            sb.AppendLine("  JOB_ID INTEGER,");
            sb.AppendLine("  REQUESTED_TIME REAL NOT NULL,");
            sb.AppendLine("  CREATED_AT REAL NOT NULL,");
            sb.AppendLine("  VALID_UNTIL REAL,");
            sb.AppendLine("  NOTES TEXT,");
            sb.AppendLine("  QUOTE_REJECTION_REASON_ID INTEGER,");
            sb.AppendLine("  FOREIGN KEY(QUOTE_STATUS_ID) REFERENCES QUOTE_STATUS(ID),");
            sb.AppendLine("  FOREIGN KEY(QUOTE_REJECTION_REASON_ID) REFERENCES QUOTE_REJECTION_REASON(ID),");
            sb.AppendLine("  FOREIGN KEY(RATE_POLICY_ID) REFERENCES RATE_POLICY(ID),");
            sb.AppendLine("  FOREIGN KEY(PICKUP_TYPE_ID) REFERENCES PICKUP_TYPE(ID),");
            sb.AppendLine("  FOREIGN KEY(VEHICLE_TYPE_ID) REFERENCES VEHICLE_TYPE(ID)");
            sb.AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());

            // Optional index for quick lookups
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_QUOTES_USER ON QUOTES(USER_ID);");
        }

        private static void Orders(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS ORDERS (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  USER_ID INTEGER NOT NULL,");
            sb.AppendLine("  COMPANY_ID INTEGER,");
            sb.AppendLine("  PREFIX_ID INTEGER,");
            sb.AppendLine("  QUOTE_ID INTEGER,");
            sb.AppendLine("  NUMBER TEXT NOT NULL UNIQUE,");
            sb.AppendLine("  PRICE REAL NOT NULL,");
            sb.AppendLine("  GST_AMOUNT REAL NOT NULL,");
            sb.AppendLine("  TOTAL_PRICE REAL NOT NULL,");
            sb.AppendLine("  CREATED_OADATE REAL NOT NULL,");
            sb.AppendLine("  UPDATED_OADATE REAL NOT NULL,");
            sb.AppendLine("  FOREIGN KEY(QUOTE_ID) REFERENCES QUOTES(ID)");
            sb.AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());
        }
        private static void OrderItems(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS ORDER_ITEMS (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  ORDER_ID INTEGER NOT NULL,");
            sb.AppendLine("  ITEM_TEMPLATE_ID INTEGER NOT NULL,");
            sb.AppendLine("  QUANTITY REAL NOT NULL,");
            sb.AppendLine("  PRICE REAL NOT NULL,");
            sb.AppendLine("  GST_AMOUNT REAL NOT NULL,");
            sb.AppendLine("  TOTAL_PRICE REAL NOT NULL,");
            sb.AppendLine("  FOREIGN KEY(ORDER_ID) REFERENCES ORDERS(ID)");
            sb.AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());
        }
        private static void OrderTemplates(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS ORDER_TEMPLATES (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  NAME TEXT NOT NULL UNIQUE,");
            sb.AppendLine("  DESCRIPTION TEXT,");
            sb.AppendLine("  PRICE REAL NOT NULL,");
            sb.AppendLine("  GST_AMOUNT REAL NOT NULL,");
            sb.AppendLine("  CREATED_OADATE REAL NOT NULL,");
            sb.AppendLine("  UPDATED_OADATE REAL NOT NULL");
            sb.AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());
        }

        private static void Invoices(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS INVOICES (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  INVOICE_NUMBER TEXT NOT NULL UNIQUE,");
            sb.AppendLine("  TOTAL REAL NOT NULL,");
            sb.AppendLine("  GST_AMOUNT REAL NOT NULL,");
            sb.AppendLine("  USER_ID INTEGER NOT NULL,");
            sb.AppendLine("  COMPANY_ID INTEGER,");
            sb.AppendLine("  CREATED_OADATE REAL NOT NULL,");
            sb.AppendLine("  UPDATED_OADATE REAL NOT NULL");
            sb.AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());
        }
        private static void InvoiceOrders(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS INVOICE_ORDERS (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  INVOICE_ID INTEGER NOT NULL,");
            sb.AppendLine("  ORDER_ID INTEGER NOT NULL,");
            sb.AppendLine("  FOREIGN KEY(INVOICE_ID) REFERENCES INVOICES(ID),");
            sb.AppendLine("  FOREIGN KEY(ORDER_ID) REFERENCES ORDERS(ID)");
            sb.AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());
        }
        private static void Transactions(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS TRANSACTIONS (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  INVOICE_ID INTEGER NOT NULL,");
            sb.AppendLine("  AMOUNT REAL NOT NULL,");
            sb.AppendLine("  TYPE TEXT NOT NULL,"); // e.g., "InvoiceIssue"
            sb.AppendLine("  CREATED_OADATE REAL NOT NULL,");
            sb.AppendLine("  FOREIGN KEY(INVOICE_ID) REFERENCES INVOICES(ID)");
            sb.AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());
        }
        #endregion
    }
}
