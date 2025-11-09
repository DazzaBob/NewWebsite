using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Website.App.Database;
using Website.App.Security;

namespace Website.Pages.Endpoints
{
    [IgnoreAntiforgeryToken]
    public class AllocateModel : PageModel
    {
        public class Input
        {
            public long JobId { get; set; }

        }
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost([FromBody] Input input)
        {
            if (!User.IsAuthorised()) return new JsonResult(new { ok = false, msg = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            if (input == null || input.JobId <= 0)
                return new JsonResult(new { ok = false, msg = "Invalid payment reference." })
                { StatusCode = StatusCodes.Status400BadRequest };

            try
            {
                // 1️ Verify that the job exists and belongs to the current user
                string sqlCheck = $@"SELECT j.id FROM ops.job j JOIN ent.users u ON u.id = j.user_id WHERE j.id = {input.JobId} AND u.id = {User.Id()} LIMIT 1;";
                object? jobCheck = DataAccessManager.ExecuteScalar("pub", sqlCheck, []); // Use the pub schema, its a cross table operation.
                if (jobCheck == null || jobCheck == DBNull.Value)
                    return new JsonResult(new { ok = false, msg = "Job not found or access denied." })
                    { StatusCode = StatusCodes.Status404NotFound };

                // 2️ Update job status to ALLOCATING
                string sqlUpdate = $@"UPDATE ops.job SET job_status_id = (SELECT id FROM cfg.job_status WHERE code='CREATED' LIMIT 1), updated_on_oad = {DateTime.UtcNow.ToOADate()} WHERE id = {input.JobId};";
                DataAccessManager.ExecuteNonQuery("pub", sqlUpdate, []); // Use the pub schema, its a cross table operation.

                // 3️ Record event for audit / status tracking
                string sqlEvent = $@"INSERT INTO ops.job_event (job_id, event_code_id, payload)
                VALUES ({input.JobId}, (SELECT id FROM cfg.job_event_code WHERE code='CREATED' LIMIT 1),'{{}}')";
                DataAccessManager.ExecuteNonQuery("pub", sqlEvent, []); // Use the pub schema, its a cross table operation.

                // 4️ Kick off background allocation logic asynchronously
                Task.Run(() =>
                {
                    try
                    {
                        // AllocatorService.Start(input.JobId); Stubbed out for now
                    }
                    catch (Exception ex)
                    {
                        App.Bootstrap.Logger?.Add($"Allocator failed for job {input.JobId}" + ex.Message, App.Helper.Logger.LogLevel.Error);
                    }
                });

                // 5️ Return immediate response
                return new JsonResult(new { ok = true, msg = "Allocation started.", job_id = input.JobId });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, msg = ex.Message }) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}
