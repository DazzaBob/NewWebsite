using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Linq;
using Website.App.Database;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.network
{
    [IgnoreAntiforgeryToken]
    public class ListModel : PageModel
    {
        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost()
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            if (User.Id() == 0)
                return new JsonResult(new { ok = false, msg = "Bad Request" }) { StatusCode = StatusCodes.Status400BadRequest };

            try
            {
                int userId = User.Id();

                string sql = $@"
                    SELECT
                        ul.linked_user_id AS user_id,
                        u.fullname        AS name,
                        STRING_AGG(r.name, ', ' ORDER BY r.id) AS roles,
                        COALESCE(STRING_AGG(r.iconclass, '||' ORDER BY r.id), '') AS role_icons,
                        ul.linked_on      AS linked_on_oadate,
                        ul.invite_code
                    FROM {Schema.Entities.Tables.UserLink} ul
                    JOIN {Schema.Entities.Tables.Users} u
                      ON u.id = ul.linked_user_id
                    LEFT JOIN {Schema.Entities.Tables.UserRoles} ur
                      ON ur.user_id = u.id AND ur.isactive = TRUE
                    LEFT JOIN {Schema.Entities.Tables.Roles} r
                      ON r.id = ur.role_id
                    WHERE ul.user_id = {userId}
                    GROUP BY ul.linked_user_id, u.fullname, ul.linked_on, ul.invite_code
                    ORDER BY u.fullname;";

                using DataTable DT = DataAccessManager.GetDataTable(Schema.Entities.Name, sql, []);
                if (DT.Rows.Count == 0)
                    return new JsonResult(new { ok = true, msg = "No linked users found.", html = string.Empty });

                var html = new System.Text.StringBuilder();
                foreach (DataRow row in DT.Rows)
                {
                    int uid = Convert.ToInt32(row["user_id"]);
                    string name = row["name"]?.ToString() ?? string.Empty;
                    string roles = row["roles"] == DBNull.Value ? string.Empty : row["roles"]!.ToString()!;
                    string linked = OADateToDate(row["linked_on_oadate"] == DBNull.Value ? 0 : Convert.ToDouble(row["linked_on_oadate"]));
                    string initials = GetInitials(name);

                    string iconList = row["role_icons"] == DBNull.Value ? string.Empty : row["role_icons"]!.ToString()!;
                    var icons = (iconList ?? string.Empty).Split("||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    string iconsHtml = string.Concat(
                        icons.Select(ic => $"<i class='{Website.App.Database.Shared.Sanitize(ic, false, true)}' aria-hidden='true' style='margin-right:.35rem;'></i>")
                    );

                    html.Append(Website.App.StringBuilders.Pages.Network.BuildNetworkRow(uid, name, roles, linked, initials, iconsHtml));
                }

                return new JsonResult(new { ok = true, html = html.ToString() });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message });
            }
        }

        private static string GetInitials(string name)
        {
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0][0].ToString().ToUpperInvariant();
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }

        private static string OADateToDate(double oa)
        {
            try
            {
                if (oa <= 0) return string.Empty;
                return DateTime.FromOADate(oa).ToString("yyyy-MM-dd");
            }
            catch { return string.Empty; }
        }
    }
}
