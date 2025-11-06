using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Website.App.Security
{
    internal static class UserExtensions
    {
        // READS: ClaimsPrincipal-only (no HttpContext here)
        internal static bool IsAuthorised(this ClaimsPrincipal user)
            => user?.Identity?.IsAuthenticated ?? false;

        internal static int Id(this ClaimsPrincipal user)
            => int.TryParse(user?.FindFirst("UserId")?.Value, out var id) ? id : 0;

        // WRITES: HttpContext is required only where we mutate auth state
        internal static void SetId(this ClaimsPrincipal user, HttpContext http, int value)
            => UpdateClaim(user, http, "UserId", value.ToString());

        internal static void SignOut(this ClaimsPrincipal user, HttpContext http)
        {
            if (user?.Identity?.IsAuthenticated != true) return;
            http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
                .GetAwaiter().GetResult();
        }

        internal static void SignInUser(this HttpContext http, int userId)
        {
            var claims = new List<Claim>
            {
                new("UserId", userId.ToString()),
                new(ClaimTypes.Name, "Placeholder")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal)
                .GetAwaiter().GetResult();
        }

        private static void UpdateClaim(ClaimsPrincipal user, HttpContext http, string type, string value)
        {
            if (user.Identity is not ClaimsIdentity identity) return;

            var existing = identity.FindFirst(type);
            if (existing != null) identity.RemoveClaim(existing);
            identity.AddClaim(new Claim(type, value));

            http.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            ).GetAwaiter().GetResult();
        }
    }
}