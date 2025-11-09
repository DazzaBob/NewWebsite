using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using Website.App.Security;

namespace Website.Pages.homefolder.content.endpoints
{
    [IgnoreAntiforgeryToken]
    public class WelcomeBackModel : PageModel
    {
        public IActionResult OnGet() => NotFound();
        public IActionResult OnPost()
        {
            if (!User.IsAuthorised())
                return new JsonResult(new { ok = false, html = "" });

            int hour = DateTime.Now.Hour;
            string greeting = hour switch
            {
                >= 0 and < 12 => "Good morning",
                >= 12 and < 17 => "Good afternoon",
                _ => "Good evening"
            };

            var vuser = App.Database.DataAccessManager.ExecuteScalar(App.Database.Schema.Entities.Name, $"select fullname from {App.Database.Schema.Entities.Tables.Users} where id={User.Id()}", []);
            if (vuser == null)
                return new JsonResult(new { ok = false, html = "" });

            string user = vuser.ToString()?.Split(' ').FirstOrDefault() ?? "there";

            string[] variants = {
                $"{greeting} {user}, welcome back!",
                $"{greeting}, {user}! Glad to see you again.",
                $"Welcome back, {user}! {greeting.ToLower()} to you.",
                $"{greeting} {user}!&nbsp;&nbsp;Ready to get started?"
            };

            var random = new Random();
            string message = variants[random.Next(variants.Length)];

            string modalId = Guid.NewGuid().ToString("N"); // unique modal identifier
            StringBuilder sb = new();
            sb.Append("<style>")
              .Append(".fa-hand { ")
              .Append("font-size:1.8rem; color:var(--primary, #e74c3c); transform-origin:70% 70%; display:inline-block; ")
              .Append("animation:handWave 1.8s ease-in-out infinite; vertical-align:middle; margin-right:0.3rem; ")
              .Append("} ")

              .Append("@keyframes handWave { ")
              .Append("0%,100% { transform:rotate(0deg); } ")
              .Append("20% { transform:rotate(15deg); } ")
              .Append("40% { transform:rotate(-10deg); } ")
              .Append("60% { transform:rotate(12deg); } ")
              .Append("80% { transform:rotate(-6deg); } ")
              .Append("} ")

              .Append(".welcome-content h2 { ")
              .Append("display:inline-block; font-weight:600; line-height:1.3; ")
              .Append("} ")

              .Append(".welcome-actions { margin-top:1.5rem; } ")

              .Append("@media(max-width:480px){ ")
              .Append(".fa-hand { font-size:1.5rem; margin-right:0.2rem; } ")
              .Append("}")
              .Append("</style>");

            sb.Append($"<div class=\"custom-modal\" data-modal-type=\"welcomeback\" data-modal-id=\"{modalId}\">")
              .Append("<div class=\"custom-modal-content\" role=\"dialog\" aria-modal=\"true\" aria-labelledby=\"welcomebackmodaltitle_@modalId\">")
              .Append("<div class='welcome-content' style='text-align:center;'>")
              .Append($"<h2><i class='fa-solid fa-hand' aria-hidden='true'></i>{message}</h2>")
              .Append("<p>Click OK when you're ready.</p>")
              .Append("<div class='welcome-actions'>")
              .Append("<button id='welcomeOk' class='btn btn-primary'>OK</button>")
              .Append("</div>")
              .Append("</div></div></div>");


            string html = sb.ToString();

            return new JsonResult(new { ok = true, html });
        }
    }
}
