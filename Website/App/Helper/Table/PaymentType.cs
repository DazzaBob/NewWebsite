using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace Website.App.Helper.Table
{
    public class PaymentType
    {
        private static SelectList selectlist = new(Enumerable.Empty<SelectListItem>());
        private static readonly object lockAssignSelectListObj = new();
        public static SelectList PaymentTypeOptions()
        {
            var (list, error) = GetPaymentTypeOptions(); // Load the address types for the dropdown

            SelectList SelectOption = new("");
            if (!string.IsNullOrEmpty(error))
                return SelectOption;

            SelectOption = list;
            return SelectOption;
        }
        public static (SelectList List, string? Error) GetPaymentTypeOptions()
        {
            lock (lockAssignSelectListObj)
            {
                if (selectlist.Items.Cast<object>().Any())
                { return (selectlist, string.Empty); } // already loaded

                try
                {
                    using DataTable dt = Database.DataAccessManager.GetDataTable(Database.Schema.Config.Name, Database.Schema.Config.Tables.PaymentType, "is_enabled=true");
                    var items = dt.Rows
                        .Cast<DataRow>()
                        .Select(r => new
                        {
                            ID = Convert.ToInt64(r.Field<long>("ID")),
                            NAME = r.Field<string>("NAME") ?? string.Empty
                        })
                        .ToList();

                    selectlist = new SelectList(items, "ID", "NAME");
                    return (selectlist, string.Empty);
                }
                catch (Exception ex)
                {
                    return (new SelectList(Enumerable.Empty<object>()), "PaymentType load error: " + ex.Message);
                }
            }
        }
    }
}