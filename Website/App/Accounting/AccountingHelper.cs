using Npgsql;
using System.Data;
using Website.App.Database;

namespace Website.App.Accounting
{
    public static class AccountingHelper
    {
        private const string Schema = Database.Schema.Accounting.Name;

        // ------------------------------------------------------------
        // Account ID cache (resolves live from DB, one-time per code)
        // ------------------------------------------------------------
        private static readonly Dictionary<string, long> AccountCache = new();

        /// <summary>
        /// Returns the account ID for a given code, caching results to avoid repeat lookups.
        /// </summary>
        public static long IdByCode(string code)
        {
            if (AccountCache.TryGetValue(code, out var id))
                return id;

            using DataTable dt = DataAccessManager.GetDataTable(
                Schema,
                Database.Schema.Accounting.Tables.Account,
                $"code = '{code}' AND is_active = true");

            if (dt.Rows.Count == 0)
                throw new Exception($"Account code {code} not found or inactive.");

            id = Convert.ToInt64(dt.Rows[0]["id"]);
            AccountCache[code] = id;
            return id;
        }

        // ------------------------------------------------------------
        // Journal models
        // ------------------------------------------------------------
        public class JournalLineModel
        {
            public short LineNo { get; set; }
            public long AccountId { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public string? Description { get; set; }
            public long? EntityId { get; set; }
            public long? TaxCodeId { get; set; }
        }

        public class JournalModel
        {
            public DateTime Date { get; set; }
            public string Description { get; set; } = string.Empty;
            public string CurrencyCode { get; set; } = "NZD";
            public long CreatedByUserId { get; set; }
            public List<JournalLineModel> Lines { get; set; } = [];
        }
        public static bool CanDeleteJournal(long journalId)
        {
            // Journals cannot be deleted once created
            throw new InvalidOperationException("Deleting journals is prohibited for audit integrity.");
        }

        // ------------------------------------------------------------
        // Currency
        // ------------------------------------------------------------
        public static DataTable GetCurrencyList()
            => DataAccessManager.GetDataTable(Schema, Database.Schema.Accounting.Tables.Currency, string.Empty, "code ASC");

        public static bool CurrencyExists(string code)
        {
            const string sql = "SELECT 1 FROM acc.currency WHERE code = @code LIMIT 1;";
            NpgsqlParameter[] p = [new("@code", code)];
            object? result = DataAccessManager.ExecuteScalar(Schema, sql, p);
            return result != null;
        }

        // ------------------------------------------------------------
        // Journal creation & posting
        // ------------------------------------------------------------
        public static long CreateJournal(DateTime date, string description, string currencyCode, long createdByUserId)
        {
            // acc.journal schema confirmed: doc_date, posting_date, posted_at are all double precision
            string fields = "doc_date, posting_date, description, currency_code, created_by";
            NpgsqlParameter[] p =
            [
                new("@doc", date.ToOADate()),
                new("@post", date.ToOADate()),
                new("@desc", description ?? string.Empty),
                new("@cc", currencyCode ?? "NZD"),
                new("@uid", createdByUserId)
            ];

            return Convert.ToInt64(DataAccessManager.Insert(Schema, Database.Schema.Accounting.Tables.Journal, fields, p));
        }

        public static void AddJournalLine(long journalId, short lineNo, long accountId,
            decimal debit, decimal credit, string? description = null,
            long? entityId = null, long? taxCodeId = null)
        {
            string fields = "journal_id, line_no, account_id, entity_id, tax_code_id, debit, credit, description";
            NpgsqlParameter[] p =
            [
                new("@jid", journalId),
                new("@line", lineNo),
                new("@acc", accountId),
                new("@ent", (object?)entityId ?? DBNull.Value),
                new("@tax", (object?)taxCodeId ?? DBNull.Value),
                new("@debit", debit),
                new("@credit", credit),
                new("@desc", (object?)description ?? DBNull.Value)
            ];

            _ = DataAccessManager.Insert(Schema, Database.Schema.Accounting.Tables.JournalLine, fields, p);
        }

        public static bool ValidateJournalBalance(long journalId)
        {
            DataTable dt = DataAccessManager.GetDataTable(
                Schema,
                Database.Schema.Accounting.Tables.JournalLine,
                $"journal_id = {journalId}");

            if (dt.Rows.Count == 0)
                return false;

            object dObj = dt.Compute("SUM(debit)", "");
            object cObj = dt.Compute("SUM(credit)", "");

            decimal debits = dObj is DBNull ? 0m : Convert.ToDecimal(dObj);
            decimal credits = cObj is DBNull ? 0m : Convert.ToDecimal(cObj);
            return Math.Round(debits, 2) == Math.Round(credits, 2);
        }

