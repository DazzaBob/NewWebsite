using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Security;

namespace Website.Pages.Home.PickitUp.Endpoints
{
    [IgnoreAntiforgeryToken]
    public class TaskCardModel : PageModel
    {
        public class TaskCard
        {
            public string Recipient { get; set; } = string.Empty;
            public string Reference { get; set; } = string.Empty;
            public string PickupAddressId { get; set; } = string.Empty;
            public string DropoffAddressId { get; set; } = string.Empty;
            public string PackageTypeId { get; set; } = string.Empty;
            public string PackageSizeId { get; set; } = string.Empty;
            public string ReadyTime { get; set; } = string.Empty;
            public string SpecificTime { get; set; } = string.Empty;
            public string PickupNotes { get; set; } = string.Empty;
            public string DropoffNotes { get; set; } = string.Empty;
            public string DropoffTypeId { get; set; } = string.Empty;
        }
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] TaskCard task, int index)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };

            if (task is null)
                return BadRequest(new { ok = false, msg = "Invalid payload." });

            string pickupLabel = BuilldAddress(Convert.ToInt64(task.PickupAddressId));
            string dropoffLabel = BuilldAddress(Convert.ToInt64(task.DropoffAddressId));

            var packageTypeName = App.Database.DataAccessManager.GetDataTable(App.Database.Schema.Entities.SchemaName, App.Database.Schema.Entities.PackageType, $"id={task.PackageTypeId}").Rows[0]["NAME"].ToString();
            var packageSizeName = App.Database.DataAccessManager.GetDataTable(App.Database.Schema.Entities.SchemaName, App.Database.Schema.Entities.PackageSize, $"id={task.PackageSizeId}").Rows[0]["NAME"].ToString();
            var dropoffTypeName = App.Database.DataAccessManager.GetDataTable(App.Database.Schema.Entities.SchemaName, App.Database.Schema.Entities.DropoffType, $"id={task.DropoffTypeId}").Rows[0]["NAME"].ToString();
            var readyTimeDisplay = task.ReadyTime == "now" ? "Ready Now" : task.SpecificTime;

            // Build the HTML card
            System.Text.StringBuilder sb = new();
            sb.Append($"<div class='card' id='PIUC{index}' style='padding:0.5rem; border:1px solid var(--color-primary); text-align:left; margin-bottom:0.5rem;'>")
              .Append("<table><tr><th></th><th></th></tr>")
              .Append("<tr><td></td><td style=\"display:flex; justify-content:flex-end;\">")
              .Append($"<button type=\"button\" class=\"btn btn-outline address-delete\" onclick=\"PIUremoveTask({index});\" style=\"display:block;\">")
              .Append("<i class=\"fa-solid fa-trash\"></i>")
              .Append("</button></td></tr>")
              .Append($"<tr><td style='text-align:right;'><b>Recipient:</b></td><td>{task.Recipient}</td></tr>");

            if (!string.IsNullOrWhiteSpace(task.Reference))
                sb.Append($"<tr><td style='text-align:right;'><b>Reference:</b></td><td>{task.Reference}</td></tr>");

            sb.Append($"<tr><td style='text-align:right;'><b>Pickup:</b></td><td>{pickupLabel}</td></tr>")
              .Append($"<tr><td style='text-align:right;'><b>Ready:</b></td><td>{readyTimeDisplay}</td></tr>")
              .Append($"<tr><td style='text-align:right;'><b>Type:</b></td><td>{packageTypeName}</td></tr>")
              .Append($"<tr><td style='text-align:right;'><b>Size:</b></td><td>{packageSizeName}</td></tr>");

            if (!string.IsNullOrWhiteSpace(task.PickupNotes))
                sb.Append($"<tr><td style='text-align:right;'><b>Notes:</b></td><td>{task.PickupNotes}</td></tr>");

            sb.Append($"<tr><td style='text-align:right;'><b>Dropoff:</b></td><td>{dropoffLabel}</td></tr>")
              .Append($"<tr><td style='text-align:right;'><b>Type:</b></td><td>{dropoffTypeName}</td></tr>");

            if (!string.IsNullOrWhiteSpace(task.DropoffNotes))
                sb.Append($"<tr><td style='text-align:right;'><b>Notes:</b></td><td>{task.DropoffNotes}</td></tr>");

            sb.Append("<tr class='PIUPV' style='display:none;'><td colspan='2'><hr /></td></tr>")
              .Append($"<tr class='PIUPV' style='display:none;'><td style='text-align:right;'><b>Distance:</b></td><td><span class='task-distance' data-index='{index}'>--</span></td></tr>")
              .Append($"<tr class='PIUPV' style='display:none;'><td style='text-align:right;'><b>Time:</b></td><td><span class='task-time' data-index='{index}'>--</span></td></tr>")
              .Append("<tr class='PIUPV' style='display:none;'><td colspan='2'><hr /></td></tr>");

            sb.Append("<tr class='PIUPV' style='display:none;'><td colspan='2'>")
              .Append("<table style='width:100%'>")
              .Append($"<tr><td style='text-align:right;'><b>Subtotal:</b></td><td style='text-align:right;'><span class='task-subtotal' data-index='{index}'>--</span></td></tr>")
              .Append("</table>")
              .Append("</td></tr>");

            sb.Append("</table>")
              .Append("</div>");

            return Content(sb.ToString(), "text/html");
        }
        private static string BuilldAddress(long AddressId)
        {
            DataRow[] r = App.Database.Views.UserAddress.DataTable(0, 0, AddressId).Select("");

            string label = r[0]["LABEL"] != DBNull.Value ? r[0]["LABEL"].ToString()!.Trim() : "No Label";
            string streetNumber = r[0]["STREET_NUMBER"] != DBNull.Value ? r[0]["STREET_NUMBER"].ToString()!.Trim() : "";
            string streetName = r[0]["STREET_NAME"] != DBNull.Value ? r[0]["STREET_NAME"].ToString()!.Trim() : "";
            string place = r[0]["PLACE_NAME"] != DBNull.Value ? r[0]["PLACE_NAME"].ToString()!.Trim() : "";
            string locality = r[0]["LOCALITY_NAME"] != DBNull.Value ? r[0]["LOCALITY_NAME"].ToString()!.Trim() : "";
            string address = string.Join(", ", new[] { $"{streetNumber} {streetName}".Trim(), locality, place }
                                        .Where(s => !string.IsNullOrEmpty(s)));

            return $"{label} - {address}";
        }
    }
}
