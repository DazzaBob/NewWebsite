using Microsoft.Data.Sqlite;
using System.Data;
using System.Globalization;

namespace Website.App.Helper
{
    public class Connection : IDisposable
    {
        private readonly SqliteConnection _connection;
        private bool _disposed;
        public Connection(string connectionString)
        {
            _connection = new SqliteConnection(connectionString);
            _connection.Open();
            EnableForeignKeys();
        }
        private void EnableForeignKeys()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
        }
        internal void EnsureOpen()
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }
        private static T ExecuteWithRetry<T>(Func<T> action, int maxRetries = 5, int initialDelayMs = 50)
        {
            int delay = initialDelayMs;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try { return action(); }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 5) // SQLITE_BUSY
                {
                    System.Threading.Thread.Sleep(delay);
                    delay *= 2;
                }
            }
            return action(); // last attempt
        }

        public DataTable GetDataTable(string commandText, params SqliteParameter[] parameters)
        {
            DataTable result = new() { Locale = CultureInfo.InvariantCulture };
            EnsureOpen();

            ExecuteWithRetry(() =>
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = commandText;
                cmd.Parameters.AddRange(parameters);
                using var reader = cmd.ExecuteReader();
                result.Load(reader);
                return true;
            });

            return result;
        }
        public DataTable GetDataTable(string commandText)
        {
            return GetDataTable(commandText, []);
        }

        public object ExecuteNonQuery(string commandText, params SqliteParameter[] parameters)
        {
            EnsureOpen();
            return ExecuteWithRetry(() =>
            {
                using var transaction = _connection.BeginTransaction();
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = commandText;
                cmd.Parameters.AddRange(parameters);
                cmd.Transaction = transaction;
                int rows = cmd.ExecuteNonQuery();
                transaction.Commit();
                return rows;
            });
        }
        public object ExecuteNonQuery(string commandText)
        {
            return ExecuteNonQuery(commandText, []);
        }

        public object ExecuteScalar(string commandText, params SqliteParameter[] parameters)
        {
            EnsureOpen();
            return ExecuteWithRetry(() =>
            {
                using var transaction = _connection.BeginTransaction();
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = commandText;
                cmd.Parameters.AddRange(parameters);
                cmd.Transaction = transaction;
                var result = cmd.ExecuteScalar();
                transaction.Commit();
                return result ?? DBNull.Value;
            });
        }
        public object ExecuteScalar(string commandText)
        {
            return ExecuteScalar(commandText, []);
        }

        public static SqliteParameter CreateParameter(string name, object value)
        {
            return new SqliteParameter(name, value ?? DBNull.Value);
        }

        public static string GetLastInsertId()
        {
            return "SELECT last_insert_rowid();";
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // dispose managed resources
                    _connection.Close();
                    //_connection.Dispose(); // optional if needed
                }
                // free unmanaged resources here if any
                _disposed = true;
            }
        }
    }
}
