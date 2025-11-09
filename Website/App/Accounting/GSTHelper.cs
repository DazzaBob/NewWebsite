using Npgsql;
using Website.App.Database;
namespace Website.App.Accounting
{
    public static class GstHelper
    {
        private const string Schema = Database.Schema.Accounting.Name;
        public static decimal GetTaxRate(long taxCodeId)
        {
            string sql = $"SELECT rate FROM {Schema}.tax_code WHERE id = @id AND is_active = true LIMIT 1;";
            NpgsqlParameter[] p = [new("@id", taxCodeId)];
            object? result = DataAccessManager.ExecuteScalar(Schema, sql, p);
            return result == null ? 0m : Convert.ToDecimal(result);
        }

        /// <summary>
        /// Splits a given amount into GST and Net components, IRD compliant.
        /// </summary>
        /// <param name="amount">Total value (inclusive or exclusive of GST)</param>
        /// <param name="rate">GST rate as decimal (e.g. 0.15 for 15%)</param>
        /// <param name="inclusive">True if amount includes GST, false if exclusive.</param>
        /// <returns>Tuple of (gstPortion, netAmount)</returns>
        public static (decimal gstPortion, decimal netAmount) SplitGST(decimal amount, decimal rate, bool inclusive = false)
        {
            if (rate <= 0m) return (0m, amount);

            decimal gst, net;

            if (inclusive)
            {
                // IRD method: GST fraction = rate / (1 + rate)
                gst = Math.Round(amount * (rate / (1 + rate)), 2);
                net = Math.Round(amount - gst, 2);
            }
            else
            {
                // Exclusive price: GST added as amount * rate
                gst = Math.Round(amount * rate, 2);
                net = Math.Round(amount, 2);
            }

            return (gst, net);
        }

        /// <summary>
        /// Overload using tax_code.id lookup
        /// </summary>
        public static (decimal gstPortion, decimal netAmount) SplitGST(decimal amount, long taxCodeId, bool inclusive = false)
        {
            decimal rate = GetTaxRate(taxCodeId);
            return SplitGST(amount, rate, inclusive);
        }
    }
}
