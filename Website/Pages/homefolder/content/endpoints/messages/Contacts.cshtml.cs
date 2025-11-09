using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.messages
{
    [IgnoreAntiforgeryToken]
    public class ContactsModel : PageModel
    {
        private const string SchemaOps = "ops";
        private const string SchemaEnt = "ent";

        public IActionResult OnGet()
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" })
                { StatusCode = StatusCodes.Status401Unauthorized };

            try
            {
                long userId = User.Id();

                // Try to find the real Support user ID from ent.users (role-based)
                string sqlSupport = @"
                    SELECT u.id
                    FROM ent.users u
                    INNER JOIN ent.user_roles ur ON ur.user_id = u.id
                    INNER JOIN ent.roles r ON r.id = ur.role_id
                    WHERE LOWER(r.name) = 'support'
                    LIMIT 1;";
                object? supObj = App.Database.DataAccessManager.ExecuteScalar(SchemaEnt, sqlSupport, []);
                long? supportId = supObj == null || supObj == DBNull.Value ? null : Convert.ToInt64(supObj);

                // Start building contact list
                var contacts = new List<object>();

                if (supportId.HasValue)
                {
                    contacts.Add(new
                    {
                        id = supportId.Value,
                        name = "Support",
                        icon = "fa-headset"
                    });
                }

                // Gather contacts linked to jobs for this user
                string sql = @$"
                    SELECT DISTINCT
                        CASE
                            WHEN j.user_id = {userId} AND pa.participant_id <> {userId} THEN pa.participant_id
                            WHEN pa.participant_id = {userId} AND j.user_id <> {userId} THEN j.user_id
                            ELSE NULL
                        END AS contact_id
                    FROM ops.job j
                    LEFT JOIN ops.participant_assignment pa ON pa.job_id = j.id
                    WHERE ({userId} IN (j.user_id, pa.participant_id))
                      AND pa.participant_id IS NOT NULL
                      AND j.allocation_status_id IS NOT NULL;";

                DataTable dt = App.Database.DataAccessManager.GetDataTable(SchemaOps, sql, []);

                var validIds = dt.AsEnumerable()
                    .Where(r => r["contact_id"] != DBNull.Value)
                    .Select(r => Convert.ToInt64(r["contact_id"]))
                    .Distinct()
                    .ToList();

                if (validIds.Count > 0)
                {
                    string idList = string.Join(",", validIds);
                    string sqlNames = $"SELECT id, fullname FROM ent.users WHERE id IN ({idList});";
                    DataTable names = App.Database.DataAccessManager.GetDataTable(SchemaEnt, sqlNames, []);

                    foreach (DataRow n in names.Rows)
                    {
                        contacts.Add(new
                        {
                            id = Convert.ToInt64(n["id"]),
                            name = n["fullname"]?.ToString() ?? "Unknown",
                            icon = "fa-user"
                        });
                    }
                }

                return new JsonResult(new { ok = true, contacts });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message });
            }
        }
    }
}