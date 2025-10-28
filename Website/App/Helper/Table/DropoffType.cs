using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace Website.App.Helper.Table
{
    public class DropoffType
    {
        private static SelectList selectlist = new(Enumerable.Empty<SelectListItem>());
        private static readonly object lockAssignSelectListObj = new();
        public static SelectList DropoffTypeOptions()
        {
            var (list, error) = GetDropoffTypeOptions(); // Load the address types for the dropdown

            SelectList SelectOption = new("");
            if (!string.IsNullOrEmpty(error))
                return SelectOption;

            SelectOption = list;
            return SelectOption;
        }
        public static (SelectList List, string? Error) GetDropoffTypeOptions()
        {
            lock (lockAssignSelectListObj)
            {
                if (selectlist.Items.Cast<object>().Any())
                { return (selectlist, string.Empty); } // already loaded

                try
                {
                    DataRow[] DR = Database.DataAccessManager.GetDataTable(Database.Schema.Entities.SchemaName, Database.Schema.Entities.DropoffType).Select("");
                    var items = DR
                        .Cast<DataRow>()
                        .Select(r => new
                        {
                            ID = Convert.ToInt32(r.Field<int>("id")),
                            NAME = r.Field<string>("name") + " (" + r.Field<string>("description") + ")" ?? string.Empty
                        })
                        .ToList();

                    selectlist = new SelectList(items, "ID", "NAME");
                    return (selectlist, string.Empty);
                }
                catch (Exception ex)
                {
                    return (new SelectList(Enumerable.Empty<object>()), "Dropoff Type load error: " + ex.Message);
                }
            }
        }
    }
}
