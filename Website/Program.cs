using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.StaticFiles;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Bootstrap
Website.App.Settings.Load();
Website.App.Bootstrap.Logger?.Add("Application starting...");

#region Asset Builder
string exePath = Path.Combine(AppContext.BaseDirectory, "AssetBuilder.exe");
string wwwrootPath = builder.Environment.WebRootPath;

if (!File.Exists(exePath))
    throw new FileNotFoundException("AssetBuilder.exe not found.", exePath);

// Call the builder
var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = exePath,
        Arguments = $"\"{wwwrootPath}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    }
};

process.Start();
string output = process.StandardOutput.ReadToEnd();
string error = process.StandardError.ReadToEnd();
process.WaitForExit();

if (process.ExitCode != 0)
    throw new Exception($"AssetBuilder failed:\n{error}");

Console.WriteLine(output);
#endregion

// Services
builder.Services.AddRazorPages().AddRazorPagesOptions(o =>
{
    // Public
    o.Conventions.AllowAnonymousToFolder("/Legal");
    o.Conventions.AllowAnonymousToPage("/Index");
    o.Conventions.AllowAnonymousToPage("/AccessDenied");
    o.Conventions.AllowAnonymousToPage("/User/Login");
    o.Conventions.AllowAnonymousToPage("/User/Recover");
    o.Conventions.AllowAnonymousToPage("/User/SignUp");
    // Protected
    o.Conventions.AuthorizePage("/User/Logout");
    o.Conventions.AuthorizeFolder("/User/Dashboard");
});

builder.Services.AddAuthentication("Cookies").AddCookie(o =>
{
    o.LoginPath = "/User/Login";
    o.LogoutPath = "/User/Logout";
    o.AccessDeniedPath = "/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
    o.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = ctx =>
        {
            var target = ctx.Request.Path + ctx.Request.QueryString;
            if (!string.IsNullOrEmpty(target) && target.StartsWith('/') && !target.StartsWith("//"))
                ctx.HttpContext.Session.SetString("redirect", target);
            ctx.Response.Redirect("/User/Login");
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings = { [".webp"] = "image/webp" }
    },
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name.Equals("all.min.css", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000,immutable");
        }
        string ext = Path.GetExtension(ctx.File.Name).ToLowerInvariant();
        if (ext == ".woff2" && ctx.File.Name.StartsWith("fa-solid", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000,immutable");
        }
    }
});
app.UseRouting();
app.Use(async (ctx, next) =>
{
    await next();

    var endpoint = ctx.GetEndpoint();
    var needsAuth = endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAuthorizeData>() != null;
    if (needsAuth)
    {
        //ctx.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
        //ctx.Response.Headers.Pragma = "no-cache";
        //ctx.Response.Headers.Expires = "0";
    }
});
app.UseSession();          // session before auth
app.UseAuthentication();   // required
app.UseAuthorization();

#if (!DEBUG)
    app.UseMiddleware<Website.App.StringBuilders.HtmlMinifyMiddleware>(); // this will minify all HTML responses
#endif
app.MapRazorPages();
app.Lifetime.ApplicationStopping.Register(() =>
{ // Graceful shutdown of DB pools
    Website.App.Database.DataAccessManager.ShutdownPools();
});
app.Run();