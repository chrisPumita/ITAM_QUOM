using ITAM.WebApp;
using ITAM.WebApp.Middleware;
using ITAM.WebApp.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using Serilog.Events;

try
{
    var builder = WebApplication.CreateBuilder(args);

    var logsDir = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "App_Data", "logs");
    Directory.CreateDirectory(logsDir);

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: Path.Combine(logsDir, "log-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    builder.Host.UseSerilog();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddControllersWithViews();
    builder.Services.AddApiConnect(builder.Configuration);
    builder.Services.AddScoped<IWebAuthSession, WebAuthSession>();
    builder.Services.AddSingleton<WebAppMetaData>();

    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/Login";
            options.SlidingExpiration = true;
            options.Cookie.Name = "ITAM.WebApp.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;

            // APIs locales del WebApp (ej. ping) → 401 JSON; páginas → redirect login.
            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    var app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // Sin AllowAnonymous los estáticos quedan atrapados por FallbackPolicy (CSS/JS del login).
    app.MapStaticAssets().AllowAnonymous();
    app.MapControllers();
    app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    Log.Information("ITAM.WebApp iniciando. Logs en {LogsDir}", logsDir);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ITAM.WebApp terminó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
