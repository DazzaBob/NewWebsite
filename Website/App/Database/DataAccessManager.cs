using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Text;
using Website.App.Helper;

namespace Website.App.Database
{
    public static class DataAccessManager
    {
        private static readonly ConcurrentDictionary<string, Database> _databases = new(StringComparer.OrdinalIgnoreCase);
        private const int DefaultMaxConnections = 25;
        private const int DefaultWaitTimeoutMs = 5000; // wait max 5s for a connection

        // Optional logging delegate
        public static Action<string>? Log { get; set; }

        #region Public Async API
        public static int Insert(string dbName, string tableName, string fields, string values)
        {
            Database dbPool = GetConnection(dbName);
            DALConnection conn = dbPool.Acquire();
            try
            {
                string sql = $"INSERT INTO {tableName} ({fields}) VALUES ({values}); SELECT last_insert_rowid();";
                object result = conn.ExecuteScalar(sql);
                return Convert.ToInt32(result);
            }
            finally
            {
                dbPool.Release(conn);
            }
        }
        public static object Update(string dbName, string tableName, string setClause, string? whereClause = null)
        {
            Database dbPool = GetConnection(dbName);
            DALConnection conn = dbPool.Acquire();
            try
            {
                StringBuilder sql = new($"UPDATE {tableName} SET {setClause}");
                if (!string.IsNullOrWhiteSpace(whereClause))
                    sql.Append(" WHERE ").Append(whereClause);

                return conn.ExecuteNonQuery(sql.ToString());
            }
            finally
            {
                dbPool.Release(conn);
            }
        }
        public static DataTable GetDataTable(string dbName, string tableName, string where = "", string orderBy = "")
        {
            Database dbPool = GetConnection(dbName);
            DALConnection conn = dbPool.Acquire();
            try
            {
                StringBuilder sql = new($"SELECT * FROM {tableName}");
                if (!string.IsNullOrWhiteSpace(where))
                    sql.Append(" WHERE ").Append(where);
                if (!string.IsNullOrWhiteSpace(orderBy))
                    sql.Append(" ORDER BY ").Append(orderBy);

                return conn.GetDataTable(sql.ToString());
            }
            finally
            {
                dbPool.Release(conn);
            }
        }
        public static DataTable GetDataTable(string dbName, string sql, params SqliteParameter[] parameters)
        {
            Database dbPool = GetConnection(dbName);
            DALConnection conn = dbPool.Acquire();
            try
            {
                return conn.GetDataTable(sql, parameters);
            }
            finally
            {
                dbPool.Release(conn);
            }
        }
        public static object ExecuteScalar(string dbName, string sql, params SqliteParameter[] parameters)
        {
            Database dbPool = GetConnection(dbName);
            DALConnection conn = dbPool.Acquire();
            try
            {
                return conn.ExecuteScalar(sql, parameters);
            }
            finally
            {
                dbPool.Release(conn);
            }
        }
        public static long ExecuteNonQuery(string dbName, string sql, params SqliteParameter[] parameters)
        {
            Database dbPool = GetConnection(dbName);
            DALConnection conn = dbPool.Acquire();
            try
            {
                return Convert.ToInt64(conn.ExecuteNonQuery(sql, parameters));
            }
            finally
            {
                dbPool.Release(conn);
            }
        }

        public static async Task<int> InsertAsync(string dbName, string tableName, string fields, string values)
        {
            return await Task.Run(() => Insert(dbName, tableName, fields, values));
        }
        public static async Task<object> UpdateAsync(string dbName, string tableName, string setClause, string? whereClause = null)
        {
            return await Task.Run(() => Update(dbName, tableName, setClause, whereClause));
        }
        public static async Task<DataTable> GetDataTableAsync(string dbName, string tableName, string where = "", string orderBy = "")
        {
            return await Task.Run(() => GetDataTable(dbName, tableName, where, orderBy));
        }
        public static async Task<DataTable> GetDataTableAsync(string dbName, string sql, params SqliteParameter[] parameters)
        {
            return await Task.Run(() => GetDataTable(dbName, sql, parameters));
        }
        public static async Task<object> ExecuteScalarAsync(string dbName, string sql, params SqliteParameter[] parameters)
        {
            return await Task.Run(() => ExecuteScalar(dbName, sql, parameters));
        }
        public static async Task<long> ExecuteNonQueryAsync(string dbName, string sql, params SqliteParameter[] parameters)
        {
            return await Task.Run(() => ExecuteNonQuery(dbName, sql, parameters));
        }
        #endregion
        public static void ShutdownPools()
        {
            foreach (var kvp in _databases)
            {
                try
                {
                    kvp.Value.Dispose(); // disposes all DALConnections in that pool
                }
                catch (Exception ex)
                {
                    Bootstrap.Logger?.Add($"Error disposing pool '{kvp.Key}': {ex}", Logger.LogLevel.Error);
                }
            }
            _databases.Clear();
        }
        private static Database GetConnection(string dbName)
        {
            return _databases.GetOrAdd(dbName, name => new Database(name, DefaultMaxConnections));
        }
        private class Database : IDisposable
        {
            private bool _disposed;
            internal string Name { get; init; }
            internal int MaxConnections { get; init; }
            internal Queue<DALConnection> AvailableConnections { get; } = new();
            internal int ActiveConnections { get; private set; }
            internal NameValueCollection Metadata { get; } = [];

