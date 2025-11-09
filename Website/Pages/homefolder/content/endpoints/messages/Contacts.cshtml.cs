using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.messages
{
    [IgnoreAntiforgeryToken]
    public class ContactsModel : PageModel
    {
        private const string SchemaEnt = "ent";

        public IActionResult OnGet()
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" })
                { StatusCode = StatusCodes.Status401Unauthorized };

            try
            {
                long userId = User.Id();
                long supportId = App.Database.Views.Messages.Contacts.SupportId();

                // Start building contact list
                var contacts = new List<object>();

                if (supportId > 0)
                {
                    contacts.Add(new { id = supportId, name = "Support", icon = "fa-headset" });
                }

                // Gather contacts linked to jobs for this user
                using DataTable dt = App.Database.Operations.Tables.GetLinkedJobsForUser(userId);
                var validIds = dt.AsEnumerable()
                    .Where(r => r["contact_id"] != DBNull.Value)
                    .Select(r => Convert.ToInt64(r["contact_id"]))
                    .Distinct()
                    .ToList();

                if (validIds.Count > 0)
                {
                    string idList = string.Join(",", validIds);
                    using DataTable names = App.Database.DataAccessManager.GetDataTable(App.Database.Schema.Entities.Name, App.Database.Schema.Entities.Tables.Users, $"id IN({idList})");

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