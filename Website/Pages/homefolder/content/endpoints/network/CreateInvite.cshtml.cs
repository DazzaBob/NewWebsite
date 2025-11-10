using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Database;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.network
{
    [IgnoreAntiforgeryToken]
    public class CreateInviteModel : PageModel
    {
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost()
        {
            if (!User.IsAuthorised()) return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            if (User.Id() == 0) return new JsonResult(new { ok = false, msg = "Bad Request" }) { StatusCode = StatusCodes.Status400BadRequest };

            try
            {
                int userId = User.Id();
                string code = GenerateInviteCode(8);
                double expires = DateTime.UtcNow.AddHours(72).ToOADate();
                _ = DataAccessManager.Insert(Schema.Entities.Name, Schema.Entities.Tables.UserInvite, "invite_code, from_user_id, expires, used", $"{App.Database.Shared.Sanitize(code, true)}, {userId}, {expires}, false");

                return new JsonResult(new
                {
                    ok = true,
                    msg = "Invite code created.",
                    payload = new { invite_code = code }
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message });
            }
        }
        private static string GenerateInviteCode(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rng = new Random();
            string code;
            bool exists;
            do
            {
                // generate random code
                code = new string([.. Enumerable.Repeat(chars, length).Select(s => s[rng.Next(s.Length)])]);

                // check database for collision
                using DataTable DT = DataAccessManager.GetDataTable(Schema.Entities.Name, Schema.Entities.Tables.UserInvite, $"invite_code = {App.Database.Shared.Sanitize(code, true)}");
                exists = DT.Rows.Count > 0;

            } while (exists);

            return code;
        }
    }
}
