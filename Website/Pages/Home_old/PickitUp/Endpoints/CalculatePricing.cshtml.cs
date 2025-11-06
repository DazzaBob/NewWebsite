using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Text;
using System.Text.Json;
using Website.App.Security;

namespace Website.Pages.Home.PickitUp.Endpoints
{
    [IgnoreAntiforgeryToken]
    public class CalculatePricingModel : PageModel
    {
        public class TaskCard
        {
            public string recipient { get; set; } = string.Empty;
            public string reference { get; set; } = string.Empty;
            public string pickupAddressId { get; set; } = string.Empty;
            public string dropoffAddressId { get; set; } = string.Empty;
            public string packageTypeId { get; set; } = string.Empty;
            public string packageSizeId { get; set; } = string.Empty;
            public string readyTime { get; set; } = string.Empty;
            public string specificTime { get; set; } = string.Empty;
            public string pickupNotes { get; set; } = string.Empty;
            public string dropoffNotes { get; set; } = string.Empty;
            public string dropoffTypeId { get; set; } = string.Empty;
        }
        private record RatePolicy(decimal Flagfall, decimal PerMeter, decimal PerSecond, decimal Minimum, decimal Markup);
        public IActionResult OnGet() => NotFound();
        public async Task<IActionResult> OnPost([FromBody] List<TaskCard> tasks)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = 401 };
            if (tasks == null || tasks.Count == 0)
                return new JsonResult(new { ok = false, msg = "Invalid payload." }) { StatusCode = 400 };

