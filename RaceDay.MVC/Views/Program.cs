using System.Globalization;
using RaceDay.MVC.Services;

// Number inputs always send "21.1" (a dot). Using the invariant culture stops machines set to South African
// regional settings (which expect a comma) from rejecting distances and fees.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// ---- Session management ----
// After login the user's token, role, ID and name are kept in the server-side session.
// The browser only holds an HttpOnly cookie with the session ID.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ---- The Part 2 API: the ONLY place this site gets data from (no database code lives in this project) ----
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
                 ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured.");
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<ApiClient>();

var app = builder.Build();

// Friendly error pages instead of raw error messages.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
