using System.Text;

namespace Website.App.Database.Entities
{
    internal static class CreateDatabase
    {

        internal static void Begin()
        {
            using Helper.Connection connection = Database.Shared.Connection(Schema.Entities.Database);
            User(connection);
            UserAddress(connection);

            Roles(connection);
            UserRole(connection);
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
    }
}
