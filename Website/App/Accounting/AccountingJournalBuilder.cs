using System;
using System.Collections.Generic;
using Website.App.Accounting;

namespace Website.App.Accounting
{
    /// <summary>
    /// Translates operational events (job acceptance, completion, payouts, refunds, write-offs)
    /// into AccountingHelper.JournalModel instances. No DB transactions here; caller posts.
    /// </summary>
    public static class AccountingJournalBuilder
    {
        // -----------------------------
        // Context models
        // -----------------------------
        public enum PaymentMode
        {
            Unknown = 0,
            CashOrCard,        // immediate funds (banked)
            GatewayAuthHold,   // authorised, not captured
            Invoice,           // AR via invoice (Invoiced Receivables)
            Credit,            // AR via credit terms (Credit Receivables)
            IOU,               // informal receivable (Customer IOU)
            AutoPayDirectDebit // trust/escrow account collection
        }

        public sealed class JobContext
        {
            /// <summary>ops.job.id (operational job primary key)</summary>
            public long JobId { get; init; }

            /// <summary>ent.users.id for the customer</summary>
            public long CustomerEntityId { get; init; }

            /// <summary>ent.users.id for the driver (optional until allocated)</summary>
            public long? DriverEntityId { get; init; }

            /// <summary>Total job amount (gross). GST treatment decided by TaxCodeId and account gst_applicability.</summary>
            public decimal TotalAmount { get; init; }

            /// <summary>True if the job is private / non-taxable (friend-to-friend, non-GSTable).</summary>
            public bool IsPrivate { get; init; }

            /// <summary>Optional service fee component (if split posting is needed later).</summary>
            public decimal? ServiceFeeAmount { get; init; }

            /// <summary>Journal description seed (appears on header/lines).</summary>
            public string Description { get; init; } = string.Empty;

            /// <summary>NZD default unless specified otherwise.</summary>
            public string CurrencyCode { get; init; } = "NZD";

            /// <summary>Tax code to apply to revenue lines (e.g., GST15, ZERO, EXEMPT). Null means no GST.</summary>
            public long? TaxCodeId { get; init; }

            /// <summary>Created-by user id (ent.users.id) for audit.</summary>
            public long CreatedByUserEntityId { get; init; }
        }

        public sealed class PaymentContext
        {
            /// <summary>How the customer is (intending to) pay at acceptance time.</summary>
            public PaymentMode Mode { get; init; }

            /// <summary>Amount covered by this payment context (gross). If null, assumes job.TotalAmount.</summary>
            public decimal? Amount { get; init; }

            /// <summary>Optional external gateway reference / token (for subledger linkage later).</summary>
            public string? ExternalReference { get; init; }

            /// <summary>Posting date for the journal (header doc_date / posting_date). Defaults to UtcNow.Date.</summary>
            public DateTime? PostingDate { get; init; }
        }

        public sealed class RefundContext
        {
            public decimal RefundAmount { get; init; }
            public string Reason { get; init; } = "Refund";
            public DateTime? PostingDate { get; init; }
            /// <summary>True if refund happens after job completion (i.e., reverse revenue). False = pre-completion (reverse liability).</summary>
            public bool AfterCompletion { get; init; }

            /// <summary>True if the original job was private/non-GSTable (friend-to-friend). Ensures refund does not post GST lines.</summary>
            public bool IsPrivate { get; init; }
        }

        // -----------------------------
        // Public builder entry points
        // -----------------------------

