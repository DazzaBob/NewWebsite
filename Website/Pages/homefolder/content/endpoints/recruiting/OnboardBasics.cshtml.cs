using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Globalization;
using Website.App.Database;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.recruiting
{
    [IgnoreAntiforgeryToken]
    public class OnboardBasicsModel : PageModel
    {
        public sealed class EndorseDateParts
        {
            public string? issued_d { get; set; }
            public string? issued_m { get; set; }
            public string? issued_y { get; set; }
            public string? expiry_d { get; set; }
            public string? expiry_m { get; set; }
            public string? expiry_y { get; set; }
        }

        public sealed class Input
        {
            public string? Surname { get; set; }
            public string? Firstnames { get; set; }

            public string? Dob_day { get; set; }
            public string? Dob_month { get; set; }
            public string? Dob_year { get; set; }

            public string? LicenceNumber { get; set; }
            public string? LicenceVersion { get; set; }

            public string? Issued_day { get; set; }
            public string? Issued_month { get; set; }
            public string? Issued_year { get; set; }
            public string? Expires_day { get; set; }
            public string? Expires_month { get; set; }
            public string? Expires_year { get; set; }

            public int[]? ClassIds { get; set; }
            public int[]? EndorsementIds { get; set; }

            // Keyed by endorsement_type_id
            public Dictionary<int, EndorseDateParts>? EndorsementDates { get; set; }
        }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] Input? input)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = 401 };
            if (input == null)
                return new JsonResult(new { ok = false, msg = "Invalid payload." }) { StatusCode = 400 };

            try
            {
                int DriverId = User.Id(); // ent.driver.id == ent.users.id
                double NowOAD = DateTime.UtcNow.ToOADate();
                bool ReapprovalTriggered = false;

                // Current snapshot
                DataTable DrvTbl = DataAccessManager.GetDataTable(Schema.Entities.Name, Schema.Entities.Tables.Driver, $"id = {DriverId}", string.Empty);
                DataRow? Existing = DrvTbl.Rows.Count > 0 ? DrvTbl.Rows[0] : null;

                // Normalize inputs
                string Surname = (input.Surname ?? string.Empty).Trim();
                string Firsts = (input.Firstnames ?? string.Empty).Trim();
                string LicNo = (input.LicenceNumber ?? string.Empty).Trim();
                string LicVer = (input.LicenceVersion ?? string.Empty).Trim();

                double? DobOad = BuildOAD(input.Dob_day, input.Dob_month, input.Dob_year);
                double? IssuedOad = BuildOAD(input.Issued_day, input.Issued_month, input.Issued_year);
                double? ExpiresOad = BuildOAD(input.Expires_day, input.Expires_month, input.Expires_year);

                // Re-approval if approved and identity/licence changed
                if (Existing != null)
                {
                    bool WasApproved = Existing["approved_oad"] != DBNull.Value;
                    bool Changed =
                        !StrEq(Existing["surname"], Surname) ||
                        !StrEq(Existing["firstnames"], Firsts) ||
                        !StrEq(Existing["licence_number"], LicNo) ||
                        !StrEq(Existing["licence_version"], LicVer) ||
                        !OadEq(Existing["dob_oad"], DobOad) ||
                        !OadEq(Existing["issued_oad"], IssuedOad) ||
                        !OadEq(Existing["expires_oad"], ExpiresOad);

                    if (WasApproved && Changed)
                    {
                        string SetReapprove = $"approved_oad = NULL, approvalwithdrawn_oad = {ToSqlOad(NowOAD)}";
                        DataAccessManager.Update(Schema.Entities.Name, Schema.Entities.Tables.Driver, SetReapprove, $"id = {DriverId}");
                        ReapprovalTriggered = true;
                    }
                }

                // Upsert ent.driver
                if (Existing == null)
                {
                    string Fields = "id, surname, firstnames, dob_oad, licence_number, licence_version, issued_oad, expires_oad, applied_oad, isactive, updated_oad";
                    string Values = string.Join(", ",
                        DriverId.ToString(CultureInfo.InvariantCulture),
                        App.Database.Shared.Sanitize(Surname, true, true),
                        App.Database.Shared.Sanitize(Firsts, true, true),
                        ToSqlOad(DobOad),
                        App.Database.Shared.Sanitize(LicNo, true, true),
                        (string.IsNullOrEmpty(LicVer) ? "NULL" : App.Database.Shared.Sanitize(LicVer, true, true)),
                        ToSqlOad(IssuedOad),
                        ToSqlOad(ExpiresOad),
                        ToSqlOad(NowOAD),
                        "TRUE",
                        ToSqlOad(NowOAD)
                    );
                    _ = DataAccessManager.Insert(Schema.Entities.Name, Schema.Entities.Tables.Driver, Fields, Values);
                }
                else
                {
                    string SetSql =
                        $"surname = {App.Database.Shared.Sanitize(Surname, true, true)}, " +
                        $"firstnames = {App.Database.Shared.Sanitize(Firsts, true, true)}, " +
                        $"dob_oad = {ToSqlOad(DobOad)}, " +
                        $"licence_number = {App.Database.Shared.Sanitize(LicNo, true, true)}, " +
                        $"licence_version = {(string.IsNullOrEmpty(LicVer) ? "NULL" : App.Database.Shared.Sanitize(LicVer, true, true))}, " +
                        $"issued_oad = {ToSqlOad(IssuedOad)}, " +
                        $"expires_oad = {ToSqlOad(ExpiresOad)}, " +
                        $"updated_oad = {ToSqlOad(NowOAD)}";
                    DataAccessManager.Update(Schema.Entities.Name, Schema.Entities.Tables.Driver, SetSql, $"id = {DriverId}");
                }

                // Classes — exact set reconcile (ent.driver_licence_class)
                var NewClassIds = new HashSet<int>(input.ClassIds ?? Array.Empty<int>());

                DataTable CurClasses = DataAccessManager.GetDataTable(Schema.Entities.Name, Schema.Entities.Tables.DriverLicenceClass, $"driver_id = {DriverId}", string.Empty);
                var CurClassSet = new HashSet<int>();
                foreach (DataRow r in CurClasses.Rows)
                    if (int.TryParse(r["vehicle_class_id"]?.ToString(), out var Vid)) CurClassSet.Add(Vid);

                foreach (var Vid in CurClassSet)
                    if (!NewClassIds.Contains(Vid))
                        DataAccessManager.Delete(Schema.Entities.Name, Schema.Entities.Tables.DriverLicenceClass, $"driver_id = {DriverId} AND vehicle_class_id = {Vid}");

                foreach (var Vid in NewClassIds)
                    if (!CurClassSet.Contains(Vid))
                    {
                        string F = "driver_id, vehicle_class_id, issued_oad, expiry_oad, updated_oad";
                        string V = $"{DriverId}, {Vid}, NULL, NULL, {ToSqlOad(NowOAD)}";
                        _ = DataAccessManager.Insert(Schema.Entities.Name, Schema.Entities.Tables.DriverLicenceClass, F, V);
                    }

                // Endorsements — exact set reconcile (driver_licence_endorsements)
                var NewEndorseIds = new HashSet<int>(input.EndorsementIds ?? Array.Empty<int>());

                // Use the table you are actively using in your project (you showed Entities in the last working version)
                DataTable CurEnd = DataAccessManager.GetDataTable(Schema.Entities.Name, Schema.Entities.Tables.DriverLicenceEndorsements, $"driver_id = {DriverId}", string.Empty);
                var CurEndSet = new HashSet<int>();
                foreach (DataRow r in CurEnd.Rows)
                    if (int.TryParse(r["endorsement_type_id"]?.ToString(), out var Et)) CurEndSet.Add(Et);

                foreach (var Et in CurEndSet)
                    if (!NewEndorseIds.Contains(Et))
                        DataAccessManager.Delete(Schema.Entities.Name, Schema.Entities.Tables.DriverLicenceEndorsements, $"driver_id = {DriverId} AND endorsement_type_id = {Et}");

                foreach (var Et in NewEndorseIds)
                {
                    double? Issued = null, Expiry = null;
                    if (input.EndorsementDates != null && input.EndorsementDates.TryGetValue(Et, out var Parts))
                    {
                        Issued = BuildOAD(Parts.issued_d, Parts.issued_m, Parts.issued_y);
                        Expiry = BuildOAD(Parts.expiry_d, Parts.expiry_m, Parts.expiry_y);
                    }

                    if (CurEndSet.Contains(Et))
                    {
                        string SetSql =
                            $"issued_date = {ToSqlOad(Issued)}, " +
                            $"expiry_date = {ToSqlOad(Expiry)}, " +
                            $"updated_at = {ToSqlOad(NowOAD)}";
                        DataAccessManager.Update(Schema.Entities.Name, Schema.Entities.Tables.DriverLicenceEndorsements, SetSql, $"driver_id = {DriverId} AND endorsement_type_id = {Et}");
                    }
                    else
                    {
                        string F = "driver_id, endorsement_type_id, issued_date, expiry_date, updated_at";
                        string V = $"{DriverId}, {Et}, {ToSqlOad(Issued)}, {ToSqlOad(Expiry)}, {ToSqlOad(NowOAD)}";
                        _ = DataAccessManager.Insert(Schema.Entities.Name, Schema.Entities.Tables.DriverLicenceEndorsements, F, V);
                    }
                }

                return new JsonResult(new { ok = true, reapprovalTriggered = ReapprovalTriggered });
            }
            catch (Exception ex)
            {
                App.Bootstrap.Logger?.Add("OnboardBasics: " + ex.Message, App.Helper.Logger.LogLevel.Error);
                return new JsonResult(new { ok = false, msg = "Internal server error." })
                { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        private static bool StrEq(object? db, string incoming)
        {
            var a = db == null || db is DBNull ? string.Empty : db.ToString() ?? string.Empty;
            return string.Equals(a.Trim(), (incoming ?? string.Empty).Trim(), StringComparison.Ordinal);
        }

        private static bool OadEq(object? db, double? incoming)
        {
            if (db == null || db is DBNull) return incoming == null;
            if (!double.TryParse(db.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var a)) return incoming == null;
            if (incoming == null) return false;
            return Math.Abs(a - incoming.Value) < 1e-9;
        }

        private static string ToSqlOad(double? d) =>
            d.HasValue ? d.Value.ToString(CultureInfo.InvariantCulture) : "NULL";

        private static double? BuildOAD(string? d, string? m, string? y)
        {
            if (string.IsNullOrWhiteSpace(d) || string.IsNullOrWhiteSpace(m) || string.IsNullOrWhiteSpace(y)) return null;
            if (!int.TryParse(d, out var dd) || !int.TryParse(m, out var mm) || !int.TryParse(y, out var yy)) return null;
            if (mm < 1 || mm > 12 || dd < 1 || dd > 31 || yy < 1900 || yy > 2100) return null;
            try { return new DateTime(yy, mm, dd, 0, 0, 0, DateTimeKind.Utc).ToOADate(); }
            catch { return null; }
        }
    }
}