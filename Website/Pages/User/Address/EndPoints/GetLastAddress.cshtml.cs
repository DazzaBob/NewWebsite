using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Security;

namespace Website.Pages.User.Address.EndPoints
{
    [IgnoreAntiforgeryToken]
    public class GetLastAddressModel : PageModel
    {
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost()
        {
            if (!User.IsAuthorised()) return Unauthorized();
            int userId = User.Id();

            DataRow[] DR = App.Database.Views.DataTables.UserAddress.DataTable(userId).Select("", "ID DESC");
            if (DR.Length == 0) return NotFound(new { ok = false, msg = "Address not found." });
            string Id = DR[0]["ID"] != DBNull.Value ? DR[0]["ID"].ToString()!.Trim() : "";
            string label = DR[0]["LABEL"] != DBNull.Value ? DR[0]["LABEL"].ToString()!.Trim() : "No Label";
            string streetNumber = DR[0]["STREET_NUMBER"] != DBNull.Value ? DR[0]["STREET_NUMBER"].ToString()!.Trim() : "";
            string streetName = DR[0]["STREET_NAME"] != DBNull.Value ? DR[0]["STREET_NAME"].ToString()!.Trim() : "";
            string place = DR[0]["PLACE_NAME"] != DBNull.Value ? DR[0]["PLACE_NAME"].ToString()!.Trim() : "";
            string locality = DR[0]["LOCALITY_NAME"] != DBNull.Value ? DR[0]["LOCALITY_NAME"].ToString()!.Trim() : "";
            string address = string.Join(", ", new[] { $"{streetNumber} {streetName}".Trim(), locality, place }.Where(s => !string.IsNullOrEmpty(s)));

            return new JsonResult(new { ok = true, Id, label, address });
        }
    }
}