        /// <summary>
        /// Customer confirms job (“Confirm & Pay”). Recognise cash/hold/receivable vs. deferred revenue.
        /// </summary>
        public static AccountingHelper.JournalModel BuildJobAcceptanceJournal(JobContext job, PaymentContext payment)
        {
            var amount = payment.Amount ?? job.TotalAmount;
            var date = (payment.PostingDate ?? DateTime.UtcNow.Date);

            var j = NewJournal(date, $"Job {job.JobId} accepted – {job.Description}", job.CurrencyCode, job.CreatedByUserEntityId);

            // ------------------------------------------------------------
            // Job Acceptance Journal (funds or receivable recognised)
            // ------------------------------------------------------------
            short lineNo = 1;
            string desc = $"Job {job.JobId} acceptance";

            switch (payment.Mode)
            {
                case PaymentMode.CashOrCard:
                    // Immediate funds received into main bank; liability to perform service
                    AddByCode(j, lineNo++, "1000", amount, 0m, desc, job.CustomerEntityId);
                    AddByCode(j, lineNo++, "2100", 0m, amount, desc, job.CustomerEntityId);
                    break;

                case PaymentMode.GatewayAuthHold:
                    // Funds authorised via gateway; not yet captured
                    AddByCode(j, lineNo++, "1050", amount, 0m, desc, job.CustomerEntityId);
                    AddByCode(j, lineNo++, "2210", 0m, amount, desc, job.CustomerEntityId);
                    break;

                case PaymentMode.Invoice:
                    // Customer to be invoiced later; receivable arises immediately
                    AddByCode(j, lineNo++, "1140", amount, 0m, desc, job.CustomerEntityId);
                    AddByCode(j, lineNo++, "2120", 0m, amount, desc, job.CustomerEntityId);
                    break;

                case PaymentMode.Credit:
                    // Customer has credit terms; record receivable
                    AddByCode(j, lineNo++, "1150", amount, 0m, desc, job.CustomerEntityId);
                    AddByCode(j, lineNo++, "2120", 0m, amount, desc, job.CustomerEntityId);
                    break;

                case PaymentMode.IOU:
                    // Informal promise to pay; customer IOU
                    AddByCode(j, lineNo++, "1110", amount, 0m, desc, job.CustomerEntityId);
                    AddByCode(j, lineNo++, "2120", 0m, amount, desc, job.CustomerEntityId);
                    break;

                case PaymentMode.AutoPayDirectDebit:
                    // Funds collected automatically via trust/escrow
                    AddByCode(j, lineNo++, "1010", amount, 0m, desc, job.CustomerEntityId);
                    AddByCode(j, lineNo++, "2100", 0m, amount, desc, job.CustomerEntityId);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported payment mode: {payment.Mode}");
            }

            // No GST posting yet; revenue will be recognised on completion.
            return j;
        }

        /// <summary>
        /// Service delivered. Move deferred revenue to revenue. Accrue driver cost to driver payable.
        /// For invoice/credit/IOU-only jobs, this is where revenue is first recognised (Dr AR / Cr Revenue).
        /// </summary>
        public static AccountingHelper.JournalModel BuildJobCompletionJournal(JobContext job)
        {
            var date = DateTime.UtcNow.Date;
            var j = NewJournal(
                date,
                $"Job {job.JobId} completed – {job.Description}",
                job.CurrencyCode,
                job.CreatedByUserEntityId);

            // ------------------------------------------------------------
            // Job Completion Journal – revenue recognition & cost accrual
            // ------------------------------------------------------------
            short lineNo = 1;
            string desc = $"Job {job.JobId} completed";
            decimal amount = job.TotalAmount;

            // (A) Revenue recognition
            // If acceptance created deferred revenue → release to income
            // Otherwise, recognise income directly from receivable
            // Choose accounts based on job type or deferred stage
            // For now, assume standard commercial GSTable job.

            bool usedDeferred = true; // later this can be inferred from payment mode if needed

            // Determine which revenue account applies
            string revenueCode = job.IsPrivate ? "4010" : "4000";   // 4010 = private, 4000 = GSTable
            long? taxId = job.IsPrivate ? null : job.TaxCodeId;      // no GST on private jobs

            if (usedDeferred)
            {
                // Move deferred liability to revenue
                AddByCode(j, lineNo++, "2100", amount, 0m, desc, job.CustomerEntityId);
                AddByCode(j, lineNo++, revenueCode, 0m, amount, desc, job.CustomerEntityId, taxId);
            }
            else
            {
                // Directly from receivable types (invoice, credit, IOU)
                AddByCode(j, lineNo++, "1140", amount, 0m, desc, job.CustomerEntityId);
                AddByCode(j, lineNo++, revenueCode, 0m, amount, desc, job.CustomerEntityId, taxId);
            }

            // (B) Driver payout accrual (cost of sales)
            // Recognise driver cost payable (net of GST)
            decimal driverCost = Math.Round(amount * 0.75m, 2); // placeholder ratio; to be replaced by actual pricing engine value
            AddByCode(j, lineNo++, "5000", driverCost, 0m, $"{desc} – Driver payout", job.DriverEntityId, job.TaxCodeId);
            AddByCode(j, lineNo++, "2010", 0m, driverCost, $"{desc} – Driver payout", job.DriverEntityId, job.TaxCodeId);

            // GST will auto-post via AccountingHelper.PostJournal()

            return j;
        }

