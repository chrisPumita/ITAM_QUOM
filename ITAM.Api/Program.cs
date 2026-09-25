using System.Text;
using ITAM.Api;
using ITAM.Api.Middleware;
using ITAM.Infrastructure;
using ITAM.Shared.Services.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "ITAM QUOM API",
            Version = "v1",
            Description = "API de gestión y resguardo de activos TI"
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

    var app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    await app.Services.SeedIdentityAsync();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ITAM QUOM API v1");
        options.RoutePrefix = "swagger";
    });

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("ITAM.Api iniciando. Logs en {LogsDir}", logsDir);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ITAM.Api terminó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
