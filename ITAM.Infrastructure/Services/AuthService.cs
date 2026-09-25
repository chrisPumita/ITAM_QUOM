using System.Security.Claims;
using ITAM.Domain.Interfaces.Services;
using ITAM.Infrastructure.Identity;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Auth;
using ITAM.Shared.Enums;
using Microsoft.AspNetCore.Identity;

namespace ITAM.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IAuthService"/> con ASP.NET Identity.
/// No genera JWT; solo valida credenciales y resuelve rol/claims.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task<Result<LoginResultDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return Fail("Credenciales inválidas.", "Email o contraseña incorrectos");

        if (!user.IsActive)
            return Fail("La cuenta está inactiva.", "Usuario inactivo");

        var passwordOk = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordOk)
            return Fail("Credenciales inválidas.", "Email o contraseña incorrectos");

        var claims = await _userManager.GetClaimsAsync(user);
        var roleClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        if (string.IsNullOrWhiteSpace(roleClaim))
        {
            var roles = await _userManager.GetRolesAsync(user);
            roleClaim = roles.FirstOrDefault();
        }

        AppRole? role = Enum.TryParse<AppRole>(roleClaim, out var parsed) ? parsed : null;

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

    private static Result<LoginResultDto> Fail(string message, string error) => new()
    {
        IsSuccess = false,
        Message = message,
        Error = error
    };
}
