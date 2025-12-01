using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using Website.App.Database;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints.recruiting
{
    [IgnoreAntiforgeryToken]
    public class UploadDriverDocumentModel : PageModel
    {
        [BindProperty(Name = "file")]
        public IFormFile? UploadedFile { get; set; }

        [BindProperty]
        public string? DocumentType { get; set; }

        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost()
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = 401 };

            if (string.IsNullOrWhiteSpace(DocumentType))
                return new JsonResult(new { ok = false, msg = "Missing document type." }) { StatusCode = 400 };

            if (UploadedFile == null || UploadedFile.Length == 0)
                return new JsonResult(new { ok = false, msg = "No file uploaded." }) { StatusCode = 400 };

            try
            {
                int driverId = User.Id();
                double nowOad = DateTime.UtcNow.ToOADate();

                byte[] buffer;
                using (var ms = new MemoryStream())
                {
                    UploadedFile.CopyTo(ms);
                    buffer = ms.ToArray();
                }

                string hex = BytesToHex(buffer);

                string fields =
                    "driver_id, driver_document_type_code, original_filename, content_type, content_length, content_data, " +
                    "expiry_oad, created_oad, updated_oad, review_status_code, review_reason, reviewed_by_user_id, reviewed_oad";

                string values = string.Join(", ",
                    driverId.ToString(CultureInfo.InvariantCulture),
                    App.Database.Shared.Sanitize(DocumentType.Trim(), true, true),
                    App.Database.Shared.Sanitize(UploadedFile.FileName, true, true),
                    App.Database.Shared.Sanitize(UploadedFile.ContentType ?? "application/octet-stream", true, true),
                    UploadedFile.Length.ToString(CultureInfo.InvariantCulture),
                    $"decode('{hex}','hex')",
                    "NULL",
                    nowOad.ToString(CultureInfo.InvariantCulture),
                    nowOad.ToString(CultureInfo.InvariantCulture),
                    App.Database.Shared.Sanitize("pending", true, true),
                    "NULL",
                    "NULL",
                    "NULL"
                );

                _ = DataAccessManager.Insert(
                    Schema.Entities.Name,
                    Schema.Entities.Tables.DriverDocuments,
                    fields,
                    values
                );

                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                App.Bootstrap.Logger?.Add("UploadDriverDocument: " + ex.Message, App.Helper.Logger.LogLevel.Error);
                return new JsonResult(new { ok = false, msg = "Internal server error." })
                { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        private static string BytesToHex(byte[] data)
        {
            char[] c = new char[data.Length * 2];
            int b;
            for (int i = 0; i < data.Length; i++)
            {
                b = data[i] >> 4;
                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = data[i] & 0xF;
                c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }
            return new string(c);
        }
    }
}