        /// <summary>
        /// When funds actually settle/capture later (e.g., gateway capture or customer pays invoice).
        /// </summary>
        public static AccountingHelper.JournalModel BuildPaymentSettlementJournal(
            long jobId,
            long customerEntityId,
            decimal amount,
            string currencyCode,
            long createdByUserEntityId,
            PaymentMode settlementMode,
            DateTime? postingDate = null,
            string description = "Payment settlement")
        {
            var date = (postingDate ?? DateTime.UtcNow.Date);
            var j = NewJournal(date, $"Job {jobId} – {description}", currencyCode, createdByUserEntityId);

            // ------------------------------------------------------------
            // Payment Settlement Journal – funds received after deferred
            // or receivable recognition.
            // ------------------------------------------------------------
            short lineNo = 1;
            string desc = $"Job {jobId} – Payment settlement";
            decimal amt = Math.Round(amount, 2);

            switch (settlementMode)
            {
                case PaymentMode.GatewayAuthHold:
                    // Gateway has captured funds; move from gateway clearing to bank
                    AddByCode(j, lineNo++, "1000", amt, 0m, $"{desc} (gateway capture)", customerEntityId);
                    AddByCode(j, lineNo++, "1050", 0m, amt, $"{desc} (gateway capture)", customerEntityId);
                    break;

                case PaymentMode.CashOrCard:
                    // Already banked at acceptance; no new journal needed
                    break;

                case PaymentMode.Invoice:
                    // Customer pays invoice
                    AddByCode(j, lineNo++, "1000", amt, 0m, $"{desc} (invoice paid)", customerEntityId);
                    AddByCode(j, lineNo++, "1140", 0m, amt, $"{desc} (invoice paid)", customerEntityId);
                    break;

                case PaymentMode.Credit:
                    // Credit settlement received
                    AddByCode(j, lineNo++, "1000", amt, 0m, $"{desc} (credit settlement)", customerEntityId);
                    AddByCode(j, lineNo++, "1150", 0m, amt, $"{desc} (credit settlement)", customerEntityId);
                    break;

                case PaymentMode.IOU:
                    // IOU settled in cash
                    AddByCode(j, lineNo++, "1000", amt, 0m, $"{desc} (IOU repaid)", customerEntityId);
                    AddByCode(j, lineNo++, "1110", 0m, amt, $"{desc} (IOU repaid)", customerEntityId);
                    break;

                case PaymentMode.AutoPayDirectDebit:
                    // Direct debit funds transferred from trust/escrow to main bank
                    AddByCode(j, lineNo++, "1000", amt, 0m, $"{desc} (AutoPay transfer)", customerEntityId);
                    AddByCode(j, lineNo++, "1010", 0m, amt, $"{desc} (AutoPay transfer)", customerEntityId);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported settlement mode: {settlementMode}");
            }

            // optional: if you explicitly convert hold→deferred on completion before capture
            // AddByCode(j, lineNo++, "2210", amt, 0m, $"{desc} (release hold)", customerEntityId);
            // AddByCode(j, lineNo++, "2100", 0m, amt, $"{desc} (release hold)", customerEntityId);

            return j;
        }