            private readonly object _syncLock = new();
            private readonly SemaphoreSlim _asyncSemaphore;
            private readonly double _idleTimeout = 0.000694444; // 1 minute OAdate() adjust as needed
            internal Database(string dbName, int maxConnections)
            {
                Name = dbName;
                MaxConnections = maxConnections;
                Metadata["Created"] = DateTime.UtcNow.ToString("o");
                _asyncSemaphore = new SemaphoreSlim(1, maxConnections);
            }
            #region Synchronous Acquire / Release
            internal DALConnection Acquire()
            {
                ShrinkIdleConnections();

                lock (_syncLock)
                {
                    while (true)
                    {
                        if (AvailableConnections.Count > 0)
                        {
                            DALConnection conn = AvailableConnections.Dequeue();
                            ActiveConnections++;
                            conn.LastUsed = DateTime.UtcNow.ToOADate();
                            return conn;
                        }

                        if (ActiveConnections < MaxConnections)
                        {
                            ActiveConnections++;
                            DALConnection newConn = new(Name) { LastUsed = DateTime.UtcNow.ToOADate() };
                            return newConn;
                        }

                        // Wait until a connection is released
                        Monitor.Wait(_syncLock);
                    }
                }
            }
            internal void Release(DALConnection connection)
            {
                lock (_syncLock)
                {
                    ActiveConnections--;
                    connection.LastUsed = DateTime.UtcNow.ToOADate();
                    AvailableConnections.Enqueue(connection);
                    Monitor.Pulse(_syncLock);
                }
            }
            #endregion
            #region Asynchronous Acquire / Release
            internal async Task<DALConnection> AcquireAsync()
            {
                await _asyncSemaphore.WaitAsync();
                ShrinkIdleConnections();

                lock (_syncLock)
                {
                    if (AvailableConnections.Count > 0)
                    {
                        DALConnection conn = AvailableConnections.Dequeue();
                        ActiveConnections++;
                        conn.LastUsed = DateTime.UtcNow.ToOADate();
                        return conn;
                    }

                    ActiveConnections++;
                    DALConnection newConn = new(Name) { LastUsed = DateTime.UtcNow.ToOADate() };
                    return newConn;
                }
            }
            internal void ReleaseAsync(DALConnection connection)
            {
                lock (_syncLock)
                {
                    ActiveConnections--;
                    connection.LastUsed = DateTime.UtcNow.ToOADate();
                    AvailableConnections.Enqueue(connection);
                }
                _asyncSemaphore.Release();
            }
            #endregion
            private void ShrinkIdleConnections()
            {
                lock (_syncLock)
                {
                    double now = DateTime.UtcNow.ToOADate();
                    var remaining = new Queue<DALConnection>();
                    while (AvailableConnections.Count > 0)
                    {
                        var conn = AvailableConnections.Dequeue();
                        if (((now - conn.LastUsed) > _idleTimeout) && (ActiveConnections > 1))
                        {
                            conn.Dispose();
                            ActiveConnections--;
                        }
                        else
                        {
                            remaining.Enqueue(conn);
                        }
                    }
                    while (remaining.Count > 0)
                        AvailableConnections.Enqueue(remaining.Dequeue());
                }
            }
            internal void DestroyPool()
            {
                lock (_syncLock)
                {
                    while (AvailableConnections.Count > 0)
                    {
                        var conn = AvailableConnections.Dequeue();
                        conn.Dispose();
                        ActiveConnections--;
                    }
                }
            }
            #region Implements IDisposable Pattern
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
                        DestroyPool();
                        _asyncSemaphore.Dispose();
                    }
                    // free unmanaged resources here if any
                    _disposed = true;
                }
            }
            #endregion
        }
        private class DALConnection : IDisposable
        {
            private readonly SqliteConnection _connection;
            private bool _disposed;
            private readonly string basePath;
            internal double LastUsed { get; set; }
            private DALConnection()
            {
                _connection = null!; // to satisfy compiler
                basePath = Settings.DatabasePath; // from appsettings.json
                LastUsed = DateTime.UtcNow.ToOADate();
            }
            internal DALConnection(string dbName) : this() // c# sux, with how they name the constructor
            {
                string fullPath = Path.Combine(basePath, dbName);
                string connectionString = $"Data Source={fullPath}";

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
            internal void EnsureOpen()
            {
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();
            }
            internal DataTable GetDataTable(string commandText, params SqliteParameter[] parameters)
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
            internal DataTable GetDataTable(string commandText)
            {
                return GetDataTable(commandText, []);
            }
            internal object ExecuteNonQuery(string commandText, params SqliteParameter[] parameters)
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
            internal object ExecuteNonQuery(string commandText)
            {
                return ExecuteNonQuery(commandText, []);
            }
            internal object ExecuteScalar(string commandText, params SqliteParameter[] parameters)
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
            internal object ExecuteScalar(string commandText)
            {
                return ExecuteScalar(commandText, []);
            }
            internal static SqliteParameter CreateParameter(string name, object value)
            {
                return new SqliteParameter(name, value ?? DBNull.Value);
            }
            internal static string GetLastInsertId()
            {
                return "SELECT last_insert_rowid();";
            }
            #region Implements IDisposable Pattern
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
            #endregion
        }
    }
}
