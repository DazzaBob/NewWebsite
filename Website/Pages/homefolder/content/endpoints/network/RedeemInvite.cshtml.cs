using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Database;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.network
{
    [IgnoreAntiforgeryToken]
    public class RedeemInviteModel : PageModel
    {
        public class RedeemRequest
        {
            public string Invite_code { get; set; } = string.Empty;
        }
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] RedeemRequest body)
        {
            if (!User.IsAuthorised()) return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            if (User.Id() == 0) return new JsonResult(new { ok = false, msg = "Bad Request" }) { StatusCode = StatusCodes.Status400BadRequest };
            if (body == null || string.IsNullOrWhiteSpace(body.Invite_code)) return new JsonResult(new { ok = false, msg = "Bad Request" }) { StatusCode = StatusCodes.Status400BadRequest };
            try
            {
                int currentUserId = User.Id();
                string code = body.Invite_code.Trim().ToUpperInvariant();

                using DataTable dt = App.Database.DataAccessManager.GetDataTable(Schema.Entities.Name, Schema.Entities.Tables.UserInvite, $"invite_code = {App.Database.Shared.Sanitize(code, true)}");
                if (dt.Rows.Count == 0) return new JsonResult(new { ok = false, msg = "Invalid invite code." });

                var row = dt.Rows[0];
                int fromUserId = Convert.ToInt32(row["from_user_id"]);
                bool used = Convert.ToBoolean(row["used"]);
                double expires = Convert.ToDouble(row["expires"]);

                if (used) return new JsonResult(new { ok = false, msg = "This invite has already been used." });
                if (DateTime.UtcNow.ToOADate() > expires) return new JsonResult(new { ok = false, msg = "This invite has expired." });
                if (fromUserId == currentUserId) return new JsonResult(new { ok = false, msg = "You cannot redeem your own invite." });

                _ = DataAccessManager.Update(Schema.Entities.Name, Schema.Entities.Tables.UserInvite, $"to_user_id = {currentUserId}, used = true", $"invite_code = {App.Database.Shared.Sanitize(code, true)}");

                double nowOa = DateTime.UtcNow.ToOADate();

                _ = DataAccessManager.Insert(Schema.Entities.Name, Schema.Entities.Tables.UserLink, "user_id, linked_user_id, linked_on, invite_code", $"{fromUserId}, {currentUserId}, {nowOa}, {App.Database.Shared.Sanitize(code, true)}");
                _ = DataAccessManager.Insert(Schema.Entities.Name, Schema.Entities.Tables.UserLink, "user_id, linked_user_id, linked_on, invite_code", $"{currentUserId}, {fromUserId}, {nowOa}, {App.Database.Shared.Sanitize(code, true)}");

                return new JsonResult(new
                {
                    ok = true,
                    msg = "Invite accepted. You are now connected."
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message });
            }
        }
    }
}
