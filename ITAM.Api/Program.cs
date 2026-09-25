using System.Text;
using ITAM.Api;
using ITAM.Infrastructure;
using ITAM.Shared.Services.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

/*
 * ═══════════════════════════════════════════════════════════════════════════
 *  MIGRACIONES EF CORE (Identity) — ejecutar MANUALMENTE desde la raíz del repo
 * ═══════════════════════════════════════════════════════════════════════════
 *
 *  Requisitos: herramienta dotnet-ef
 *    dotnet tool install --global dotnet-ef
 *    (o actualizar) dotnet tool update --global dotnet-ef
 *
 *  Crear una migración nueva (después de cambiar el DbContext / ApplicationUser):
 *    dotnet ef migrations add <NombreMigracion> ^
 *      --project ITAM.Infrastructure\ITAM.Infrastructure.csproj ^
 *      --startup-project ITAM.Api\ITAM.Api.csproj ^
 *      --output-dir Persistence/Migrations
 *
 *  Aplicar migraciones pendientes a la BD (DefaultConnection):
 *    dotnet ef database update ^
 *      --project ITAM.Infrastructure\ITAM.Infrastructure.csproj ^
 *      --startup-project ITAM.Api\ITAM.Api.csproj
 *
 *  Ver migraciones / última aplicada:
 *    dotnet ef migrations list ^
 *      --project ITAM.Infrastructure\ITAM.Infrastructure.csproj ^
 *      --startup-project ITAM.Api\ITAM.Api.csproj
 *
 *  Revertir a una migración anterior (cuidado en prod):
 *    dotnet ef database update <NombreMigracionAnterior> ^
 *      --project ITAM.Infrastructure\ITAM.Infrastructure.csproj ^
 *      --startup-project ITAM.Api\ITAM.Api.csproj
 *
 *  Nota: el arranque NO llama Database.Migrate(). Primero update, luego F5
 *  (SeedIdentityAsync crea roles/usuarios de prueba si faltan).
 *
 *  Ambientes / connection string:
 *    Development → appsettings.Development.json → localhost\SQLEXPRESS / ITAM_QUOM
 *    Production  → connection string en MonsterASP (panel o env), NO hardcodear password en git
 *    `dotnet ef` / PMC Update-Database usan el ambiente del startup (casi siempre Development = LOCAL).
 *    Por eso "already up to date" en local es normal: InitialIdentity ya está aplicada ahí.
 *
 *  Migrar a MonsterASP (pasar connection string explícita; no uses solo Update-Database):
 *
 *  Package Manager Console:
 *    Update-Database -Connection "Server=TU_HOST.databaseasp.net;Database=TU_DB;User Id=TU_USER;Password=TU_PASS;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;"
 *
 *  CLI (recomendado):
 *    dotnet ef database update ^
 *      --project ITAM.Infrastructure\ITAM.Infrastructure.csproj ^
 *      --startup-project ITAM.Api\ITAM.Api.csproj ^
 *      --connection "Server=...;Database=...;User Id=...;Password=...;Encrypt=False;TrustServerCertificate=True;"
 *
 *  Ver a qué BD va a conectar (sin aplicar):
 *    dotnet ef dbcontext info --project ITAM.Infrastructure --startup-project ITAM.Api
 * ═══════════════════════════════════════════════════════════════════════════
 */

var builder = WebApplication.CreateBuilder(args);

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

// Seed de roles/usuarios de prueba (requiere migraciones ya aplicadas).
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

app.Run();