            try
            {
                using DataTable DT = RatePolicyDT();

                var allAddressIds = tasks.SelectMany(t => new[] { t.pickupAddressId, t.dropoffAddressId }).Distinct();
                var coordsLookup = LoadAddressCoords(allAddressIds);

                // Map coordinates to task indices
                Dictionary<string, List<int>> coordMap = [];
                List<string> uniqueCoords = [];
                for (int i = 0; i < tasks.Count; i++)
                {
                    var t = tasks[i];
                    var pickupCoord = coordsLookup[t.pickupAddressId];
                    var dropoffCoord = coordsLookup[t.dropoffAddressId];

                    foreach (var c in new[] { pickupCoord, dropoffCoord })
                    {
                        if (!coordMap.TryGetValue(c, out List<int>? value))
                        {
                            value = [];
                            coordMap[c] = value;
                            uniqueCoords.Add(c);
                        }
                        value.Add(i);
                    }
                }

                // Mapbox optimization call (first 10 unique coords)
                List<string> mapboxCoords = uniqueCoords.Take(10).ToList();
                var mapboxResponse = await CallMapboxOptimization(mapboxCoords, profile: "driving");
                var legDurations = ParseLegs(mapboxResponse);

                // Prepare per-task outputs and accumulate job subtotal
                decimal jobSubtotal = 0m;
                var taskResponses = new List<object>();
                double totalDistanceMetersFromMapbox = 0;
                double totalDurationSecondsFromMapbox = 0;
                decimal minimumChargeTotal = 0;

                int legIndex = 0; // sequential mapping for tasks
                for (int i = 0; i < tasks.Count; i++)
                {
                    var t = tasks[i];

                    // Lookup rate policy
                    DataRow[] matchingRows = DT.Select($"package_type_id = {t.packageTypeId} AND (user_id IS NULL OR user_id = {User.Id()})");
                    DataRow? rateRow = matchingRows.OrderByDescending(r =>
                        (r.Table.Columns.Contains("zones_base_id") && r["zones_base_id"] != DBNull.Value ? 1 : 0) +
                        (r.Table.Columns.Contains("user_id") && r["user_id"] != DBNull.Value ? 1 : 0)).FirstOrDefault();

                    if (rateRow == null)
                    {
                        rateRow = DT.NewRow();
                        rateRow["flagfall"] = 3.0m;
                        rateRow["per_meter_rate"] = 0.005m;
                        rateRow["per_second_rate"] = 0.01m;
                        rateRow["minimum_charge"] = 6.0m;
                        rateRow["markup_multiplier"] = 1.2m;
                    }

                    decimal flagfall = (decimal)rateRow["flagfall"];
                    decimal perMeter = (decimal)rateRow["per_meter_rate"];
                    decimal perSecond = (decimal)rateRow["per_second_rate"];
                    decimal minimumCharge = (decimal)rateRow["minimum_charge"];
                    minimumChargeTotal += minimumCharge;

                    // Get distance and duration from Mapbox sequentially
                    var (distanceMeters, durationSeconds) = legDurations.TryGetValue((legIndex, legIndex + 1), out var val)
                        ? val
                        : (0, 0);
                    legIndex++;

                    // If single-task job, enforce per-task minimum
                    decimal baseCost = flagfall + (perMeter * distanceMeters) + (perSecond * durationSeconds);
                    if (tasks.Count == 1 && baseCost < minimumCharge) baseCost = minimumCharge;

                    jobSubtotal += baseCost;

                    taskResponses.Add(new
                    {
                        index = i,
                        distanceMeters,
                        travelTimeMinutes = Math.Round(durationSeconds / 60.0),
                        subtotal = baseCost,
                        displayDiscount = 0m // no shared pickup/dropoff fudge
                    });
                }

                // Extract Mapbox-provided totals
                try
                {
                    using var doc = JsonDocument.Parse(mapboxResponse);
                    if (doc.RootElement.TryGetProperty("trips", out JsonElement trips) && trips.GetArrayLength() > 0)
                    {
                        var firstTrip = trips[0];
                        if (firstTrip.TryGetProperty("distance", out JsonElement td)) totalDistanceMetersFromMapbox = td.GetDouble();
                        if (firstTrip.TryGetProperty("duration", out JsonElement tt)) totalDurationSecondsFromMapbox = tt.GetDouble();
                    }
                }
                catch
                {
                    // ignore parse errors
                }

                // Sliding scale minimum for multi-task jobs
                if (tasks.Count > 1)
                {
                    decimal scaledMinimum = (minimumChargeTotal / tasks.Count) + ((tasks.Count - 1) * (minimumChargeTotal / tasks.Count * 0.5m));
                    if (jobSubtotal < scaledMinimum) jobSubtotal = scaledMinimum;
                }

                // Apply job-level markup
                decimal jobMarkup = DT.Rows.Count > 0 && DT.Columns.Contains("markup_multiplier")
                    ? (decimal)DT.Rows[0]["markup_multiplier"]
                    : 1.2m;

                decimal grandTotal = Math.Round(jobSubtotal * jobMarkup, 2);

                // Warning if total duration exceeds 15 minutes
                string? warning = totalDurationSecondsFromMapbox > 900
                    ? "Warning: Estimated total delivery time exceeds 15 minutes."
                    : null;

                var response = new
                {
                    ok = true,
                    tasks = taskResponses,
                    totalDistanceMeters = (int)Math.Round(totalDistanceMetersFromMapbox),
                    totalTravelTimeMinutes = Math.Round(totalDurationSecondsFromMapbox / 60.0),
                    serviceFee = (jobSubtotal * jobMarkup) - jobSubtotal,
                    total = grandTotal,
                    warning
                };

                return new JsonResult(response);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = "Error calculating pricing", error = ex.Message }) { StatusCode = 500 };
            }
        }
        private DataTable RatePolicyDT()
        {
            StringBuilder sb = new();
            sb.Append("SELECT * FROM cfg.rate_policies rp ")
              .Append($"WHERE (rp.user_id IS NULL OR rp.user_id = {User.Id()}) ");

            DataTable DT = App.Database.DataAccessManager.GetDataTable(App.Database.Schema.Entities.Name, sb.ToString(), []);
            if (DT.Rows.Count == 0)
            {
                if (DT.Columns.Count == 0)
                {
                    DT.Columns.Add("package_type_id", typeof(int));
                    DT.Columns.Add("flagfall", typeof(decimal));
                    DT.Columns.Add("per_meter_rate", typeof(decimal));
                    DT.Columns.Add("per_second_rate", typeof(decimal));
                    DT.Columns.Add("minimum_charge", typeof(decimal));
                    DT.Columns.Add("markup_multiplier", typeof(decimal));
                    DT.Columns.Add("policy_version_id", typeof(int));
                }
                DataRow defaultRow = DT.NewRow();
                defaultRow["package_type_id"] = 3;        // generic fallback
                defaultRow["flagfall"] = 3.0m;
                defaultRow["per_meter_rate"] = 0.005m;
                defaultRow["per_second_rate"] = 0.01m;
                defaultRow["minimum_charge"] = 6.0m;
                defaultRow["markup_multiplier"] = 1.2m;
                defaultRow["policy_version_id"] = 0;
                DT.Rows.Add(defaultRow);
            }

            return DT;
        }
        private static Dictionary<string, string> LoadAddressCoords(IEnumerable<string> addressIds)
        {
            var result = new Dictionary<string, string>();
            if (!addressIds.Any()) return result;

            string idsCsv = string.Join(",", addressIds.Distinct());
            string sql = $@"SELECT ua.ID, ua.USER_ID, a.ID AS ADDRESS_ID, a.LONGITUDE, a.LATITUDE
            FROM {App.Database.Schema.Entities.Tables.UserAddress} ua
            LEFT JOIN {App.Database.Schema.Locations.Tables.Address} a ON ua.ADDRESS_ID = a.ID 
            WHERE ua.ID IN({idsCsv}); ";

            DataTable DT = App.Database.DataAccessManager.GetDataTable("loc", sql, []);

            foreach (DataRow row in DT.Rows)
            {
                string id = row["id"].ToString()!;
                double lon = Convert.ToDouble(row["longitude"]);
                double lat = Convert.ToDouble(row["latitude"]);
                result[id] = $"{lon},{lat}";
            }

            foreach (var id in addressIds)
                if (!result.ContainsKey(id)) result[id] = "0,0";

            return result;
        }
        private static async Task<string> CallMapboxOptimization(List<string> uniqueCoords, string profile)
        {
            if (uniqueCoords == null || uniqueCoords.Count < 2) return "{}"; // not enough points

            string coords = string.Join(";", uniqueCoords);
            string token = App.Settings.MapboxToken;
            // Only request the data we actually need
            string url = $"https://api.mapbox.com/optimized-trips/v1/mapbox/{profile}/{coords}?overview=false&steps=false&geometries=geojson&roundtrip=false&source=first&destination=last&access_token={token}";

            try
            {
                using HttpClient http = new();
                HttpResponseMessage response = await http.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                App.Bootstrap.Logger?.Add($"Mapbox Optimization API call failed: {ex.Message}");
                return "{}";
            }
        }
        private static Dictionary<(int, int), (int, int)> ParseLegs(string mapboxResponse)
        {
            var result = new Dictionary<(int, int), (int, int)>();
            if (string.IsNullOrWhiteSpace(mapboxResponse) || !mapboxResponse.Contains("legs"))
                return result;

            try
            {
                using var doc = JsonDocument.Parse(mapboxResponse);
                var root = doc.RootElement;

                if (!root.TryGetProperty("trips", out JsonElement trips) || trips.GetArrayLength() == 0)
                    return result;

                var firstTrip = trips[0];
                if (!firstTrip.TryGetProperty("legs", out JsonElement legs))
                    return result;

                int fromIndex = 0;
                foreach (var leg in legs.EnumerateArray())
                {
                    double distance = leg.GetProperty("distance").GetDouble();   // meters
                    double duration = leg.GetProperty("duration").GetDouble();   // seconds

                    int toIndex = fromIndex + 1;
                    result[(fromIndex, toIndex)] = ((int)Math.Round(distance), (int)Math.Round(duration));
                    fromIndex++;
                }
            }
            catch (Exception ex)
            {
                App.Bootstrap.Logger?.Add($"Failed to parse Mapbox legs: {ex.Message}");
            }

            return result;
        }
    }
}
