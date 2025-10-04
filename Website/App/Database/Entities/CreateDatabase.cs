using System.Text;

namespace Website.App.Database.Entities
{
    internal static class CreateDatabase
    {
        internal static void Begin()
        {
            using Helper.Connection connection = Database.Shared.Connection(Schema.Entities.Database);

            // User tables
            User(connection);
            UserAddress(connection);

            // Roles
            Roles(connection);
            UserRole(connection);

            // Vehicle tables
            VehicleClass(connection);
            VehicleType(connection);

            // Driver table (must come before any FK references)
            Driver(connection);

            // Driver license tables
            DriverLicenceClass(connection);               // now safe, DRIVER exists
            DriverLicenceEndorsementType(connection);
            DriverLicenceEndorsements(connection);       // now safe, DRIVER exists

            // Driver-related tables
            DriverVehicles(connection);
            DriverDocuments(connection);
        }
        private static void User(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS USER (");
            sb.AppendLine("ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("FULLNAME TEXT,");
            sb.AppendLine("EMAIL TEXT UNIQUE NOT NULL,");
            sb.AppendLine("PHONE TEXT,");
            sb.AppendLine("PASSWORDHASH TEXT NOT NULL,");
            sb.AppendLine("CREATEDOADATE REAL NOT NULL,");
            sb.AppendLine("UPDATEDOADATE REAL NOT NULL)");
            connection.ExecuteNonQuery(sb.ToString());
        }
        private static void UserAddress(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS USER_ADDRESS (");
            sb.AppendLine("ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("USER_ID INTEGER NOT NULL,");
            sb.AppendLine("ADDRESS_ID INTEGER NOT NULL,");
            sb.AppendLine("LABEL TEXT,");
            sb.AppendLine("ISDEFAULT INTEGER NOT NULL DEFAULT 0,");
            sb.AppendLine("CREATEDOADATE REAL NOT NULL,");
            sb.AppendLine("UPDATEDOADATE REAL NOT NULL,");
            sb.AppendLine("ISACTIVE INTEGER NOT NULL DEFAULT 1,");
            sb.AppendLine("FOREIGN KEY(USER_ID) REFERENCES USER(ID) ON DELETE CASCADE,");
            // sb.AppendLine("FOREIGN KEY(ADDRESS_ID) REFERENCES ADDRESS(ID) ON DELETE CASCADE,");this is in the Locations.db
            sb.AppendLine("UNIQUE(USER_ID, ADDRESS_ID)"); // Prevent duplicate addresses for same user
            sb.AppendLine(");");

            connection.ExecuteNonQuery(sb.ToString());
        }
        private static void Roles(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE ROLES (");
            sb.AppendLine("ID INTEGER PRIMARY KEY AUTOINCREMENT, ");
            sb.AppendLine("NAME TEXT NOT NULL UNIQUE, ");
            sb.AppendLine("DESCRIPTION TEXT, ");
            sb.AppendLine("ROUTE TEXT NOT NULL, ");
            sb.AppendLine("ICONCLASS TEXT NOT NULL, ");
            sb.AppendLine("CREATEDOADATE REAL NOT NULL, "); // OLE Automation date
            sb.AppendLine("UPDATEDOADATE REAL NOT NULL)");   // OLE Automation date

            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear();
            double today = DateTime.Today.ToOADate();

            // Customer
            sb.AppendLine($@"
        INSERT OR IGNORE INTO ROLES(NAME, DESCRIPTION, ROUTE, ICONCLASS, CREATEDOADATE, UPDATEDOADATE)
        VALUES('Customer', 'Customers placing orders', '/orders', 'fa-solid fa-user', {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear();
            // Driver
            sb.AppendLine($@"
        INSERT OR IGNORE INTO ROLES(NAME, DESCRIPTION, ROUTE, ICONCLASS, CREATEDOADATE, UPDATEDOADATE)
        VALUES('Driver', 'Delivery drivers registered.', '/driver', 'fa-solid fa-truck', {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear();
            // Partner
            sb.AppendLine($@"
        INSERT OR IGNORE INTO ROLES(NAME, DESCRIPTION, ROUTE, ICONCLASS, CREATEDOADATE, UPDATEDOADATE)
        VALUES('Partner', 'Restaurant or business partners', '/partner', 'fa-solid fa-store', {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear();
            // Admin
            sb.AppendLine($@"
        INSERT OR IGNORE INTO ROLES(NAME, DESCRIPTION, ROUTE, ICONCLASS, CREATEDOADATE, UPDATEDOADATE)
        VALUES('Admin', 'System administrators with full access', '/admin', 'fa-solid fa-shield-halved', {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear();
            // Support
            sb.AppendLine($@"
        INSERT OR IGNORE INTO ROLES(NAME, DESCRIPTION, ROUTE, ICONCLASS, CREATEDOADATE, UPDATEDOADATE)
        VALUES('Support', 'Customer support role with limited elevated privileges', '/support', 'fa-solid fa-headset', {today}, {today})");
            connection.ExecuteNonQuery(sb.ToString());
        }
        private static void UserRole(Website.App.Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE USER_ROLES (");
            sb.AppendLine("ID INTEGER PRIMARY KEY AUTOINCREMENT, ");
            sb.AppendLine("USER_ID INTEGER NOT NULL, ");
            sb.AppendLine("ROLE_ID INTEGER NOT NULL, ");
            sb.AppendLine("GRANTEDOADATE REAL NOT NULL, "); // --OLE Automation date(stored as REAL)
            sb.AppendLine("EXPIRATIONOADATE REAL, "); // --Nullable
            sb.AppendLine("REVOKEDOADATE REAL, "); // --Nullable
            sb.AppendLine("REVOKEDREASON TEXT, "); // --Nullable reason
            sb.AppendLine("CREATEDBY_USER_ID INTEGER NOT NULL, ");
            sb.AppendLine("UPDATEDBY_USER_ID INTEGER NOT NULL, ");
            sb.AppendLine("ISACTIVE INTEGER NOT NULL DEFAULT 1, ");
            // --Foreign keys if you want referential integrity
            sb.AppendLine("FOREIGN KEY(USER_ID) REFERENCES USER(ID), ");
            sb.AppendLine("FOREIGN KEY(ROLE_ID) REFERENCES ROLES(ID) );");

            connection.ExecuteNonQuery(sb.ToString());
        }

        // Driver Tables
        private static void Driver(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS DRIVER (")
              .AppendLine("ID INTEGER PRIMARY KEY,")
              .AppendLine("SURNAME TEXT NOT NULL,")
              .AppendLine("FIRSTNAMES TEXT NOT NULL,")
              .AppendLine("DOB REAL NOT NULL,")
              .AppendLine("LICENSE_NUMBER TEXT NOT NULL,")
              .AppendLine("LICENSE_VERSION TEXT NULL,")
              .AppendLine("ISSUED REAL NULL,")
              .AppendLine("EXPIRES REAL NULL,")
              .AppendLine("PHOTO_PATH TEXT NULL,")
              .AppendLine("PHOTO_HASH TEXT NULL,")
              .AppendLine("NOTES TEXT NULL,")
              .AppendLine("APPLIED_AT REAL NOT NULL,")
              .AppendLine("APPROVED_AT REAL NULL,")
              .AppendLine("APPROVALWITHDRAWN_AT REAL NULL,")
              .AppendLine("LAST_DROVE_AT REAL NULL,")
              .AppendLine("ISACTIVE INTEGER NOT NULL DEFAULT 1,")
              .AppendLine("UPDATED_AT REAL NOT NULL,")
              .AppendLine("CHECK (EXPIRES IS NULL OR ISSUED IS NULL OR EXPIRES >= ISSUED)")
              .AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());

            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_DRIVER_ISACTIVE ON DRIVER(ISACTIVE);");
            connection.ExecuteNonQuery("CREATE UNIQUE INDEX IF NOT EXISTS IDX_DRIVER_LICENSE_NUMBER ON DRIVER(LICENSE_NUMBER);");
        }
        private static void DriverLicenceClass(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS DRIVER_LICENCE_CLASS (")
              .AppendLine("ID INTEGER PRIMARY KEY AUTOINCREMENT,")
              .AppendLine("DRIVER_ID INTEGER NOT NULL,")
              .AppendLine("VEHICLE_CLASS_ID INTEGER NOT NULL,")
              .AppendLine("ISSUED_DATE REAL NULL,")
              .AppendLine("EXPIRY_DATE REAL NULL,")
              .AppendLine("UPDATED_AT REAL NOT NULL,")
              .AppendLine("FOREIGN KEY(DRIVER_ID) REFERENCES DRIVER(ID) ON DELETE CASCADE,")
              .AppendLine("FOREIGN KEY(VEHICLE_CLASS_ID) REFERENCES VEHICLE_CLASS(ID) ON DELETE CASCADE,")
              .AppendLine("CHECK (EXPIRY_DATE IS NULL OR ISSUED_DATE IS NULL OR EXPIRY_DATE >= ISSUED_DATE)")
              .AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());

            connection.ExecuteNonQuery(
                "CREATE UNIQUE INDEX IF NOT EXISTS IDX_DRIVER_LICENCE_CLASS_UNIQUE " +
                "ON DRIVER_LICENCE_CLASS(DRIVER_ID, VEHICLE_CLASS_ID);");
        }
        private static void DriverLicenceEndorsements(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS DRIVER_LICENCE_ENDORSEMENTS (")
              .AppendLine("ID INTEGER PRIMARY KEY AUTOINCREMENT,")
              .AppendLine("DRIVER_ID INTEGER NOT NULL,")
              .AppendLine("ENDORSEMENT_TYPE_ID INTEGER NOT NULL,")
              .AppendLine("ISSUED_DATE REAL NULL,")
              .AppendLine("EXPIRY_DATE REAL NULL,")
              .AppendLine("UPDATED_AT REAL NOT NULL,")
              .AppendLine("FOREIGN KEY(DRIVER_ID) REFERENCES DRIVER(ID) ON DELETE CASCADE,")
              .AppendLine("FOREIGN KEY(ENDORSEMENT_TYPE_ID) REFERENCES DRIVER_LICENSE_ENDORSEMENT_TYPE(ID) ON DELETE CASCADE,")
              .AppendLine("CHECK (EXPIRY_DATE IS NULL OR ISSUED_DATE IS NULL OR EXPIRY_DATE >= ISSUED_DATE)")
              .AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());

            connection.ExecuteNonQuery(
                "CREATE UNIQUE INDEX IF NOT EXISTS IDX_DRIVER_LICENCE_ENDORSEMENTS_UNIQUE " +
                "ON DRIVER_LICENCE_ENDORSEMENTS(DRIVER_ID, ENDORSEMENT_TYPE_ID);");
        }

        private static void VehicleClass(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS VEHICLE_CLASS (")
              .AppendLine("ID INTEGER PRIMARY KEY AUTOINCREMENT,")
              .AppendLine("NAME TEXT NOT NULL UNIQUE,")
              .AppendLine("DESCRIPTION TEXT,")
              .AppendLine("MIN_GROSS_WEIGHT_KG REAL NULL,")
              .AppendLine("MAX_GROSS_WEIGHT_KG REAL NULL")
              .AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());

            string sql = "INSERT OR IGNORE INTO VEHICLE_CLASS (ID, NAME, DESCRIPTION, MIN_GROSS_WEIGHT_KG, MAX_GROSS_WEIGHT_KG) VALUES ";

            connection.ExecuteNonQuery($"{sql}(1,'Class 1 - Light','Car / light vehicles',0,6000)");
            connection.ExecuteNonQuery($"{sql}(2,'Class 2 - Medium Rigid','Rigid GLW 6,001–18,000 kg',6001,18000)");
            connection.ExecuteNonQuery($"{sql}(3,'Class 3 - Medium Combination','GCW 12,001–25,000 kg',12001,25000)");
            connection.ExecuteNonQuery($"{sql}(4,'Class 4 - Heavy Rigid','Rigid >18,000 kg',18001,NULL)");
            connection.ExecuteNonQuery($"{sql}(5,'Class 5 - Heavy Combination','GCW >25,000 kg',25001,NULL)");
            connection.ExecuteNonQuery($"{sql}(6,'Class 6 - Motorcycle','Motorcycles incl. learner/restricted',0,1000)");
        }
        private static void VehicleType(Helper.Connection connection)
        {
            StringBuilder sb = new();
            double today = DateTime.Today.ToOADate();

            sb.AppendLine("CREATE TABLE IF NOT EXISTS VEHICLE_TYPE (")
              .AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,")
              .AppendLine("  CLASS_ID INTEGER NOT NULL REFERENCES VEHICLE_CLASS(ID),")
              .AppendLine("  PASSENGERS INTEGER NOT NULL,")
              .AppendLine("  CARGO_KG INTEGER NOT NULL,")
              .AppendLine("  DESCRIPTION TEXT,")
              .AppendLine("  CREATEDOADATE REAL NOT NULL,")
              .AppendLine("  UPDATEDOADATE REAL NOT NULL")
              .AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());

            // Optional index for faster lookup by class
            connection.ExecuteNonQuery(
                "CREATE INDEX IF NOT EXISTS IDX_VEHICLE_TYPE_CLASS_ID ON VEHICLE_TYPE(CLASS_ID);");

            string sqlLine = "INSERT OR IGNORE INTO VEHICLE_TYPE (CLASS_ID, PASSENGERS, CARGO_KG, DESCRIPTION, CREATEDOADATE, UPDATEDOADATE) ";

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (6, 0, 5, 'Light 2-wheeler for parcels', {today}, {today});"); // Motorbike
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (1, 0, 50, 'Compact car, small cargo', {today}, {today});"); // Small Car
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (1, 4, 50, 'SUV, 4 passengers, small cargo', {today}, {today});"); // SUV
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (1, 3, 50, 'Hatchback, 3 passengers, small cargo', {today}, {today});"); // Hatchback
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (1, 4, 50, 'Station Wagon, 4 passengers, small cargo', {today}, {today});"); // Station Wagon
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (1, 0, 20, 'Coupe, small cargo', {today}, {today});"); // Coupe
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (1, 4, 50, 'Sedan, 4 passengers, small cargo', {today}, {today});"); // Sedan
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (1, 0, 1000, 'Mini Van, parcels', {today}, {today});"); // Mini Van
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (1, 4, 50, 'Mini Van, 4 passengers, parcels', {today}, {today});"); // Mini Van
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (1, 5, 50, 'Mini Van, 5 passengers, parcels', {today}, {today});"); // Mini Van
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (1, 7, 50, 'Mini Van, 7 passengers, parcels', {today}, {today});"); // Mini Van
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (1, 0, 1000, 'Minibus, parcels', {today}, {today});"); // Minibus
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (1, 12, 250, 'Minibus, 12 passengers', {today}, {today});"); // Minibus
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (2, 0, 1000, 'Large Van', {today}, {today});"); // Large Van
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (2, 12, 250, 'Passenger Van, 12+ passengers', {today}, {today});"); // Large Van
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (1, 0, 1000, 'Pickup truck, 1000kg cargo', {today}, {today});"); // Pickup
            connection.ExecuteNonQuery(sb.ToString());

            sb.Clear(); sb.AppendLine(sqlLine + $"VALUES (2, 0, 2500, 'Small Truck, heavy cargo', {today}, {today});"); // Small Truck
            connection.ExecuteNonQuery(sb.ToString());
        }

        private static void DriverLicenceEndorsementType(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS DRIVER_LICENCE_ENDORSEMENT_TYPE (")
              .AppendLine("ID INTEGER PRIMARY KEY AUTOINCREMENT,")
              .AppendLine("CODE TEXT NOT NULL UNIQUE,")
              .AppendLine("NAME TEXT NOT NULL,")
              .AppendLine("DESCRIPTION TEXT NULL,")
              .AppendLine("SORTORDER INTEGER NOT NULL DEFAULT 0,")
              .AppendLine("CREATED_AT REAL NOT NULL,")
              .AppendLine("UPDATED_AT REAL NOT NULL")
              .AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());

            connection.ExecuteNonQuery(
                "CREATE INDEX IF NOT EXISTS IDX_DRIVER_LICENCE_ENDORSEMENT_CODE ON DRIVER_LICENCE_ENDORSEMENT_TYPE(CODE);");

            double now = DateTime.Today.ToOADate();
            string insert = "INSERT OR IGNORE INTO DRIVER_LICENCE_ENDORSEMENT_TYPE (CODE, NAME, DESCRIPTION, SORTORDER, CREATED_AT, UPDATED_AT) VALUES";

            connection.ExecuteNonQuery($"{insert}('D','Dangerous Goods','Allow transport of dangerous goods.',10,{now},{now})");
            connection.ExecuteNonQuery($"{insert}('F','Forklift','Operate forklifts on road as special-type vehicles.',20,{now},{now})");
            connection.ExecuteNonQuery($"{insert}('I','Driving Instructor','Instructor authorisation for driver training.',30,{now},{now})");
            connection.ExecuteNonQuery($"{insert}('O','Testing Officer','Permitted to conduct driving tests.',40,{now},{now})");
            connection.ExecuteNonQuery($"{insert}('P','Passenger Service','Drive passenger service vehicles (bus, taxi etc.).',50,{now},{now})");
            connection.ExecuteNonQuery($"{insert}('R','Rollers','Special-type vehicles that run on rollers.',60,{now},{now})");
            connection.ExecuteNonQuery($"{insert}('T','Tracks','Special-type vehicles on self-laying tracks.',70,{now},{now})");
            connection.ExecuteNonQuery($"{insert}('W','Wheels (special-type)','Vehicles running on wheels (not forklifts).',80,{now},{now})");
            connection.ExecuteNonQuery($"{insert}('V','Vehicle Recovery','Tow trucks / recovery vehicles.',90,{now},{now})");
        }
        private static void DriverDocuments(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS DRIVER_DOCUMENTS (")
              .AppendLine("ID INTEGER PRIMARY KEY AUTOINCREMENT,")
              .AppendLine("DRIVER_ID INTEGER NOT NULL,")
              .AppendLine("DOC_TYPE TEXT NOT NULL,")
              .AppendLine("FILE_PATH TEXT NOT NULL,")
              .AppendLine("EXPIRY_DATE REAL NULL,")
              .AppendLine("CREATED_AT REAL NOT NULL,")
              .AppendLine("UPDATED_AT REAL NOT NULL,")
              .AppendLine("FOREIGN KEY(DRIVER_ID) REFERENCES DRIVER(ID) ON DELETE CASCADE,")
              .AppendLine("CHECK (EXPIRY_DATE IS NULL OR EXPIRY_DATE >= CREATED_AT)")
              .AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());

            connection.ExecuteNonQuery(
                "CREATE INDEX IF NOT EXISTS IDX_DRIVER_DOCUMENTS_DRIVER_ID ON DRIVER_DOCUMENTS(DRIVER_ID);");
        }
        private static void DriverVehicles(Helper.Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS DRIVER_VEHICLES (")
              .AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,")
              .AppendLine("  DRIVER_ID INTEGER NOT NULL,")
              .AppendLine("  MAKE TEXT NOT NULL,")
              .AppendLine("  MODEL TEXT NOT NULL,")
              .AppendLine("  YEAR INTEGER NULL,")
              .AppendLine("  REGISTRATION TEXT NOT NULL UNIQUE,")
              .AppendLine("  WOF_EXPIRY REAL NULL,")
              .AppendLine("  INSURANCE_EXPIRY REAL NULL,")
              .AppendLine("  CAPACITY REAL NULL,")
              .AppendLine("  VEHICLE_CLASS_ID INTEGER NULL,")
              .AppendLine("  VEHICLE_TYPE_ID INTEGER NULL,")
              .AppendLine("  CREATED_AT REAL NOT NULL,")
              .AppendLine("  UPDATED_AT REAL NOT NULL,")
              .AppendLine("  FOREIGN KEY(DRIVER_ID) REFERENCES DRIVER(ID) ON DELETE CASCADE,")
              .AppendLine("  FOREIGN KEY(VEHICLE_CLASS_ID) REFERENCES VEHICLE_CLASS(ID),")
              .AppendLine("  FOREIGN KEY(VEHICLE_TYPE_ID) REFERENCES VEHICLE_TYPE(ID),")
              .AppendLine("  CHECK(WOF_EXPIRY IS NULL OR WOF_EXPIRY >= CREATED_AT),")
              .AppendLine("  CHECK(INSURANCE_EXPIRY IS NULL OR INSURANCE_EXPIRY >= CREATED_AT)")
              .AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());

            // Create indexes for faster lookups
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_DRIVER_VEHICLES_DRIVER_ID ON DRIVER_VEHICLES(DRIVER_ID);");
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_DRIVER_VEHICLES_VEHICLE_CLASS_ID ON DRIVER_VEHICLES(VEHICLE_CLASS_ID);");
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_DRIVER_VEHICLES_VEHICLE_TYPE_ID ON DRIVER_VEHICLES(VEHICLE_TYPE_ID);");
        }
    }
}
