// -----------------------------------------------------------------------------
//  ZoneManager – full zoning engine with boot-sweep, pioneer logic, auto-grow
// -----------------------------------------------------------------------------
using System.Data;
using System.Diagnostics;

namespace Website.App.Operations.Zoning
{
    internal sealed partial class ZoneManager
    {
        private static volatile bool _isRunning = false;
        private static readonly object _lock = new();
        private static readonly string NamespaceClass = "App.Operations.Zoning.ZoneManager.";
        private static string errorMessage = string.Empty;
        private const int BatchSize = 100; // Number of updates per batch

        private static readonly List<string> _pendingUpdates = [];
        private static readonly object _batchLock = new();

        private ZoneManager() { } // private constructor

        internal static void StartZoning(bool IsBackground = true)
        {
            if (IsBackground)
            {
                lock (_lock)
                {
                    if (_isRunning) return;
                    _isRunning = true;

                    new Thread(() =>
                    {
                        try { ExecutePendingZoningBatch(); }
                        finally { lock (_lock) { _isRunning = false; } }
                    })
                    { IsBackground = true }.Start();
                }
            }
            else
            {
                try
                {
                    ExecutePendingZoningBatch();
                }
                catch (Exception ex)
                {
                    errorMessage = NamespaceClass + $"StartZoning: {ex.Message}";
                    Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Error);
                }
            }
        }

        private static void ExecutePendingZoningBatch()
        {
            using Helper.Connection conn = Database.Shared.Connection(Database.Schema.Locations.Database);

            DataTable addressDT = Database.Shared.GetDataTable(conn, "ADDRESS", "ZONE_ID IS NULL");
            if (addressDT.Rows.Count == 0)
            {
                errorMessage = NamespaceClass + $"ExecutePendingZoningBatch: Sweep finished – no addresses pending.";
                Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Info);
                return;
            }

            string placeIds = string.Join(",", addressDT.AsEnumerable().Select(r => r["PLACE_ID"].ToString()).Distinct());
            DataTable zoneDT = Database.Shared.GetDataTable(conn, "ZONES", $"PLACE_ID IN ({placeIds})", "CREATEDOADATE DESC");

            if (zoneDT.Rows.Count == 0)
            {
                errorMessage = NamespaceClass + $"ExecutePendingZoningBatch: No zones found for pending addresses.";
                Website.App.Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Warn);
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();

            foreach (DataRow addr in addressDT.Rows)
            {
                int placeId = (int)addr["PLACE_ID"];
                object localityObj = addr["LOCALITY_ID"];
                DataRow[] matchedZones = localityObj != DBNull.Value
                    ? zoneDT.Select($"PLACE_ID = {placeId} AND LOCALITY_ID = {localityObj}")
                    : zoneDT.Select($"PLACE_ID = {placeId} AND LOCALITY_ID IS NULL");

                if (matchedZones.Length > 0)
                {
                    int zoneId = (int)matchedZones[0]["ID"];
                    QueueUpdate(addrId: (int)addr["ID"], zoneId: zoneId, conn: conn);
                }
            }

            // Execute remaining batch
            FlushPendingUpdates(conn);

            sw.Stop();
            errorMessage = NamespaceClass + $"ExecutePendingZoningBatch: Sweep finished – {addressDT.Rows.Count} addresses, {sw.ElapsedMilliseconds} ms";
            Website.App.Bootstrap.Logger?.Add(errorMessage, Helper.Logger.LogLevel.Info);
        }

        private static void QueueUpdate(int addrId, int zoneId, Helper.Connection conn)
        {
            lock (_batchLock)
            {
                _pendingUpdates.Add($"UPDATE ADDRESS SET ZONE_ID = {zoneId} WHERE ID = {addrId};");

                if (_pendingUpdates.Count >= BatchSize)
                {
                    FlushPendingUpdates(conn);
                }
            }
        }

        private static void FlushPendingUpdates(Website.App.Helper.Connection conn)
        {
            if (_pendingUpdates.Count == 0) return;

            try
            {
                foreach (string sql in _pendingUpdates)
                {
                    _ = conn.ExecuteNonQuery(sql);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                _pendingUpdates.Clear();
            }
        }
    }
}