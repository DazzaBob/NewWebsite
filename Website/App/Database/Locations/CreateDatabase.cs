using System.Text;
using Website.App.Helper;

namespace Website.App.Database.Locations
{
    internal static class CreateDatabase
    {
        internal static void Begin()
        {
            using Helper.Connection connection = Database.Shared.Connection(Database.Schema.Locations.Database);
            Country(connection);
            Region(connection);
            Place(connection);
            Locality(connection);
            AddressType(connection);
            Address(connection);
            Zones(connection);
        }
        private static void Country(Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS COUNTRY (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  NAME TEXT NOT NULL UNIQUE,");
            sb.AppendLine("  CREATEDOADATE REAL NOT NULL,");
            sb.AppendLine("  UPDATEDOADATE REAL NOT NULL");
            sb.AppendLine(");");

            connection.ExecuteNonQuery(sb.ToString());

            string today = DateTime.Today.ToOADate().ToString();
            connection.ExecuteNonQuery($"INSERT OR IGNORE INTO COUNTRY(NAME, CREATEDOADATE, UPDATEDOADATE) VALUES('New Zealand', {today}, {today} )");
        }
        private static void Region(Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS REGION (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  COUNTRY_ID INTEGER NOT NULL,");
            sb.AppendLine("  NAME TEXT NOT NULL,");
            sb.AppendLine("  SHORTCODE TEXT DEFAULT '',");
            sb.AppendLine("  CREATEDOADATE REAL NOT NULL,");
            sb.AppendLine("  UPDATEDOADATE REAL NOT NULL,");
            sb.AppendLine("  FOREIGN KEY(COUNTRY_ID) REFERENCES COUNTRY(ID) ON DELETE RESTRICT ON UPDATE CASCADE,");
            sb.AppendLine("  UNIQUE(COUNTRY_ID, NAME)");
            sb.AppendLine(");");

            connection.ExecuteNonQuery(sb.ToString());
        }
        private static void Place(Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS PLACE (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  REGION_ID INTEGER NOT NULL,");
            sb.AppendLine("  NAME TEXT NOT NULL,");
            sb.AppendLine("  LATITUDE REAL,");
            sb.AppendLine("  LONGITUDE REAL,");
            sb.AppendLine("  MIN_LATITUDE REAL,");
            sb.AppendLine("  MAX_LATITUDE REAL,");
            sb.AppendLine("  MIN_LONGITUDE REAL,");
            sb.AppendLine("  MAX_LONGITUDE REAL");
            sb.AppendLine(");");

            connection.ExecuteNonQuery(sb.ToString());
        }
        private static void Locality(Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS LOCALITY (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  PLACE_ID INTEGER NOT NULL,");
            sb.AppendLine("  NAME TEXT NOT NULL,");
            sb.AppendLine("  LATITUDE REAL,");
            sb.AppendLine("  LONGITUDE REAL,");
            sb.AppendLine("  MIN_LATITUDE REAL,");
            sb.AppendLine("  MAX_LATITUDE REAL,");
            sb.AppendLine("  MIN_LONGITUDE REAL,");
            sb.AppendLine("  MAX_LONGITUDE REAL,");
            sb.AppendLine("  FOREIGN KEY(PLACE_ID) REFERENCES PLACE(ID) ON DELETE CASCADE");
            sb.AppendLine(");");

            connection.ExecuteNonQuery(sb.ToString());

            // Optional index for faster lookups by place
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_LOCALITY_PLACE ON LOCALITY(PLACE_ID);");
        }
        private static void AddressType(Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS ADDRESS_TYPE (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  NAME TEXT NOT NULL UNIQUE");
            sb.AppendLine(");");
            connection.ExecuteNonQuery(sb.ToString());

            // Seed the address types
            string[] types = ["Residential", "Commercial", "School", "Airport", "Industrial", "Government", "Other"];
            for (int i = 0; i < types.Length; i++)
            {
                connection.ExecuteNonQuery($"INSERT OR IGNORE INTO ADDRESS_TYPE (ID, NAME) VALUES ({i + 1}, {Database.Shared.SafeReplace(types[i])});");
            }
        }
        private static void Address(Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS ADDRESS (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  STREET_NUMBER TEXT,");
            sb.AppendLine("  STREET_NAME TEXT,");
            sb.AppendLine("  POSTCODE TEXT,");
            sb.AppendLine("  COUNTRY_ID INTEGER NOT NULL,");
            sb.AppendLine("  REGION_ID INTEGER NOT NULL,");
            sb.AppendLine("  PLACE_ID INTEGER NULL,");
            sb.AppendLine("  LOCALITY_ID INTEGER NULL,");
            sb.AppendLine("  ADDRESS_TYPE_ID INTEGER NOT NULL,");
            sb.AppendLine("  LONGITUDE REAL,");
            sb.AppendLine("  LATITUDE REAL,");
            sb.AppendLine("  ROUTABLE_LONGITUDE REAL,");
            sb.AppendLine("  ROUTABLE_LATITUDE REAL,");
            sb.AppendLine("  LASTUSEDOADATE REAL,");
            sb.AppendLine("  ZONE_ID INTEGER NULL,");
            sb.AppendLine("  FOREIGN KEY(COUNTRY_ID) REFERENCES COUNTRY(ID) ON DELETE NO ACTION,");
            sb.AppendLine("  FOREIGN KEY(REGION_ID) REFERENCES REGION(ID) ON DELETE NO ACTION,");
            sb.AppendLine("  FOREIGN KEY(PLACE_ID) REFERENCES PLACE(ID) ON DELETE NO ACTION,");
            sb.AppendLine("  FOREIGN KEY(LOCALITY_ID) REFERENCES LOCALITY(ID) ON DELETE SET NULL,");
            sb.AppendLine("  FOREIGN KEY(ADDRESS_TYPE_ID) REFERENCES ADDRESS_TYPE(ID) ON DELETE NO ACTION");
            sb.AppendLine(");");

            connection.ExecuteNonQuery(sb.ToString());

            // Optional indexes for faster zoning queries
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_ADDRESS_PLACE_LOCALITY ON ADDRESS(PLACE_ID, LOCALITY_ID);");
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_ADDRESS_COUNTRY_REGION_TYPE ON ADDRESS(COUNTRY_ID, REGION_ID, ADDRESS_TYPE_ID);");
        }
        private static void Zones(Connection connection)
        {
            StringBuilder sb = new();
            sb.AppendLine("CREATE TABLE IF NOT EXISTS ZONES (");
            sb.AppendLine("  ID INTEGER PRIMARY KEY AUTOINCREMENT,");
            sb.AppendLine("  NAME TEXT NOT NULL,");
            sb.AppendLine("  PLACE_ID INTEGER NOT NULL,");
            sb.AppendLine("  LOCALITY_ID INTEGER,");
            sb.AppendLine("  CENTEROID_LATITUDE REAL NOT NULL,");
            sb.AppendLine("  CENTEROID_LONGITUDE REAL NOT NULL,");
            sb.AppendLine("  MIN_LATITUDE REAL NOT NULL,");
            sb.AppendLine("  MAX_LATITUDE REAL NOT NULL,");
            sb.AppendLine("  MIN_LONGITUDE REAL NOT NULL,");
            sb.AppendLine("  MAX_LONGITUDE REAL NOT NULL,");
            sb.AppendLine("  CREATEDOADATE REAL NOT NULL,");
            sb.AppendLine("  UPDATEDOADATE REAL NOT NULL,");
            sb.AppendLine("  SHAPE_JSON TEXT NOT NULL,");
            sb.AppendLine("  IS_RURAL INTEGER NOT NULL,");
            sb.AppendLine("  AUTO_GROW INTEGER NOT NULL,");
            sb.AppendLine("  IS_PIONEER_ZONE INTEGER NOT NULL,");
            sb.AppendLine("  IS_RURAL_CONFIRMED INTEGER NOT NULL,");
            sb.AppendLine("  FOREIGN KEY(PLACE_ID) REFERENCES PLACE(ID) ON DELETE CASCADE,");
            sb.AppendLine("  FOREIGN KEY(LOCALITY_ID) REFERENCES LOCALITY(ID) ON DELETE CASCADE");
            sb.AppendLine(");");

            connection.ExecuteNonQuery(sb.ToString());

            // Optional index for faster lookups by place/locality
            connection.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IDX_ZONES_PLACE_LOCALITY ON ZONES(PLACE_ID, LOCALITY_ID);");
        }
    }
}