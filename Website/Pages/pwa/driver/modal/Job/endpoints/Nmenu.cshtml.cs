using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Text;
using Website.App.Security;

namespace Website.Pages.pwa.driver.modal.Job.endpoints
{
    public class Request
    {
        public long JobId { get; set; }
        public string? Stage { get; set; }
    }

    [IgnoreAntiforgeryToken]
    public class NmenuModel : PageModel
    {
        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] Request req)
        {
            if (!User.IsAuthorised()) return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            if (req == null || req.JobId <= 0) return new JsonResult(new { ok = false, msg = "Invalid Job id." }) { StatusCode = StatusCodes.Status400BadRequest };


            var JobStatusID = App.Database.DataAccessManager.ExecuteScalar(App.Database.Schema.Operations.Name, $"SELECT job_status_id FROM ops.job WHERE id = {req.JobId}", []);
            DataTable DT = App.Database.DataAccessManager.GetDataTable(App.Database.Schema.Messaging.Name, App.Database.Schema.Messaging.Tables.QuickNotification, $"job_status_id = {JobStatusID}", "id ASC");

            StringBuilder sb = new();

            sb.Append("<div class=\"card\">")
              .Append("<table style=\"width: 100%;\"><tr><td style=\"width:90%;\"><h5 id=\"driverjobnotificationsmodaltitle\"><span id=\"driver-jobnoticications-display-id\">Send Notification</span></h5>")
              .Append("</td><td style=\"width:10%;\"><button id=\"MJOBNOTBTNCLS\" class=\"btn btn-close\" type=\"button\" aria-label=\"Close\" onclick=\"CloseDJContent();\">×</button></td></tr></table> ");
            if (DT.Rows.Count == 0)
            {
                sb.Append("<p>No notifications available for this stage.</p>");
            }
            else
            {
                foreach (DataRow row in DT.Rows)
                {
                    var templateId = row["id"]?.ToString() ?? "0";
                    var label = row["label"]?.ToString() ?? "";
                    sb.Append("<button type=\"button\" class=\"btn btn-outline quick-notify\" ")
                      .Append("data-templateid=\"").Append(templateId).Append("\" ")
                      .Append("data-jobid=\"").Append(req.JobId).Append("\" ")
                      .Append("onclick=\"sendQuickNotification(")
                      .Append(templateId).Append(", ")
                      .Append(req.JobId).Append(");\">")
                      .Append(System.Net.WebUtility.HtmlEncode(label))
                      .Append("</button>");
                }
            }
            sb.Append("</div>");

            Response.ContentType = "text/html";
            return Content(sb.ToString());
        }
    }
}