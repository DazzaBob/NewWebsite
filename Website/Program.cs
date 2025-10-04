using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.StaticFiles;
using Website.Pages.User.Address.EndPoints;

var builder = WebApplication.CreateBuilder(args);

// Make sure the databases are created; and there are tables in the databases.
Website.App.Database.Shared.EnsureDatabase(Website.App.Database.Schema.Locations.Database);
Website.App.Database.Shared.EnsureDatabase(Website.App.Database.Schema.Entities.Database);
Website.App.Database.Shared.EnsureDatabase(Website.App.Database.Schema.Operations.Database);
Website.App.Database.Shared.EnsureDatabase(Website.App.Database.Schema.LiveOps.Database);

// Bootstrap
Website.App.Settings.Load();
Website.App.Bootstrap.Logger?.Add("Application starting...");

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
builder.Services.AddScoped<IAddressService, AddressService>();

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

app.MapRazorPages();
app.Run();