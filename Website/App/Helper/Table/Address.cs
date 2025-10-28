using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace Website.App.Helper.Table
{
    public class Address
    {
        private static SelectList selectlist = new(Enumerable.Empty<SelectListItem>());
        private static readonly object lockAssignSelectListObj = new();
        public static SelectList AddressOptions(long userId)
        {
            var (list, error) = GetAddressOptions(userId); // Load the address types for the dropdown

            SelectList SelectOption = new("");
            if (!string.IsNullOrEmpty(error))
                return SelectOption;

            SelectOption = list;
            return SelectOption;
        }
        public static (SelectList List, string? Error) GetAddressOptions(long userId)
        {
            lock (lockAssignSelectListObj)
            {
                if (selectlist.Items.Cast<object>().Any())
                { return (selectlist, string.Empty); } // already loaded

                try
                {
                    using DataTable dt = Database.Views.UserAddress.DataTable(userId);

                    var items = dt.Rows
                        .Cast<DataRow>()
                        .Select(r =>
                        {
                            string userAddressId = r["ID"] != DBNull.Value ? r["ID"].ToString()!.Trim() : "";
                            string label = r["LABEL"] != DBNull.Value ? r["LABEL"].ToString()!.Trim() : "No Label";
                            string streetNumber = r["STREET_NUMBER"] != DBNull.Value ? r["STREET_NUMBER"].ToString()!.Trim() : "";
                            string streetName = r["STREET_NAME"] != DBNull.Value ? r["STREET_NAME"].ToString()!.Trim() : "";
                            string place = r["PLACE_NAME"] != DBNull.Value ? r["PLACE_NAME"].ToString()!.Trim() : "";
                            string locality = r["LOCALITY_NAME"] != DBNull.Value ? r["LOCALITY_NAME"].ToString()!.Trim() : "";
                            string address = string.Join(", ", new[] { $"{streetNumber} {streetName}".Trim(), locality, place }
                                                        .Where(s => !string.IsNullOrEmpty(s)));

                            return new
                            {
                                ID = userAddressId,
                                NAME = label + " - " + address
                            };
                        })
                        .ToList();

                    // Example SelectList using UserAddressId as value and concatenated label + address as text
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
