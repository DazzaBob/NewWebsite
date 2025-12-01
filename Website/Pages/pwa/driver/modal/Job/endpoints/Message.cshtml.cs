using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Text;
using Website.App.Security;

namespace Website.Pages.pwa.driver.modal.Job.endpoints
{
    public class MsgRequest
    {
        public long JobId { get; set; }
    }

    [IgnoreAntiforgeryToken]
    public class MessageModel : PageModel
    {
        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] MsgRequest req)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };

            if (req == null || req.JobId <= 0)
                return new JsonResult(new { ok = false, msg = "Invalid Job id." }) { StatusCode = StatusCodes.Status400BadRequest };

            // Get the job and customer info
            DataTable jobDt = App.Database.DataAccessManager.GetDataTable(App.Database.Schema.Operations.Name, App.Database.Schema.Operations.Tables.Job, $"id = {req.JobId}");
            if (jobDt == null || jobDt.Rows.Count == 0) return new JsonResult(new { ok = false, msg = "Job not found." }) { StatusCode = StatusCodes.Status404NotFound };

            var customerId = Convert.ToString(jobDt.Rows[0]["user_id"]) ?? "0";
            DataTable userDt = App.Database.DataAccessManager.GetDataTable(App.Database.Schema.Entities.Name, App.Database.Schema.Entities.Tables.Users, $"id = {customerId}");
            var customerName = userDt.Rows.Count > 0 ? userDt.Rows[0]["fullname"] as string ?? "" : "";

            StringBuilder sb = new();

            sb.Append("<div class=\"card\">")
              .Append("<table style=\"width: 100%;\"><tr>")
              .Append("<td style=\"width:90%;\"><h5 id=\"driverjobmessagesmodaltitle\">Send Message</h5></td>")
              .Append("<td style=\"width:10%;\"><button id=\"MJOBMSGCLS\" class=\"btn btn-close\" type=\"button\" aria-label=\"Close\" onclick=\"CloseDJContent();\">×</button></td>")
              .Append("</tr></table>")
              .Append("<div style=\"margin-top:0.5rem;\">")
              .Append("<label for=\"messageText\">To: ").Append(System.Net.WebUtility.HtmlEncode(customerName)).Append("</label>")


              .Append("<textarea id=\"messageText\" class=\"form-control\" rows=\"4\" placeholder=\"Type your message...\" maxlength=\"128\"></textarea>")
              .Append("<small id=\"messageCharCount\" style=\"float:right; font-size:0.85rem;\" >0 / 128</small>")
              .Append("</div>")
              .Append("<div style=\"margin-top:0.5rem;text-align:right;\">")
              .Append("<button type=\"button\" class=\"btn btn-primary\" onclick=\"SendQuickMessage(").Append(req.JobId).Append(");\">Send</button>")
              .Append("</div>")
              .Append("</div>");

            Response.ContentType = "text/html";
            return Content(sb.ToString());
        }
    }
}