using System.Text;

namespace Website.App.Database.LiveOps
{
    public class CreateDatabase
    {
        internal static void Begin()
        {
            using Helper.Connection connection = Database.Shared.Connection(Schema.LiveOps.Database);
            DriverStatusType(connection);
            DriverStatus(connection);
            DriverStatusHistory(connection);

            DriverLocationHistory(connection);
            DriverHeartbeat(connection);
        }
        private static void DriverStatusType(Helper.Connection connection)
        {
            // 1️⃣ Create table
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS DRIVER_STATUS_TYPE (")
              .AppendLine("  ID INTEGER PRIMARY KEY,")                             // stable numeric codes
              .AppendLine("  NAME TEXT NOT NULL UNIQUE,")
              .AppendLine("  DESCRIPTION TEXT,")
              .AppendLine("  PARENT_ID INTEGER NULL,")                           // hierarchical parent
              .AppendLine("  CATEGORY TEXT NULL,")                               // e.g. 'OFFER_FLOW', 'JOB_FLOW', 'SYSTEM'
              .AppendLine("  NEXT_ALLOWED_STATUSES TEXT NULL,")                  // comma-separated list of allowed next state IDs
              .AppendLine("  IS_TERMINAL INTEGER NOT NULL DEFAULT 0,")
              .AppendLine("  SORTORDER INTEGER NOT NULL DEFAULT 0,")
              .AppendLine("  FOREIGN KEY(PARENT_ID) REFERENCES DRIVER_STATUS_TYPE(ID)")
              .AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());

            // 2️⃣ Seed data
            string sqlLine = "INSERT OR IGNORE INTO DRIVER_STATUS_TYPE(ID, NAME, DESCRIPTION, PARENT_ID, CATEGORY, NEXT_ALLOWED_STATUSES, IS_TERMINAL, SORTORDER) ";

            // --- SYSTEM STATES ---
            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (0, 'OFFLINE', 'Driver not logged in / app offline', NULL, 'SYSTEM', '1', 0, 10);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (1, 'STANDBY', 'Driver available and idle', NULL, 'SYSTEM', '2,12,13', 0, 20);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (12, 'ON_BREAK', 'Paused by driver', NULL, 'SYSTEM', '1', 0, 80);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (13, 'UNAVAILABLE', 'Off-duty / unavailable', NULL, 'SYSTEM', '1', 0, 85);");
            connection.ExecuteNonQuery(sb.ToString());

            // --- OFFER FLOW ---
            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (100, 'OFFER_FLOW', 'Root for offer-related states', NULL, 'GROUP', '14,15,16,17', 0, 90);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (14, 'OFFER_PENDING', 'Offer sent, pending acceptance', 100, 'OFFER_FLOW', '15,16,17', 0, 91);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (15, 'OFFER_ACCEPTED', 'Driver accepted offer', 100, 'OFFER_FLOW', '3,26', 0, 92);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (16, 'OFFER_DECLINED', 'Driver declined offer', 100, 'OFFER_FLOW', NULL, 1, 93);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (17, 'OFFER_EXPIRED', 'Offer expired (no response)', 100, 'OFFER_FLOW', NULL, 1, 94);");
            connection.ExecuteNonQuery(sb.ToString());

            // --- JOB FLOW ---
            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (200, 'JOB_FLOW', 'Root for active job states', NULL, 'GROUP', '2,3,5,6,7,8,9,10,11,18,19,20,28,29', 0, 100);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (2, 'COMPLETING_CURRENT_JOB', 'Finishing current job', 200, 'JOB_FLOW', '3,11', 0, 101);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (3, 'ENROUTE_TO_PICKUP', 'Heading to pickup location', 200, 'JOB_FLOW', '4', 0, 102);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (4, 'ARRIVED_AT_PICKUP', 'At pickup location', 200, 'JOB_FLOW', '6,19', 0, 103);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (5, 'HEADING_TO_NEXT_PICKUP', 'Heading to next pickup', 200, 'JOB_FLOW', '28', 0, 104);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (6, 'ITEMS_ACCEPTED', 'Items accepted / in possession', 200, 'JOB_FLOW', '7', 0, 105);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (7, 'ENROUTE_TO_ADDRESS', 'Heading to delivery/dropoff', 200, 'JOB_FLOW', '8,9', 0, 106);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (8, 'ARRIVED_AT_ADDRESS', 'Arrived at delivery/dropoff', 200, 'JOB_FLOW', '9,20', 0, 107);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (9, 'DELIVERING', 'Performing handover', 200, 'JOB_FLOW', '10', 0, 108);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (10, 'COMPLETED_DELIVERY', 'Delivery completed', 200, 'JOB_FLOW', NULL, 1, 109);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (11, 'RETURNING_TO_BASE', 'Returning to base', 200, 'JOB_FLOW', '1', 0, 110);");
            connection.ExecuteNonQuery(sb.ToString());
            // Multi-pickup
            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (28, 'COLLECTING_PICKUPS', 'Collecting multiple pickups', 200, 'JOB_FLOW', '29', 0, 111);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (29, 'CONSOLIDATING_LOAD', 'Consolidating items / staging', 200, 'JOB_FLOW', '7', 0, 112);");
            connection.ExecuteNonQuery(sb.ToString());

            // Vendor / pickup
            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (18, 'WAITING_FOR_VENDOR', 'Waiting at vendor', 200, 'JOB_FLOW', '19', 0, 113);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (19, 'LOADING_GOODS', 'Loading goods at pickup', 200, 'JOB_FLOW', '6', 0, 114);");
            connection.ExecuteNonQuery(sb.ToString());

            // Delivery / dropoff
            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (20, 'UNLOADING_GOODS', 'Unloading goods at dropoff', 200, 'JOB_FLOW', '10', 0, 115);");
            connection.ExecuteNonQuery(sb.ToString());

            // --- UTILITY / SYSTEM FLAGS ---
            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (21, 'MAINTENANCE_REQUIRED', 'Vehicle/service maintenance required', NULL, 'UTILITY', NULL, 0, 130);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (22, 'BLOCKED_BY_TRAFFIC', 'Blocked by traffic / heavy delay', NULL, 'UTILITY', NULL, 0, 140);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (23, 'PAUSED_BY_SYSTEM', 'Paused by system / admin action', NULL, 'UTILITY', NULL, 0, 150);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (24, 'STUCK', 'Stuck / incident (requires help)', NULL, 'UTILITY', NULL, 0, 160);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (25, 'JOB_CANCELLED', 'Job cancelled by customer/system', NULL, 'UTILITY', NULL, 1, 170);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (26, 'ASSIGNED_NO_ACTION', 'Assigned but driver has not acted', NULL, 'UTILITY', NULL, 0, 175);");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + "VALUES (27, 'NAVIGATING', 'Actively navigating using map', NULL, 'UTILITY', NULL, 0, 180);");
            connection.ExecuteNonQuery(sb.ToString());
        }
        private static void DriverStatus(Helper.Connection connection)
        {
            StringBuilder sb = new();

            // --- DRIVER STATUS TABLE ---
            sb.AppendLine("CREATE TABLE IF NOT EXISTS DRIVER_STATUS (")
              .AppendLine("  DRIVER_ID INTEGER PRIMARY KEY,")                    // Driver = UserID with driver role
              .AppendLine("  STATUS_ID INTEGER NOT NULL,")                    // FK to DRIVER_STATUS_TYPE.ID
              .AppendLine("  CURRENT_JOB_ID INTEGER NULL,")                  // Optional: current active job
              .AppendLine("  LAST_HEARTBEAT_OADATE REAL NOT NULL,")         // Last update timestamp
              .AppendLine("  LOCATION_LATITUDE REAL NULL,")
              .AppendLine("  LOCATION_LONGITUDE REAL NULL,")
              .AppendLine("  LOCATION_ACCURACY_M REAL NULL,")
              .AppendLine("  SPEED REAL NULL,")
              .AppendLine("  HEADING REAL NULL,")
              .AppendLine("  GEOHASH TEXT NULL,")                            // For spatial indexing / queries
              .AppendLine("  IS_STAGING INTEGER NOT NULL DEFAULT 0,")       // Staging / preview flag
              .AppendLine("  CREATEDOADATE REAL NOT NULL,")
              .AppendLine("  UPDATEDOADATE REAL NOT NULL,")
              .AppendLine("  FOREIGN KEY(STATUS_ID) REFERENCES DRIVER_STATUS_TYPE(ID)")
              .AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());

