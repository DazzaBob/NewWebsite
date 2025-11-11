using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Accounting;
using Website.App.Database;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.paymentmethod
{
    [IgnoreAntiforgeryToken]
    public class ConfirmAndPayModel : PageModel
    {
        public class ConfirmAndPayInput
        {
            public long PaymentId { get; set; }
            public string? Pricing { get; set; } // JSON snapshot from front-end
        }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] ConfirmAndPayInput input)
        {
            if (!User.IsAuthorised()) return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            if (input == null || input.PaymentId <= 0)
                return new JsonResult(new { ok = false, msg = "Invalid payment reference." })
                { StatusCode = StatusCodes.Status400BadRequest };

            try
            {
                long userId = User.Id();

                // 1️ Validate payment belongs to user
                DataTable dt = App.Database.Views.DataTables.UserPayments.DataTable(input.PaymentId, userId);
                if (dt == null || dt.Rows.Count == 0)
                    return new JsonResult(new { ok = false, msg = "Payment method not found or inactive." })
                    { StatusCode = StatusCodes.Status400BadRequest };

                DataRow payRow = dt.Rows[0];

                // 2️ Parse pricing JSON snapshot
                decimal totalAmount = 0m;
                string currency = "NZD";
                bool isPrivate = false;
                long? taxCodeId = null;

                if (!string.IsNullOrWhiteSpace(input.Pricing))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(input.Pricing);
                    var root = doc.RootElement;

                    static decimal GetDec(System.Text.Json.JsonElement el, string name, decimal fallback = 0)
                        => el.TryGetProperty(name, out var v) && v.TryGetDecimal(out var d) ? d : fallback;

                    static string GetStr(System.Text.Json.JsonElement el, string name, string fallback = "")
                        => el.TryGetProperty(name, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String
                            ? v.GetString() ?? fallback : fallback;

                    static bool GetBool(System.Text.Json.JsonElement el, string name, bool fallback = false)
                        => el.TryGetProperty(name, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.True ? true
                         : v.ValueKind == System.Text.Json.JsonValueKind.False ? false : fallback;

                    totalAmount = GetDec(root, "TotalAmount", GetDec(root, "total", 0));
                    currency = GetStr(root, "Currency", GetStr(root, "CurrencyCode", "NZD")).ToUpperInvariant();
                    isPrivate = GetBool(root, "IsPrivate", GetBool(root, "private", false));

                    if (root.TryGetProperty("TaxCodeId", out var tce) && tce.TryGetInt64(out var tcid))
                        taxCodeId = tcid;
                }

                if (totalAmount <= 0m)
                    return new JsonResult(new { ok = false, msg = "Pricing total is zero or invalid." })
                    { StatusCode = StatusCodes.Status400BadRequest };

                // 3️ Find or create job
                string findJobSql = $@"SELECT id FROM ops.job WHERE user_id = {userId} AND payment_id = {input.PaymentId} AND job_status_id = (SELECT id FROM cfg.job_status WHERE code IN ('CREATED', 'PENDING_ALLOCATION') LIMIT 1);";
                object? jobIdObj = DataAccessManager.ExecuteScalar("pub", findJobSql, []);
                long jobId = jobIdObj == null || jobIdObj == DBNull.Value ? 0L : Convert.ToInt64(jobIdObj);

                if (jobId > 0)
                {
                    _ = DataAccessManager.Update(Schema.Operations.Name, Schema.Operations.Tables.Job, $"total_amount_cents={(int)(totalAmount * 100)}, updated_on_oad={DateTime.UtcNow.ToOADate()}", $"id={jobId}");
                }
                else
                {
                    string insertJobSql = $@"
                        INSERT INTO ops.job (
                            user_id, payment_id, job_status_id, allocation_status_id,
                            total_amount_cents, currency, created_on_oad, updated_on_oad
                        )
                        VALUES (
                            {userId}, {input.PaymentId},
                            (SELECT id FROM cfg.job_status WHERE code='CREATED' LIMIT 1),
                            (SELECT id FROM cfg.allocation_status WHERE code='PENDING' LIMIT 1),
                            {(int)(totalAmount * 100)}, '{currency}',
                            {DateTime.UtcNow.ToOADate()}, {DateTime.UtcNow.ToOADate()}
                        )
                        RETURNING id;";

                    object? newIdObj = DataAccessManager.ExecuteScalar("pub", insertJobSql);
                    if (newIdObj == null || newIdObj == DBNull.Value)
                        return new JsonResult(new { ok = false, msg = "Failed to create job." })
                        { StatusCode = StatusCodes.Status500InternalServerError };

                    jobId = Convert.ToInt64(newIdObj);
                }

                // 4️ Build Accounting Contexts
                var jobCtx = new AccountingJournalBuilder.JobContext
                {
                    JobId = jobId,
                    CustomerEntityId = userId,
                    DriverEntityId = null,
                    TotalAmount = totalAmount,
                    IsPrivate = isPrivate,
                    ServiceFeeAmount = null,
                    Description = "Job accepted",
                    CurrencyCode = currency,
                    TaxCodeId = taxCodeId,
                    CreatedByUserEntityId = userId
                };

                // Determine payment mode by provider (basic mapping; refine later)
                string providerCode = payRow["provider_code"].ToString()?.ToUpperInvariant() ?? "";
                var paymentMode = providerCode switch
                {
                    "STRIPE" or "GATEWAY" => AccountingJournalBuilder.PaymentMode.GatewayAuthHold,
                    "CASH" or "CARD" => AccountingJournalBuilder.PaymentMode.CashOrCard,
                    "INVOICE" => AccountingJournalBuilder.PaymentMode.Invoice,
                    "CREDIT" => AccountingJournalBuilder.PaymentMode.Credit,
                    "IOU" => AccountingJournalBuilder.PaymentMode.IOU,
                    _ => AccountingJournalBuilder.PaymentMode.Unknown
                };

                var payCtx = new AccountingJournalBuilder.PaymentContext
                {
                    Mode = paymentMode,
                    Amount = totalAmount,
                    ExternalReference = payRow["vault_token"].ToString(),
                    PostingDate = DateTime.UtcNow.Date
                };

                // 5️ Build and post the journal
                var journal = AccountingJournalBuilder.BuildJobAcceptanceJournal(jobCtx, payCtx);
                var postResult = AccountingHelper.PostJournal(journal);

                if (!postResult.ok)
                    return new JsonResult(new { ok = false, msg = postResult.message, job_id = jobId })
                    { StatusCode = StatusCodes.Status500InternalServerError };

                // 6️ Return success
                return new JsonResult(new
                {
                    ok = true,
                    msg = "Job created and accounting journal posted.",
                    job_id = jobId,
                    journal_id = postResult.journalId
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message })
                { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}