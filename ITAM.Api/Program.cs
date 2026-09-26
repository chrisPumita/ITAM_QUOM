using System.Text;
using System.Threading.RateLimiting;
using ITAM.Api;
using ITAM.Api.Middleware;
using ITAM.Infrastructure;
using ITAM.Shared.Services.Cors;
using ITAM.Shared.Services.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;

/*
 * ═══════════════════════════════════════════════════════════════════════════
 *  MIGRACIONES EF CORE — ejecutar MANUALMENTE desde la raíz del repo
 * ═══════════════════════════════════════════════════════════════════════════
 *
 *  Nueva migración (negocio + Identity):
 *    dotnet ef migrations add <NombreMigracion> ^
 *      --project ITAM.Infrastructure\ITAM.Infrastructure.csproj ^
 *      --startup-project ITAM.Api\ITAM.Api.csproj ^
 *      --output-dir Persistence/Migrations
 *
 *  Aplicar:
 *    dotnet ef database update ^
 *      --project ITAM.Infrastructure\ITAM.Infrastructure.csproj ^
 *      --startup-project ITAM.Api\ITAM.Api.csproj
 *
 *  MonsterASP: usar --connection "..." (no solo Update-Database en Development).
 *
 *  Logs Serilog: {ContentRoot}/wwwroot/App_Data/logs/log-.log  (portable al publish)
 * ═══════════════════════════════════════════════════════════════════════════
 */

try
{
    var builder = WebApplication.CreateBuilder(args);

    var logsDir = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "App_Data", "logs");
    Directory.CreateDirectory(logsDir);

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: Path.Combine(logsDir, "log-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // MonsterASP / reverse proxy: no conocemos IPs fijas del edge.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "ITAM QUOM API",
            Version = "v1",
            Description = "API de gestión y resguardo de activos TI. Autenticación: Authorize → Bearer {JWT}."
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header. Ejemplo: Bearer {token}"
        });

        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
    });

    builder.Services.AddSingleton<ApiMetaData>();
    builder.Services.AddInfrastructure(builder.Configuration);

    var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
        ?? throw new InvalidOperationException("JwtSettings no configurado.");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

    builder.Services.AddAuthorization();

    var cors = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>()
               ?? new CorsSettings();
    var corsOrigins = cors.AllowedOrigins
        .Where(o => !string.IsNullOrWhiteSpace(o))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (corsOrigins.Length > 0)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("ItamCors", policy =>
                policy.WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });
    }

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                code = StatusCodes.Status429TooManyRequests,
                message = "Demasiadas solicitudes. Intente más tarde.",
                error = "RateLimit"
            }, token);
        };

        options.AddPolicy("login", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 300,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    });

    var app = builder.Build();

    app.UseForwardedHeaders();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    await app.Services.SeedIdentityAsync();

    // Swagger habilitado también en Production (documentación para clientes / MonsterASP).
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Ruta relativa: funciona detrás del reverse proxy de MonsterASP.
        options.SwaggerEndpoint("v1/swagger.json", "ITAM QUOM API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "ITAM QUOM API — Swagger";
        options.DisplayRequestDuration();
    });

    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

    app.UseHttpsRedirection();
    if (corsOrigins.Length > 0)
        app.UseCors("ItamCors");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("ITAM.Api iniciando. Logs en {LogsDir}", logsDir);
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException and not OperationCanceledException)
{
    // HostAbortedException es normal: `dotnet ef` / PMC abortan el host tras obtener el DbContext.
    Log.Fatal(ex, "ITAM.Api terminó inesperadamente");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
