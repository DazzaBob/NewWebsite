using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Data;
using Website.App.Security;

namespace Website.App.Hubs
{
    [IgnoreAntiforgeryToken] //[Authorize] // requires cookie auth; PWA must be logged in
    public class DriverHub : Hub
    {
        public class DriverShiftSlotDTO
        {
            public double StartIso { get; set; }
            public double EndIso { get; set; }
        }
        public class DriverActiveJobDTO
        {
            public long Id { get; set; }
            public string Label { get; set; } = "";
            public string? SubLabel { get; set; }
        }
        private static readonly ConcurrentDictionary<string, (long DriverId, string DeviceKey)> _connections = new();
        public override Task OnConnectedAsync()
        {
            if (Context.User != null)
            {
                var driverId = Context.User.Id();
                // device key is not known yet; we’ll fill it in on RegisterDevice
                _connections[Context.ConnectionId] = (driverId, DeviceKey: "");
            }

            return base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _connections.TryRemove(Context.ConnectionId, out _);
            return base.OnDisconnectedAsync(exception);
        }
        public Task RegisterDevice(string deviceKey)
        {
            if (string.IsNullOrWhiteSpace(deviceKey)) return Task.CompletedTask;
            if (Context.User == null) return Task.CompletedTask;

            var userId = Context.User.Id();
            var nowOad = DateTime.UtcNow.ToOADate();
            var deviceKeySql = Database.Shared.Sanitize(deviceKey, true);

            var http = Context.GetHttpContext();
            string? userAgent = http?.Request.Headers.UserAgent.ToString();

            var userAgentSql = !string.IsNullOrEmpty(userAgent) ? Database.Shared.Sanitize(userAgent, true) : "null";

            // simple platform derivation – OK if blank
            string? platform = null;
            if (!string.IsNullOrEmpty(userAgent))
            {
                var ua = userAgent;
                if (ua.Contains("Android", StringComparison.OrdinalIgnoreCase)) platform = "Android";
                else if (ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || ua.Contains("iPad", StringComparison.OrdinalIgnoreCase)) platform = "iOS";
                else if (ua.Contains("Windows", StringComparison.OrdinalIgnoreCase)) platform = "Windows";
                else if (ua.Contains("Mac OS", StringComparison.OrdinalIgnoreCase) || ua.Contains("Macintosh", StringComparison.OrdinalIgnoreCase)) platform = "macOS";
                else if (ua.Contains("Linux", StringComparison.OrdinalIgnoreCase)) platform = "Linux";
                else platform = "Other";
            }

            var platformSql = !string.IsNullOrEmpty(platform) ? Database.Shared.Sanitize(platform, true) : "null";

            string fields = "driver_id, device_key, user_agent, platform, created_oad, last_seen_oad, last_hub_oad, is_active, revoked_oad";
            string values = $"{userId}, {deviceKeySql}, {userAgentSql}, {platformSql}, {nowOad}, {nowOad}, {nowOad}, true, null";

            // Deactivate all other devices for this driver
            _ = App.Database.DataAccessManager.Update(Database.Schema.Entities.Name, Database.Schema.Entities.Tables.DriverDevice, $"is_active = false, revoked_oad = {nowOad}", $"driver_id = {userId} AND device_key <> {deviceKeySql}");

            // Does this driver/device already exist?
            DataTable dt = App.Database.DataAccessManager.GetDataTable(Database.Schema.Entities.Name, Database.Schema.Entities.Tables.DriverDevice, $"driver_id = {userId} AND device_key = {deviceKeySql}");
            if (dt == null || dt.Rows.Count == 0)
            {
                _ = App.Database.DataAccessManager.Insert(Database.Schema.Entities.Name, Database.Schema.Entities.Tables.DriverDevice, fields, values);
            }
            else
            {
                _ = App.Database.DataAccessManager.Update(Database.Schema.Entities.Name, Database.Schema.Entities.Tables.DriverDevice, $"is_active = true, last_seen_oad = {nowOad}, last_hub_oad = {nowOad}, revoked_oad = null, user_agent = {userAgentSql}, platform = {platformSql}", $"driver_id = {userId} AND device_key = {deviceKeySql}");
            }

            // Mark this connection with the real deviceKey
            _connections[Context.ConnectionId] = (userId, deviceKey);

            // Find other live connections for this driver with a different deviceKey
            var otherConnections = _connections
                .Where(kvp => kvp.Value.DriverId == userId && kvp.Key != Context.ConnectionId && !string.Equals(kvp.Value.DeviceKey, deviceKey, StringComparison.Ordinal));

            foreach (var kvp in otherConnections)
            {
                var otherConnId = kvp.Key;
                // Tell the other client to log out / stop
                _ = Clients.Client(otherConnId).SendAsync("ForceLogout", "You have been signed out because your account is active on another device.");
            }
            return Task.CompletedTask;
        }
        public Task StartShiftNow()
        { // Get and set driver status to ACTIVE_READY (driver is ready for jobs)
            var userId = Context.User?.Id();
            if (userId == null || userId <= 0) throw new HubException("Driver is not authenticated.");
            DataTable DT = App.Database.DataAccessManager.GetDataTable(Database.Schema.Config.Name, Database.Schema.Config.Tables.DriverStatus);
            DataRow[] activeStatusRows = DT.Select("code='ACTIVE_READY'");
            if (activeStatusRows.Length == 0) throw new HubException("Active – Ready for jobs driver status is not configured.");
            _ = App.Database.DataAccessManager.Update(Database.Schema.Entities.Name, Database.Schema.Entities.Tables.Driver, $"driver_status_id = {activeStatusRows[0]["id"]}", $"id = {userId}");

            
            
            // At this point:
            // - Client will treat this as a successful "start shift now"
            // - DriverShiftCtaClick()'s .then(...) will run
            // - setDriverAvailability(true) will flip the bottom nav to READY
            // - The DRIVE NOW button will be hidden on the client

            // When you're ready to persist active shifts (driver_session, etc.),
            // we can add that logic here against your actual tables.
            return Task.CompletedTask;
        }
        public Task SetDriverStatus(int statusId)
        {
            var userId = Context.User?.Id();
            if (userId == null || userId <= 0)
                throw new HubException("Driver is not authenticated.");

            // Only allow user-selectable ACTIVE_* statuses:
            // 25 = ACTIVE_OFFLINE, 26 = ACTIVE_READY, 28 = ACTIVE_BREAK, 29 = ACTIVE_SNOOZED
            if (statusId != 25 && statusId != 26 && statusId != 28 && statusId != 29) throw new HubException("This driver status cannot be set manually.");

            // Ensure the status exists in cfg.driver_status
            var dt = App.Database.DataAccessManager.GetDataTable(Database.Schema.Config.Name, Database.Schema.Config.Tables.DriverStatus, $"id = {statusId}");
            if (dt == null || dt.Rows.Count == 0) throw new HubException("Unknown driver status.");

            // Persist the chosen status against this driver
            _ = App.Database.DataAccessManager.Update(Database.Schema.Entities.Name, Database.Schema.Entities.Tables.Driver, $"driver_status_id = {statusId}", $"id = {userId}");
            return Task.CompletedTask;
        }
        public async Task SaveShiftSlots(List<DriverShiftSlotDTO> slots)
        {
            if (slots == null || slots.Count == 0)
                throw new HubException("No shift slots were provided.");

            var userId = Context.User?.Id();
            if (userId == null || userId <= 0)
                throw new HubException("Driver is not authenticated.");

            var driverId = userId.Value;

            // Normalise: throw away garbage, ensure Start < End
            var validSlots = new List<DriverShiftSlotDTO>();

            foreach (var slot in slots)
            {
                var startOad = slot.StartIso;
                var endOad = slot.EndIso;

                if (double.IsNaN(startOad) || double.IsNaN(endOad))
                    continue;

                if (endOad <= startOad)
                    continue;

                validSlots.Add(slot);
            }

            if (validSlots.Count == 0)
                throw new HubException("No valid slots were provided.");

            // Determine 15-minute grid and window start
            // 96 slots per 24 hours → 24 * 60 / 15 = 96
            double slotsPerDay = 96.0;

            double minStartOad = validSlots.Min(s => s.StartIso);
            // Align window start to the previous 15-minute boundary
            double windowStartOad = Math.Floor(minStartOad * slotsPerDay) / slotsPerDay;

            var mask = new char[96];
            for (int i = 0; i < mask.Length; i++)
                mask[i] = '0';

            foreach (var slot in validSlots)
            {
                double startOffsetDays = slot.StartIso - windowStartOad;
                double endOffsetDays = slot.EndIso - windowStartOad;

                // Convert offsets into 15-minute slot indices
                int startIndex = (int)Math.Floor(startOffsetDays * slotsPerDay);
                int endIndexExclusive = (int)Math.Ceiling(endOffsetDays * slotsPerDay);

                if (endIndexExclusive <= 0 || startIndex >= mask.Length)
                    continue;

                if (startIndex < 0) startIndex = 0;
                if (endIndexExclusive > mask.Length) endIndexExclusive = mask.Length;

                if (startIndex >= endIndexExclusive)
                    continue;

                for (int i = startIndex; i < endIndexExclusive; i++)
                {
                    mask[i] = '1';
                }
            }

            var maskString = new string(mask);
            var maskSql = Database.Shared.Sanitize(maskString, true);

            const string schema = Database.Schema.Operations.Name;
            const string table = Database.Schema.Operations.Tables.DriverShift;

            // Upsert single row per driver
            var existing = App.Database.DataAccessManager.GetDataTable(
                schema,
                table,
                $"driver_id = {driverId}"
            );

            if (existing != null && existing.Rows.Count > 0)
            {
                _ = App.Database.DataAccessManager.Update(
                    schema,
                    table,
                    $"window_start_oad = {windowStartOad}, slots_mask = {maskSql}",
                    $"driver_id = {driverId}"
                );
            }
            else
            {
                string fields = "driver_id, window_start_oad, slots_mask";
                string values = $"{driverId}, {windowStartOad}, {maskSql}";

                _ = App.Database.DataAccessManager.Insert(
                    schema,
                    table,
                    fields,
                    values
                );
            }

            await Clients.Caller.SendAsync("ShiftSaved");
        }
        public Task<List<DriverShiftSlotDTO>> GetShiftSlots()
        {
            var userId = Context.User?.Id();
            if (userId == null || userId <= 0)
                throw new HubException("Driver is not authenticated.");

            var driverId = userId.Value;

            const string schema = Database.Schema.Operations.Name;
            const string table = Database.Schema.Operations.Tables.DriverShift;

            var dt = App.Database.DataAccessManager.GetDataTable(
                schema,
                table,
                $"driver_id = {driverId}"
            );

            var result = new List<DriverShiftSlotDTO>();

            if (dt == null || dt.Rows.Count == 0)
                return Task.FromResult(result);

            var row = dt.Rows[0];

            double windowStartOad = Convert.ToDouble(row["window_start_oad"]);
            string mask = Convert.ToString(row["slots_mask"]) ?? string.Empty;

            if (string.IsNullOrEmpty(mask))
                return Task.FromResult(result);

            //double slotsPerDay = 96.0;              // 15-minute resolution
            double minutesPerSlot = 15.0;
            double daysPerSlot = minutesPerSlot / (24.0 * 60.0);

            int length = mask.Length;
            int index = 0;

            while (index < length)
            {
                if (mask[index] != '1')
                {
                    index++;
                    continue;
                }

                int runStart = index;
                int runEndExclusive = index + 1;

                while (runEndExclusive < length && mask[runEndExclusive] == '1')
                {
                    runEndExclusive++;
                }

                double startOad = windowStartOad + runStart * daysPerSlot;
                double endOad = windowStartOad + runEndExclusive * daysPerSlot;

                result.Add(new DriverShiftSlotDTO
                {
                    StartIso = startOad,
                    EndIso = endOad
                });

                index = runEndExclusive;
            }

            return Task.FromResult(result);
        }
        public Task<List<DriverActiveJobDTO>> GetActiveJobs()
        {
            var userId = Context.User?.Id();
            if (userId == null || userId <= 0)
                throw new HubException("Driver is not authenticated.");

            var result = new List<DriverActiveJobDTO>();

            // 1) Find the participant row for this user (driver)
            DataTable participantDt = Database.DataAccessManager.GetDataTable(Database.Schema.Entities.Name, Database.Schema.Entities.Tables.Participant, $"user_id = {userId} AND is_active = true AND participant_type = 'DRIVER'");
            if (participantDt == null || participantDt.Rows.Count == 0) return Task.FromResult(result);

            long participantId = Convert.ToInt64(participantDt.Rows[0]["id"]);

            // 2) All assignments for this participant
            DataTable assignmentDt = Database.DataAccessManager.GetDataTable(Database.Schema.Operations.Name, Database.Schema.Operations.Tables.ParticipantAssignment, $"participant_id = {participantId}");
            if (assignmentDt == null || assignmentDt.Rows.Count == 0) return Task.FromResult(result);

            var jobIdSet = new HashSet<long>();
            foreach (DataRow row in assignmentDt.Rows)
            {
                if (row["job_id"] == DBNull.Value) continue;
                jobIdSet.Add(Convert.ToInt64(row["job_id"]));
            }

            if (jobIdSet.Count == 0) return Task.FromResult(result);
            string? jobIdCsv = string.Join(",", jobIdSet);

            // 3) Jobs for those IDs
            DataTable jobDt = Database.DataAccessManager.GetDataTable(Database.Schema.Operations.Name, Database.Schema.Operations.Tables.Job, $"id IN ({jobIdCsv})");
            if (jobDt == null || jobDt.Rows.Count == 0) return Task.FromResult(result);

            // 4) Lookup job_status codes
            var statusIdSet = new HashSet<long>();
            foreach (DataRow row in jobDt.Rows)
            {
                if (row["job_status_id"] == DBNull.Value) continue;
                statusIdSet.Add(Convert.ToInt64(row["job_status_id"]));
            }

            var statusById = new Dictionary<long, string>(); // id -> code
            if (statusIdSet.Count > 0)
            {
                string? statusIdCsv = string.Join(",", statusIdSet);
                DataTable statusDt = Database.DataAccessManager.GetDataTable(Database.Schema.Config.Name, Database.Schema.Config.Tables.JobStatus, $"id IN ({statusIdCsv})");
                if (statusDt != null)
                {
                    foreach (DataRow row in statusDt.Rows)
                    {
                        long idVal = Convert.ToInt64(row["id"]);
                        string? codeVal = Convert.ToString(row["code"]) ?? "";
                        statusById[idVal] = codeVal;
                    }
                }
            }

            // 5) Tasks for these jobs (for recipient + destination)
            var taskByJob = new Dictionary<long, DataRow>();
            var dropoffUserAddressIds = new HashSet<long>();

            DataTable taskDt = Database.DataAccessManager.GetDataTable(Database.Schema.Operations.Name, Database.Schema.Operations.Tables.JobTask, $"job_id IN ({jobIdCsv})");
            if (taskDt != null)
            {
                foreach (DataRow row in taskDt.Rows)
                {
                    if (row["job_id"] == DBNull.Value) continue;
                    long jobId = Convert.ToInt64(row["job_id"]);

                    // First task wins if multiple tasks per job
                    if (!taskByJob.ContainsKey(jobId)) taskByJob[jobId] = row;

                    if (row["dropoff_address_id"] != DBNull.Value) dropoffUserAddressIds.Add(Convert.ToInt64(row["dropoff_address_id"]));
                }
            }

            // 6) ent.user_address + loc.address for dropoff text
            var userAddressById = new Dictionary<long, DataRow>();
            var locAddressIds = new HashSet<long>();

            if (dropoffUserAddressIds.Count > 0)
            {
                string? uaIdCsv = string.Join(",", dropoffUserAddressIds);
                DataTable uaDt = Database.DataAccessManager.GetDataTable(Database.Schema.Entities.Name, Database.Schema.Entities.Tables.UserAddress, $"id IN ({uaIdCsv})");
                if (uaDt != null)
                {
                    foreach (DataRow row in uaDt.Rows)
                    {
                        long idVal = Convert.ToInt64(row["id"]);
                        userAddressById[idVal] = row;
                        if (row["address_id"] != DBNull.Value) locAddressIds.Add(Convert.ToInt64(row["address_id"]));
                    }
                }
            }

            var locAddressById = new Dictionary<long, DataRow>();

            if (locAddressIds.Count > 0)
            {
                string? locIdCsv = string.Join(",", locAddressIds);
                DataTable locDt = App.Database.DataAccessManager.GetDataTable(Database.Schema.Locations.Name, Database.Schema.Locations.Tables.Address, $"id IN ({locIdCsv})");
                if (locDt != null)
                {
                    foreach (DataRow row in locDt.Rows)
                    {
                        long idVal = Convert.ToInt64(row["id"]);
                        locAddressById[idVal] = row;
                    }
                }
            }

            // 7) Build DTOs: only non-terminal, active driver phases
            foreach (DataRow jobRow in jobDt.Rows)
            {
                long jobId = Convert.ToInt64(jobRow["id"]);
                long statusId = jobRow["job_status_id"] != DBNull.Value ? Convert.ToInt64(jobRow["job_status_id"]) : 0;

                if (!statusById.TryGetValue(statusId, out var statusCode)) continue;
                if (statusCode != "ASSIGNED" && statusCode != "EN_ROUTE" && statusCode != "PICKED_UP") continue;

                string destinationText = "Unknown destination";
                string? recipient = null;

                if (taskByJob.TryGetValue(jobId, out var taskRow))
                {
                    if (taskRow["recipient"] != DBNull.Value) recipient = Convert.ToString(taskRow["recipient"]);

                    if (taskRow["dropoff_address_id"] != DBNull.Value)
                    {
                        var uaId = Convert.ToInt64(taskRow["dropoff_address_id"]);
                        if (userAddressById.TryGetValue(uaId, out var uaRow) && uaRow["address_id"] != DBNull.Value)
                        {
                            var locId = Convert.ToInt64(uaRow["address_id"]);
                            if (locAddressById.TryGetValue(locId, out var locRow))
                            {
                                var num = Convert.ToString(locRow["street_number"]) ?? "";
                                var street = Convert.ToString(locRow["street_name"]) ?? "";
                                var text = (num + " " + street).Trim();
                                if (!string.IsNullOrEmpty(text))
                                    destinationText = text;
                            }
                        }
                    }
                }

                var statusLabel = statusCode switch
                {
                    "ASSIGNED" => "ASSIGNED",
                    "EN_ROUTE" => "EN_ROUTE",
                    "PICKED_UP" => "PICKED_UP",
                    _ => statusCode
                };

                string sub = statusLabel;
                if (!string.IsNullOrWhiteSpace(recipient)) sub = statusLabel + " – " + recipient;

                var label = $"JOB {jobId} – {destinationText.ToUpperInvariant()}";

                result.Add(new DriverActiveJobDTO
                {
                    Id = jobId,
                    Label = label,
                    SubLabel = sub
                });
            }

            return Task.FromResult(result);
        }
        public async Task SendQuickNotification(long templateId, long jobId)
        {
            var senderId = Context.User?.Id();
            if (senderId == null || senderId <= 0) throw new HubException("Driver is not authenticated.");

            if (templateId <= 0) throw new HubException("Invalid template id.");
            if (jobId <= 0) throw new HubException("Invalid job id.");

            // 1) Load template row from msg.quick_notification
            DataTable templateDt = App.Database.DataAccessManager.GetDataTable(Database.Schema.Messaging.Name, Database.Schema.Messaging.Tables.QuickNotification, $"id = {templateId}");
            if (templateDt == null || templateDt.Rows.Count == 0) throw new HubException("Notification template not found.");

            var trow = templateDt.Rows[0];
            var typeCode = Convert.ToString(trow["type_code"]) ?? "";
            var title = Convert.ToString(trow["label"]) ?? "";
            var messageHtml = Convert.ToString(trow["message"]) ?? "";

            // 2) Get recipient user_id from job (recipient derived from job.user_id)
            DataTable jobDt = App.Database.DataAccessManager.GetDataTable(Database.Schema.Operations.Name, Database.Schema.Operations.Tables.Job, $"id = {jobId}");
            if (jobDt == null || jobDt.Rows.Count == 0) throw new HubException("Job not found.");

            long recipientId = Convert.ToInt64(jobDt.Rows[0]["user_id"]);

            // 3) Lookup notification type metadata (icon, importance) using typeCode
            var sanitizedTypeCode = Database.Shared.Sanitize((typeCode ?? string.Empty).ToLower(), true);
            DataTable typeDt = App.Database.DataAccessManager.GetDataTable(Database.Schema.Messaging.Name, Database.Schema.Messaging.Tables.NotificationType, $"code = {sanitizedTypeCode} AND is_active = true"
            );
            if (typeDt == null || typeDt.Rows.Count == 0) throw new HubException("Notification type not found.");

            var typeRow = typeDt.Rows[0];
            string iconClass = Convert.ToString(typeRow["default_icon_class"]) ?? "";
            int importance = Convert.ToInt32(typeRow["importance_default"] ?? 0);

            double nowOad = DateTime.UtcNow.ToOADate();

            // 4) Insert notification row into msg.notification
            string fields = "recipient_user_id, sender_user_id, related_job_id, type_code, title, message_html, is_read, created_on_oad, importance, icon_class, ding";

            string values = $"{recipientId}, {senderId}, {jobId}, " +
                            $"{Database.Shared.Sanitize(typeCode?.ToLower() ?? "", true)}, " +
                            $"{Database.Shared.Sanitize(title ?? "", true)}, " +
                            $"{Database.Shared.Sanitize(messageHtml ?? "", true)}, " +
                            $"false, {nowOad}, {importance}, " +
                            $"{Database.Shared.Sanitize(iconClass ?? "", true)}, true";

            _ = App.Database.DataAccessManager.Insert(
                App.Database.Schema.Messaging.Name,
                App.Database.Schema.Messaging.Tables.Notification,
                fields,
                values
            );

            // 5) Push via SignalR to recipient if connected (reuses DriverHub._connections)
            // NOTE: if recipient is a customer you must ensure connections map includes customers.
            var recipientConnections = DriverHub._connections
                .Where(kvp => kvp.Value.DriverId == recipientId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var connId in recipientConnections)
            {
                await Clients.Client(connId).SendAsync("ReceiveNotification", new
                {
                    RecipientUserId = recipientId,
                    SenderUserId = senderId,
                    JobId = jobId,
                    TemplateId = templateId,
                    TypeCode = typeCode?.ToLower(),
                    Title = title,
                    MessageHtml = messageHtml,
                    IconClass = iconClass,
                    Importance = importance,
                    CreatedOnOad = nowOad
                });
            }
        }
    }
}