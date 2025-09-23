using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
namespace Website.App.Security
{
    internal static class UserExtensions
    {
        internal static bool IsAuthorised(this ClaimsPrincipal user) => user?.Identity?.IsAuthenticated ?? false;
        internal static int Id(this ClaimsPrincipal user) => int.TryParse(user?.FindFirst("UserId")?.Value, out var id) ? id : 0;
        internal static void SetId(this ClaimsPrincipal user, HttpContext http, int value) => UpdateClaim(user, http, "UserId", value.ToString());
        internal static void SignOut(this ClaimsPrincipal user, HttpContext http)
        {
            // Use 'user' to meet standard and guard call
            if (user?.Identity?.IsAuthenticated != true) return;
            http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
                .GetAwaiter().GetResult();
        }
        internal static void SignInUser(this HttpContext http, int userId)
        {
            List<Claim> claims =
            [
                new Claim("UserId", userId.ToString()),
                new Claim(ClaimTypes.Name, "Placeholder") // optional, if needed
            ];

            ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal principal = new(identity);

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
            ).GetAwaiter().GetResult(); // no await at call site
        }
    }
}
