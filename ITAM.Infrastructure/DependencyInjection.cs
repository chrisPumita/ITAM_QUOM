using System.Security.Claims;
using ITAM.Domain.Interfaces.Repositories.Catalog;
using ITAM.Domain.Interfaces.Repositories.Company;
using ITAM.Domain.Interfaces.Services;
using ITAM.Domain.Interfaces.Services.Catalog;
using ITAM.Domain.Interfaces.Services.Company;
using ITAM.Infrastructure.Identity;
using ITAM.Infrastructure.Persistence;
using ITAM.Infrastructure.Repositories.Catalog;
using ITAM.Infrastructure.Repositories.Company;
using ITAM.Infrastructure.Services;
using ITAM.Infrastructure.Services.Catalog;
using ITAM.Infrastructure.Services.Company;
using ITAM.Shared.Enums;
using ITAM.Shared.Services.Identity;
using ITAM.Shared.Services.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ITAM.Infrastructure;

/// <summary>
/// Registro de DI de Infrastructure: SQL Server, Identity, JwtSettings, servicios.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Agrega DbContext, ASP.NET Identity (<see cref="ApplicationUser"/>) y <see cref="IAuthService"/>.
    /// No aplica migraciones: ejecutarlas manualmente (ver comentarios en <c>ITAM.Api/Program.cs</c>).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<ITAM.Shared.Services.Mail.SmtpSettings>(
            configuration.GetSection(ITAM.Shared.Services.Mail.SmtpSettings.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no configurada.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IModelService, ModelService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IEmployeeService, EmployeeService>();

        return services;
    }

    /// <summary>
    /// Seed de roles y usuarios de prueba. Requiere que las migraciones ya estén aplicadas.
    /// </summary>
    /// <remarks>
    /// Usuarios:
    /// <list type="bullet">
    /// <item><c>admin@itam.local</c> / <c>Admin123!</c> → Administrador</item>
    /// <item><c>operador@itam.local</c> / <c>Operador123!</c> → Operador</item>
    /// </list>
    /// Cada usuario recibe Identity Role + claim <c>ClaimTypes.Role</c> (para el JWT).
    /// </remarks>
    public static async Task SeedIdentityAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }

        await EnsureUserAsync(
            userManager,
            email: "admin@itam.local",
            password: "Admin123!",
            displayName: "Administrador ITAM",
            role: AppRoles.Administrador);

        await EnsureUserAsync(
            userManager,
            email: "operador@itam.local",
            password: "Operador123!",
            displayName: "Operador ITAM",
            role: AppRoles.Operador);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string displayName,
        string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var create = await userManager.CreateAsync(user, password);
        if (!create.Succeeded)
            throw new InvalidOperationException(
                $"No se pudo crear el usuario seed {email}: {string.Join(", ", create.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(user, role);
        await userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, role));
    }
}