        /// <summary>
        /// Pay the driver. Clears 2010 → 1130 (wallet) or 1000 (bank).
        /// </summary>
        public static AccountingHelper.JournalModel BuildDriverPayoutJournal(
            long jobId,
            long? driverEntityId,
            decimal payoutAmount,
            bool payFromWalletNotBank,
            string currencyCode,
            long createdByUserEntityId,
            DateTime? postingDate = null,
            string description = "Driver payout")
        {
            var date = (postingDate ?? DateTime.UtcNow.Date);
            var j = NewJournal(date, $"Job {jobId} – {description}", currencyCode, createdByUserEntityId);
            // ------------------------------------------------------------
            // Driver Payout Journal – clears driver payable via wallet or bank
            // ------------------------------------------------------------
            short lineNo = 1;
            string desc = $"Job {jobId} – Driver payout";
            decimal amt = Math.Round(payoutAmount, 2);

            if (payFromWalletNotBank)
            {
                // Funds moved to driver wallet account (in-platform liability transfer)
                AddByCode(j, lineNo++, "2010", amt, 0m, $"{desc} to wallet", driverEntityId);
                AddByCode(j, lineNo++, "1130", 0m, amt, $"{desc} to wallet", driverEntityId);
            }
            else
            {
                // Paid directly via bank
                AddByCode(j, lineNo++, "2010", amt, 0m, $"{desc} to bank", driverEntityId);
                AddByCode(j, lineNo++, "1000", 0m, amt, $"{desc} to bank", driverEntityId);
            }
            return j;
        }

        /// <summary>
        /// Refunds: pre-completion (reverse deferred) or post-completion (reverse revenue + cash).
        /// </summary>
        public static AccountingHelper.JournalModel BuildRefundJournal(
            JobContext job,
            RefundContext refund)
        {
            var date = (refund.PostingDate ?? DateTime.UtcNow.Date);
            var j = NewJournal(date, $"Job {job.JobId} – Refund: {refund.Reason}", job.CurrencyCode, job.CreatedByUserEntityId);

            // ------------------------------------------------------------
            // Refund Journal – reverses either liability (pre-completion)
            // or revenue (post-completion). Handles private vs GSTable.
            // ------------------------------------------------------------
            short lineNo = 1;
            string desc = $"Job {job.JobId} refund – {refund.Reason}";
            decimal amount = refund.RefundAmount;

            // Private jobs (non-GSTable) → no TaxCodeId
            long? taxId = refund.IsPrivate ? null : job.TaxCodeId;
            string revenueCode = refund.IsPrivate ? "4010" : "4000";

            if (refund.AfterCompletion)
            {
                // Reverse earned revenue and record refund
                AddByCode(j, lineNo++, revenueCode, amount, 0m, desc, job.CustomerEntityId, taxId);
                AddByCode(j, lineNo++, "5520", 0m, amount, $"{desc} expense", job.CustomerEntityId);
                AddByCode(j, lineNo++, "1000", 0m, amount, $"{desc} cash refund", job.CustomerEntityId);
            }
            else
            {
                // Pre-completion → cancel liability and release funds
                AddByCode(j, lineNo++, "2100", amount, 0m, desc, job.CustomerEntityId);
                AddByCode(j, lineNo++, "1000", 0m, amount, $"{desc} cash refund", job.CustomerEntityId);
            }
            return j;
        }

