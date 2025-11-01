using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Website.App.Database;
using Website.App.Security;

namespace Website.Pages.Endpoints
{
    [IgnoreAntiforgeryToken]
    public class ProvidersByTypeModel : PageModel
    {
        public class SetDefaultInput
        {
            public int TypeId { get; set; }
        }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost([FromBody] SetDefaultInput? input)
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" })
                { StatusCode = StatusCodes.Status401Unauthorized };

            if (input == null || input.TypeId <= 0)
                return BadRequest(new { ok = false, msg = "Invalid payload." });

            try
            {
                using DataTable dt = DataAccessManager.GetDataTable(
                    Schema.Config.Name,
                    Schema.Config.Tables.PaymentTypeProvider,
                    $"type_id = {input.TypeId} AND is_enabled = true"
                );

                if (dt.Rows.Count == 0)
                {
                    return new JsonResult(new
                    {
                        ok = false,
                        providers = Array.Empty<object>()
                    });
                }

                var providers = dt.AsEnumerable().Select(r => new
                {
                    id = Convert.ToInt64(r["id"]),
                    code = Convert.ToString(r["code"]),
                    name = Convert.ToString(r["name"]),
                    description = Convert.ToString(r["description"]),
                    websiteUrl = r.Table.Columns.Contains("website_url") && r["website_url"] != DBNull.Value
                                 ? Convert.ToString(r["website_url"])
                                 : null,
                    supportsVault = r.Table.Columns.Contains("supports_vault") && r["supports_vault"] != DBNull.Value
                                    && Convert.ToBoolean(r["supports_vault"])
                }).ToList();

                return new JsonResult(new
                {
                    ok = true,
                    providers
                });
            }
            catch (Exception ex)
            {
                App.Bootstrap.Logger?.Add(
                    $"ProvidersByType: {ex.Message}",
                    App.Helper.Logger.LogLevel.Error
                );

                return new JsonResult(new
                {
                    ok = false,
                    providers = Array.Empty<object>(),
                    error = "Internal server error"
                });
            }
        }
    }
}