        // ------------------------------------------------------------
        // Post journal (GST-aware, using IdByCode)
        // ------------------------------------------------------------
        public static (bool ok, string message, long journalId) PostJournal(JournalModel journal)
        {
            if (journal.Lines.Count == 0)
                return (false, "No journal lines provided.", -1);

            if (journal.Lines.Sum(l => l.Debit) == 0 && journal.Lines.Sum(l => l.Credit) == 0)
                return (false, "All debit and credit amounts are zero.", -1);

            long journalId = CreateJournal(journal.Date, journal.Description, journal.CurrencyCode, journal.CreatedByUserId);
            if (journalId <= 0)
                return (false, "Failed to create journal header.", -1);

            foreach (var line in journal.Lines)
            {
                try
                {
                    if (line.TaxCodeId.HasValue && line.TaxCodeId.Value > 0)
                    {
                        decimal gross = line.Debit > 0 ? line.Debit : line.Credit;
                        bool inclusive = false;

                        string sqlCheck = $"SELECT gst_applicability FROM {Schema}.account WHERE id = @id;";
                        NpgsqlParameter[] pCheck = [new("@id", line.AccountId)];
                        object? resultCheck = DataAccessManager.ExecuteScalar(Schema, sqlCheck, pCheck);
                        string gstAppForIncl = resultCheck?.ToString()?.Trim().ToUpperInvariant() ?? "NONE";

                        if (gstAppForIncl == "OUTPUT") inclusive = true;

                        var (gst, net) = GstHelper.SplitGST(gross, line.TaxCodeId.Value, inclusive);

                        string sql = $"SELECT gst_applicability FROM {Schema}.account WHERE id = @id;";
                        NpgsqlParameter[] p = [new("@id", line.AccountId)];
                        object? result = DataAccessManager.ExecuteScalar(Schema, sql, p);
                        string gstApp = result?.ToString()?.Trim().ToUpperInvariant() ?? "NONE";

                        long gstAccountId = gstApp switch
                        {
                            "INPUT" => IdByCode("9010"),
                            "OUTPUT" => IdByCode("2300"),
                            _ => -1
                        };

                        if (gstAccountId <= 0)
                        {
                            AddJournalLine(journalId, line.LineNo, line.AccountId,
                                line.Debit, line.Credit, line.Description, line.EntityId, line.TaxCodeId);
                            continue;
                        }

                        if (line.Debit > 0)
                        {
                            AddJournalLine(journalId, line.LineNo, line.AccountId, net, 0m, line.Description, line.EntityId, line.TaxCodeId);
                            AddJournalLine(journalId, (short)(line.LineNo + 1), gstAccountId, gst, 0m, "GST Component", line.EntityId, line.TaxCodeId);
                        }
                        else
                        {
                            AddJournalLine(journalId, line.LineNo, line.AccountId, 0m, net, line.Description, line.EntityId, line.TaxCodeId);
                            AddJournalLine(journalId, (short)(line.LineNo + 1), gstAccountId, 0m, gst, "GST Component", line.EntityId, line.TaxCodeId);
                        }

                        continue;
                    }

                    AddJournalLine(journalId, line.LineNo, line.AccountId,
                        line.Debit, line.Credit, line.Description, line.EntityId, line.TaxCodeId);
                }
                catch (Exception ex)
                {
                    return (false, $"Failed to add line {line.LineNo}: {ex.Message}", journalId);
                }
            }

            if (!ValidateJournalBalance(journalId))
            {
                _ = DataAccessManager.Update(Schema,
                    Database.Schema.Accounting.Tables.Journal,
                    "status = 'UNBALANCED'",
                    $"id = {journalId}");
                return (false, "Journal unbalanced after insert.", journalId);
            }

            // Properly set posting date and posted_at to OADate values
            double nowOADate = DateTime.UtcNow.ToOADate();

            _ = DataAccessManager.Update(
                Schema,
                Database.Schema.Accounting.Tables.Journal,
                $"status = 'POSTED', posting_date = {nowOADate}, posted_at = {nowOADate}",
                $"id = {journalId}"
            );

            return (true, "Journal successfully posted.", journalId);
        }

        // ------------------------------------------------------------
        // Account utilities
        // ------------------------------------------------------------
        public static DataTable GetAccounts()
            => DataAccessManager.GetDataTable(Schema, Database.Schema.Accounting.Tables.Account, "is_active = true", "code ASC");

        public static string? GetAccountName(long accountId)
        {
            string sql = $"SELECT name FROM {Schema}.account WHERE id = @id LIMIT 1;";
            NpgsqlParameter[] p = [new("@id", accountId)];
            return DataAccessManager.ExecuteScalar(Schema, sql, p)?.ToString();
        }

        public static DataTable GetAccountList(bool onlyActive = true)
        {
            string filter = onlyActive ? "is_active = true" : string.Empty;
            return DataAccessManager.GetDataTable(Schema, Database.Schema.Accounting.Tables.Account, filter, "code ASC");
        }

        public static bool IsPostable(long accountId)
        {
            string sql = $"SELECT is_postable FROM {Schema}.account WHERE id = @id;";
            NpgsqlParameter[] p = [new("@id", accountId)];
            object? result = DataAccessManager.ExecuteScalar(Schema, sql, p);
            return result != null && Convert.ToBoolean(result);
        }

        public static string? GetNormalBalanceSide(long accountId)
        {
            string sql = $@"SELECT t.normal_balance
                            FROM {Schema}.account a
                            JOIN {Schema}.account_type t ON a.type_id = t.id
                            WHERE a.id = @id;";
            NpgsqlParameter[] p = [new("@id", accountId)];
            return DataAccessManager.ExecuteScalar(Schema, sql, p)?.ToString();
        }
    }
}
