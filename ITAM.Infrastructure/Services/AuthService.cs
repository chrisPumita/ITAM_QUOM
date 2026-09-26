using System.Security.Claims;
using ITAM.Domain.Interfaces.Services;
using ITAM.Infrastructure.Identity;
using ITAM.Infrastructure.Persistence;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Auth;
using ITAM.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IAuthService"/> con ASP.NET Identity.
/// No genera JWT; solo valida credenciales y resuelve rol/claims.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public AuthService(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    /// <inheritdoc />
    public async Task<Result<LoginResultDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return FailLogin("Credenciales inválidas.", "Email o contraseña incorrectos");

        if (!user.IsActive)
            return FailLogin("La cuenta está inactiva.", "Usuario inactivo");

        var passwordOk = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordOk)
            return FailLogin("Credenciales inválidas.", "Email o contraseña incorrectos");

        var role = await ResolveRoleAsync(user);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return new Result<LoginResultDto>
        {
            IsSuccess = true,
            Message = "Inicio de sesión exitoso.",
            Data = new LoginResultDto
            {
                IdentityUserId = user.Id,
                Email = user.Email ?? dto.Email,
                DisplayName = user.DisplayName,
                Role = role
            }
        };
    }

    /// <inheritdoc />
    public async Task<Result<List<IdentityUserListDto>>> ListUsersAsync(bool? onlyActive, bool? onlyUnlinked)
    {
        var query = _userManager.Users.AsNoTracking();

        if (onlyActive == true)
            query = query.Where(u => u.IsActive);

        if (onlyUnlinked == true)
        {
            var linkedIds = _db.Employees
                .AsNoTracking()
                .Where(e => e.IdentityUserId != null)
                .Select(e => e.IdentityUserId!.Value);

            query = query.Where(u => !linkedIds.Contains(u.Id));
        }

        var users = await query
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.Email)
            .ToListAsync();

        var list = new List<IdentityUserListDto>(users.Count);
        foreach (var user in users)
        {
            var role = await ResolveRoleAsync(user);
            list.Add(new IdentityUserListDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                IsActive = user.IsActive,
                Role = role?.ToString()
            });
        }

        return new Result<List<IdentityUserListDto>>
        {
            IsSuccess = true,
            Message = "Usuarios Identity obtenidos.",
            Data = list
        };
    }

    private async Task<AppRole?> ResolveRoleAsync(ApplicationUser user)
    {
        var claims = await _userManager.GetClaimsAsync(user);
        var roleClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        if (string.IsNullOrWhiteSpace(roleClaim))
        {
            var roles = await _userManager.GetRolesAsync(user);
            roleClaim = roles.FirstOrDefault();
        }

        return Enum.TryParse<AppRole>(roleClaim, out var parsed) ? parsed : null;
    }

    private static Result<LoginResultDto> FailLogin(string message, string error) => new()
    {
        IsSuccess = false,
        Message = message,
        Error = error
    };
}
