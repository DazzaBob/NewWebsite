using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace Website.App.Helper.Table
{
    public class AddressType
    {
        private static SelectList selectlist = new(Enumerable.Empty<SelectListItem>());
        private static readonly object lockAssignSelectListObj = new();

        public static (SelectList List, string? Error) GetAddressTypeOptions(App.Helper.Connection LocationConnection)
        {
            lock (lockAssignSelectListObj)
            {
                if (selectlist.Items.Cast<object>().Any())
                { return (selectlist, string.Empty); } // already loaded

                try
                {
                    using DataTable dt = Database.Shared.GetDataTable(LocationConnection, Database.Schema.Locations.Tables.AddressType);

                    var items = dt.Rows
                        .Cast<DataRow>()
                        .Select(r => new
                        {
                            ID = Convert.ToInt32(r.Field<long>("ID")),
                            NAME = r.Field<string>("NAME") ?? string.Empty
                        })
                        .ToList();


                    selectlist = new SelectList(items, "ID", "NAME");
                    return (selectlist, string.Empty);
                }
                catch (Exception ex)
                {
                    return (new SelectList(Enumerable.Empty<object>()), "AddressType load error: " + ex.Message);
                }
            }

        }
    }
}