            // --- INDEXES ---
            // Fast lookup by driver
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_DRIVER_STATUS_STATUS ON DRIVER_STATUS(STATUS_ID)");
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_DRIVER_STATUS_HEARTBEAT ON DRIVER_STATUS(LAST_HEARTBEAT_OADATE)");
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_DRIVER_STATUS_GEOHASH ON DRIVER_STATUS(GEOHASH)");

            // Optional: For workflow validation (if using transitions table)
            connection.ExecuteNonQuery(
                @"CREATE TABLE IF NOT EXISTS DRIVER_STATUS_TRANSITION (
                FROM_STATUS_ID INTEGER NOT NULL,
                TO_STATUS_ID INTEGER NOT NULL,
                PRIMARY KEY(FROM_STATUS_ID, TO_STATUS_ID),
                FOREIGN KEY(FROM_STATUS_ID) REFERENCES DRIVER_STATUS_TYPE(ID),
                FOREIGN KEY(TO_STATUS_ID) REFERENCES DRIVER_STATUS_TYPE(ID)
                )"
            );
        }
        private static void DriverStatusHistory(Helper.Connection connection)
        {
            StringBuilder sb = new();

            // 1️⃣ Create table
            sb.AppendLine("CREATE TABLE IF NOT EXISTS DRIVER_STATUS_HISTORY (")
              .AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,")
              .AppendLine("  DRIVER_ID INTEGER NOT NULL,")                  // FK to Users/Drivers
              .AppendLine("  JOB_ID INTEGER NULL,")                      // Optional: which job this status belongs to
              .AppendLine("  STATUS_ID INTEGER NOT NULL,")              // FK to DRIVER_STATUS_TYPE.ID
              .AppendLine("  OADATE REAL NOT NULL,")                    // timestamp
              .AppendLine("  LATITUDE REAL NULL,")
              .AppendLine("  LONGITUDE REAL NULL,")
              .AppendLine("  ACCURACY_M REAL NULL,")
              .AppendLine("  SPEED REAL NULL,")
              .AppendLine("  HEADING REAL NULL,")
              .AppendLine("  GEOHASH TEXT NULL,")
              .AppendLine("  NOTES TEXT NULL,")                        // optional free text / alerts
              .AppendLine("  FOREIGN KEY(STATUS_ID) REFERENCES DRIVER_STATUS_TYPE(ID)")
              .AppendLine(");");

            connection.ExecuteNonQuery(sb.ToString());

            // 2️⃣ Indexes for fast querying
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_DRIVER_STATUS_HISTORY_DRIVER ON DRIVER_STATUS_HISTORY(DRIVER_ID);");
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_DRIVER_STATUS_HISTORY_JOB ON DRIVER_STATUS_HISTORY(JOB_ID);");
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_DRIVER_STATUS_HISTORY_OADATE ON DRIVER_STATUS_HISTORY(OADATE);");
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_DRIVER_STATUS_HISTORY_GEOHASH ON DRIVER_STATUS_HISTORY(GEOHASH);");
        }
        private static void DriverLocationHistory(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS DRIVER_LOCATION_HISTORY (")
              .AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,")
              .AppendLine("  DRIVER_ID INTEGER NOT NULL,")
              .AppendLine("  OADATE REAL NOT NULL,")
              .AppendLine("  LATITUDE REAL NOT NULL,")
              .AppendLine("  LONGITUDE REAL NOT NULL,")
              .AppendLine("  ACCURACY_M REAL NULL,")
              .AppendLine("  SPEED REAL NULL,")
              .AppendLine("  HEADING REAL NULL,")
              .AppendLine("  PROVIDER TEXT NULL,")
              .AppendLine("  GEOHASH TEXT NULL")
              .AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());

            // Index for queries by driver + time
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_DRIVER_LOC_HISTORY_DRIVER_TIME ON DRIVER_LOCATION_HISTORY(DRIVER_ID, OADATE);");

            // Optional spatial: if you compile SQLite with rtree module, consider creating an RTREE for fast spatial queries.
        }
        private static void DriverHeartbeat(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS DRIVER_HEARTBEAT (")
              .AppendLine("  DRIVER_ID INTEGER PRIMARY KEY,") // upsert semantics — keep latest
              .AppendLine("  LAST_HEARTBEAT_OADATE REAL NOT NULL,")
              .AppendLine("  LAST_LATITUDE REAL NULL,")
              .AppendLine("  LAST_LONGITUDE REAL NULL,")
              .AppendLine("  PING_MS INTEGER NULL")
              .AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());
        }
    }
}
