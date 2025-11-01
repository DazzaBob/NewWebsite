using Npgsql;
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

        #region Public API
        public static object GetScalar(string schema, string tableName, string field, string? whereClause = null, string? orderBy = null)
        {
            Database dbPool = GetConnection(schema);
            DALConnection conn = dbPool.Acquire();
            try
            {
                StringBuilder sql = new($"SELECT {field} FROM {tableName}");
                if (!string.IsNullOrWhiteSpace(whereClause))
                    sql.Append(" WHERE ").Append(whereClause);
                if (!string.IsNullOrWhiteSpace(orderBy))
                    sql.Append(" ORDER BY ").Append(orderBy);

                sql.Append(" LIMIT 1;"); // only need one value
                return conn.ExecuteScalar(sql.ToString());
            }
            finally
            {
                dbPool.Release(conn);
            }
        }

        /// <summary>
        /// Inserts a new row into the specified table within the given schema using the provided fields and values.
        /// Acquires a pooled database connection, executes the INSERT statement, retrieves the generated ID, 
        /// and then releases the connection back to the pool.
        /// </summary>
        public static int Insert(string Schema, string tableName, string fields, string values)
        {
            Database dbPool = GetConnection(Schema);
            DALConnection conn = dbPool.Acquire();
            try
            {
                string sql = $"INSERT INTO {tableName} ({fields}) VALUES ({values}) RETURNING id;";
                object result = conn.ExecuteScalar(sql);
                return Convert.ToInt32(result);
            }
            finally
            {
                dbPool.Release(conn);
            }
        }
        public static int Insert(string schema, string tableName, string fields, NpgsqlParameter[] parameters)
        {
            Database dbPool = GetConnection(schema);
            DALConnection conn = dbPool.Acquire();
            try
            {
                string sql = $"INSERT INTO {tableName} ({fields}) VALUES ({string.Join(",", parameters.Select(p => p.ParameterName))}) RETURNING id;";
                object result = conn.ExecuteScalar(sql, parameters);
                return Convert.ToInt32(result);
            }
            finally
            {
                dbPool.Release(conn);
            }
        }

        /// <summary>
        /// Updates rows in the specified table within the given schema using the provided SET clause and optional WHERE clause.
        /// Acquires a pooled database connection, executes the UPDATE statement, and returns the number of affected rows.
        /// The connection is released back to the pool after execution.
        /// </summary>
        public static object Update(string Schema, string tableName, string setClause, string? whereClause = null)
        {
            Database dbPool = GetConnection(Schema);
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

        public static DataTable GetDataTable(string Schema, string tableName, string where = "", string orderBy = "")
        {
            Database dbPool = GetConnection(Schema);
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
        public static DataTable GetDataTable(string Schema, string sql, params NpgsqlParameter[] parameters)
        {
            Database dbPool = GetConnection(Schema);
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
        public static object ExecuteScalar(string Schema, string sql, params NpgsqlParameter[] parameters)
        {
            Database dbPool = GetConnection(Schema);
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
        public static long ExecuteNonQuery(string Schema, string sql, params NpgsqlParameter[] parameters)
        {
            Database dbPool = GetConnection(Schema);
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

        public static async Task<object> GetScalarAsync(string schema, string tableName, string field, string? whereClause = null, string? orderBy = null)
        {
            return await Task.Run(() => GetScalar(schema, tableName, field, whereClause, orderBy));
        }
        public static async Task<int> InsertAsync(string Schema, string tableName, string fields, string values)
        {
            return await Task.Run(() => Insert(Schema, tableName, fields, values));
        }
        public static async Task<object> UpdateAsync(string Schema, string tableName, string setClause, string? whereClause = null)
        {
            return await Task.Run(() => Update(Schema, tableName, setClause, whereClause));
        }
        public static async Task<DataTable> GetDataTableAsync(string Schema, string tableName, string where = "", string orderBy = "")
        {
            return await Task.Run(() => GetDataTable(Schema, tableName, where, orderBy));
        }
        public static async Task<DataTable> GetDataTableAsync(string Schema, string sql, params NpgsqlParameter[] parameters)
        {
            return await Task.Run(() => GetDataTable(Schema, sql, parameters));
        }
        public static async Task<object> ExecuteScalarAsync(string Schema, string sql, params NpgsqlParameter[] parameters)
        {
            return await Task.Run(() => ExecuteScalar(Schema, sql, parameters));
        }
        public static async Task<long> ExecuteNonQueryAsync(string Schema, string sql, params NpgsqlParameter[] parameters)
        {
            return await Task.Run(() => ExecuteNonQuery(Schema, sql, parameters));
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
        private static Database GetConnection(string Schema)
        {
            return _databases.GetOrAdd(Schema, name => new Database(name, DefaultMaxConnections));
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
                            DALConnection newConn = new() { LastUsed = DateTime.UtcNow.ToOADate() };
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
                    DALConnection newConn = new() { LastUsed = DateTime.UtcNow.ToOADate() };
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
            private readonly NpgsqlConnection _connection;
            private bool _disposed;
            private readonly string basePath;
            internal double LastUsed { get; set; }
            internal DALConnection() // c# sux, with how they name the constructor
            {
                _connection = null!; // to satisfy compiler
                basePath = Settings.DatabasePath; // from appsettings.json
                LastUsed = DateTime.UtcNow.ToOADate();
                _connection = new NpgsqlConnection(App.Settings.DefaultConnectionString);
                _connection.Open();
            }
            private static T ExecuteWithRetry<T>(Func<T> action, int maxRetries = 5, int initialDelayMs = 50)
            {
                int delay = initialDelayMs;
                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    try { return action(); }
                    catch (NpgsqlException ex) when (ex is Npgsql.NpgsqlException && ex.IsTransient) // A Fallback, just incase the server is reall busy for some reason.
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
            internal DataTable GetDataTable(string commandText, params NpgsqlParameter[] parameters)
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
            internal object ExecuteNonQuery(string commandText, params NpgsqlParameter[] parameters)
            {
                EnsureOpen();
                return ExecuteWithRetry(() =>
                {
                    using var cmd = _connection.CreateCommand();
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddRange(parameters);
                    int rows = cmd.ExecuteNonQuery();
                    return rows;
                });
            }
            internal object ExecuteNonQuery(string commandText)
            {
                return ExecuteNonQuery(commandText, []);
            }
            internal object ExecuteScalar(string commandText, params NpgsqlParameter[] parameters)
            {
                EnsureOpen();
                return ExecuteWithRetry(() =>
                {
                    using var cmd = _connection.CreateCommand();
                    cmd.CommandText = commandText;
                    cmd.Parameters.AddRange(parameters);
                    var result = cmd.ExecuteScalar();
                    return result ?? DBNull.Value;
                });
            }
            internal object ExecuteScalar(string commandText)
            {
                return ExecuteScalar(commandText, []);
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