        /// <summary>
        /// Write off uncollectable receivables after credit term expires.
        /// </summary>
        public static AccountingHelper.JournalModel BuildWriteoffJournal(
            long jobId,
            long customerEntityId,
            decimal amount,
            string currencyCode,
            long createdByUserEntityId,
            PaymentMode receivableType,
            DateTime? postingDate = null,
            string description = "Credit write-off")
        {
            var date = (postingDate ?? DateTime.UtcNow.Date);
            var j = NewJournal(date, $"Job {jobId} – {description}", currencyCode, createdByUserEntityId);

            // ------------------------------------------------------------
            // Write-off Journal – uncollectable receivable
            // ------------------------------------------------------------
            short lineNo = 1;
            string desc = $"Job {jobId} – Write-off";
            decimal amt = Math.Round(amount, 2);

            // Determine which receivable account to credit based on the original payment mode
            string receivableCode = receivableType switch
            {
                PaymentMode.Invoice => "1140",
                PaymentMode.Credit => "1150",
                PaymentMode.IOU => "1110",
                _ => throw new InvalidOperationException($"Unsupported receivable type for write-off: {receivableType}")
            };

            // Recognise loss expense and clear receivable
            AddByCode(j, lineNo++, "5510", amt, 0m, $"{desc} (uncollectable)", customerEntityId);
            AddByCode(j, lineNo++, receivableCode, 0m, amt, $"{desc} (cleared)", customerEntityId);

            // Optional GST reversal (if GST was recognised at completion)
            if (receivableType is PaymentMode.Invoice or PaymentMode.Credit or PaymentMode.IOU)
            {
                // Calculate GST portion for reversal (simple 15% default; ideally derive from tax code)
                decimal gstRate = 0.15m;
                decimal gstPortion = Math.Round(amt * gstRate / (1 + gstRate), 2);

                // Reverse output GST: Dr GST Collected (2300) / Cr GST Clearing (9020)
                AddByCode(j, lineNo++, "2300", gstPortion, 0m, $"{desc} GST reversal", customerEntityId);
                AddByCode(j, lineNo++, "9020", 0m, gstPortion, $"{desc} GST reversal", customerEntityId);
            }

            return j;
        }

        // -----------------------------
        // Internal helpers for stubbing lines cleanly
        // -----------------------------

        private static AccountingHelper.JournalModel NewJournal(DateTime date, string description, string currencyCode, long createdByUserEntityId)
            => new AccountingHelper.JournalModel
            {
                Date = date,
                Description = description,
                CurrencyCode = currencyCode,
                CreatedByUserId = createdByUserEntityId,
                Lines = new List<AccountingHelper.JournalLineModel>()
            };

        /// <summary>
        /// Append a new line using an account CODE (dynamic ID via AccountingHelper.IdByCode).
        /// </summary>
        private static void AddByCode(AccountingHelper.JournalModel j, short lineNo, string accountCode,
            decimal debit, decimal credit, string? description = null,
            long? entityId = null, long? taxCodeId = null)
        {
            var accountId = AccountingHelper.IdByCode(accountCode);
            j.Lines.Add(new AccountingHelper.JournalLineModel
            {
                LineNo = lineNo,
                AccountId = accountId,
                Debit = Math.Round(debit, 2),
                Credit = Math.Round(credit, 2),
                Description = description,
                EntityId = entityId,
                TaxCodeId = taxCodeId
            });
        }

        /// <summary>
        /// Append a new line using a known account ID.
        /// </summary>
        private static void Add(AccountingHelper.JournalModel j, short lineNo, long accountId,
            decimal debit, decimal credit, string? description = null,
            long? entityId = null, long? taxCodeId = null)
        {
            j.Lines.Add(new AccountingHelper.JournalLineModel
            {
                LineNo = lineNo,
                AccountId = accountId,
                Debit = Math.Round(debit, 2),
                Credit = Math.Round(credit, 2),
                Description = description,
                EntityId = entityId,
                TaxCodeId = taxCodeId
            });
        }
    }
}
