using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ITAM.Domain.Interfaces.Services;
using ITAM.Infrastructure.Identity;
using ITAM.Infrastructure.Persistence;
using ITAM.Infrastructure.Services.Mail;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Auth;
using ITAM.Shared.Enums;
using ITAM.Shared.Services.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ITAM.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IAuthService"/> con ASP.NET Identity.
/// No genera JWT; solo valida credenciales y resuelve rol/claims.
/// Lockout: 3 intentos fallidos → bloqueo temporal (config Identity).
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ISmtpMailSender _mail;
    private readonly SmtpSettings _smtp;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        ISmtpMailSender mail,
        IOptions<SmtpSettings> smtp)
    {
        _userManager = userManager;
        _db = db;
        _mail = mail;
        _smtp = smtp.Value;
    }

    /// <inheritdoc />
    public async Task<Result<LoginResultDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return FailLogin("Credenciales inválidas.", "Email o contraseña incorrectos");

        if (!user.IsActive)
            return FailLogin("La cuenta está inactiva.", "Usuario inactivo");

        if (await _userManager.IsLockedOutAsync(user))
            return FailLogin(
                "Cuenta bloqueada temporalmente por intentos fallidos. Intente más tarde.",
                "LockedOut");

        var passwordOk = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordOk)
        {
            await _userManager.AccessFailedAsync(user);
            if (await _userManager.IsLockedOutAsync(user))
            {
                return FailLogin(
                    "Cuenta bloqueada temporalmente tras varios intentos fallidos.",
                    "LockedOut");
            }

            return FailLogin("Credenciales inválidas.", "Email o contraseña incorrectos");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

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
                IsLockedOut = await _userManager.IsLockedOutAsync(user),
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

    /// <inheritdoc />
    public async Task<Result<bool>> UnlockUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new Result<bool>
            {
                IsSuccess = false,
                Message = "Usuario no encontrado.",
                Error = "NotFound"
            };
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        return new Result<bool>
        {
            IsSuccess = true,
            Message = "Usuario desbloqueado.",
            Data = true
        };
    }

    /// <inheritdoc />
    public async Task<Result<CreateAdminUserResultDto>> CreateAdminUserAsync(
        CreateAdminUserDto dto, CancellationToken ct = default)
    {
        var email = dto.Email.Trim();
        var displayName = dto.DisplayName.Trim();
        var role = string.IsNullOrWhiteSpace(dto.Role) ? AppRoles.Administrador : dto.Role.Trim();
        if (!AppRoles.All.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            return new Result<CreateAdminUserResultDto>
            {
                IsSuccess = false,
                Message = "Rol inválido. Use Administrador u Operador.",
                Error = "Validation"
            };
        }

        role = AppRoles.All.First(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return new Result<CreateAdminUserResultDto>
            {
                IsSuccess = false,
                Message = "Ya existe un usuario con ese correo.",
                Error = "Duplicate"
            };
        }

        var password = GenerateTemporaryPassword();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            IsActive = true,
            LockoutEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        var create = await _userManager.CreateAsync(user, password);
        if (!create.Succeeded)
        {
            return new Result<CreateAdminUserResultDto>
            {
                IsSuccess = false,
                Message = string.Join("; ", create.Errors.Select(e => e.Description)),
                Error = "Validation"
            };
        }

        await _userManager.AddToRoleAsync(user, role);
        await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, role));

        var baseUrl = !string.IsNullOrWhiteSpace(dto.PublicAppBaseUrl)
            ? dto.PublicAppBaseUrl.Trim().TrimEnd('/')
            : (_smtp.PublicAppBaseUrl ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = "https://localhost:7048";

        var loginUrl = $"{baseUrl}/Account/Login";
        var resultDto = new CreateAdminUserResultDto
        {
            Id = user.Id,
            Email = email,
            DisplayName = displayName,
            Role = role,
            TemporaryPassword = password,
            LoginUrl = loginUrl
        };

        if (dto.SendEmail)
        {
            var body = $"""
                <p>Hola <strong>{System.Net.WebUtility.HtmlEncode(displayName)}</strong>,</p>
                <p>Se creó tu cuenta en <strong>ITAM QUOM</strong> ({System.Net.WebUtility.HtmlEncode(role)}).</p>
                <p>
                  Correo: <code>{System.Net.WebUtility.HtmlEncode(email)}</code><br/>
                  Contraseña temporal: <code>{System.Net.WebUtility.HtmlEncode(password)}</code>
                </p>
                <p><a href="{System.Net.WebUtility.HtmlEncode(loginUrl)}">Iniciar sesión</a></p>
                <p>Te recomendamos cambiar la contraseña en el primer acceso.</p>
                """;
            var (sent, err) = await _mail.SendAsync(email, "ITAM QUOM — acceso de administrador", body, ct);
            resultDto.EmailSent = sent;
            resultDto.EmailError = err;
        }

        return new Result<CreateAdminUserResultDto>
        {
            IsSuccess = true,
            Message = resultDto.EmailSent
                ? "Usuario creado y correo enviado."
                : "Usuario creado. Copie la contraseña temporal (correo no enviado).",
            Data = resultDto
        };
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new Result<bool>
            {
                IsSuccess = false,
                Message = "Usuario no encontrado.",
                Error = "NotFound"
            };
        }

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            return new Result<bool>
            {
                IsSuccess = false,
                Message = string.Join("; ", result.Errors.Select(e => e.Description)),
                Error = "Validation"
            };
        }

        return new Result<bool>
        {
            IsSuccess = true,
            Message = "Contraseña actualizada.",
            Data = true
        };
    }

    /// <inheritdoc />
    public async Task<Result<AdminResetPasswordResultDto>> AdminResetPasswordAsync(
        Guid userId, AdminResetPasswordDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new Result<AdminResetPasswordResultDto>
            {
                IsSuccess = false,
                Message = "Usuario no encontrado.",
                Error = "NotFound"
            };
        }

        var password = string.IsNullOrWhiteSpace(dto.NewPassword)
            ? GenerateTemporaryPassword()
            : dto.NewPassword.Trim();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await _userManager.ResetPasswordAsync(user, token, password);
        if (!reset.Succeeded)
        {
            return new Result<AdminResetPasswordResultDto>
            {
                IsSuccess = false,
                Message = string.Join("; ", reset.Errors.Select(e => e.Description)),
                Error = "Validation"
            };
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        var baseUrl = !string.IsNullOrWhiteSpace(dto.PublicAppBaseUrl)
            ? dto.PublicAppBaseUrl.Trim().TrimEnd('/')
            : (_smtp.PublicAppBaseUrl ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = "https://localhost:7048";

        var resultDto = new AdminResetPasswordResultDto
        {
            UserId = user.Id,
            Email = user.Email ?? "",
            TemporaryPassword = password
        };

        if (dto.SendEmail && !string.IsNullOrWhiteSpace(user.Email))
        {
            var body = $"""
                <p>Hola <strong>{System.Net.WebUtility.HtmlEncode(user.DisplayName)}</strong>,</p>
                <p>Se restableció tu contraseña en <strong>ITAM QUOM</strong>.</p>
                <p>Nueva contraseña temporal: <code>{System.Net.WebUtility.HtmlEncode(password)}</code></p>
                <p><a href="{System.Net.WebUtility.HtmlEncode(baseUrl + "/Account/Login")}">Iniciar sesión</a></p>
                """;
            var (sent, err) = await _mail.SendAsync(user.Email, "ITAM QUOM — contraseña restablecida", body, ct);
            resultDto.EmailSent = sent;
            resultDto.EmailError = err;
        }

        return new Result<AdminResetPasswordResultDto>
        {
            IsSuccess = true,
            Message = resultDto.EmailSent
                ? "Contraseña restablecida y correo enviado."
                : "Contraseña restablecida. Copie la temporal si el correo no se envió.",
            Data = resultDto
        };
    }

    private static string GenerateTemporaryPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$";
        var bytes = RandomNumberGenerator.GetBytes(14);
        var sb = new StringBuilder(16);
        sb.Append("Aa1!");
        foreach (var b in bytes)
            sb.Append(alphabet[b % alphabet.Length]);
        return sb.ToString();
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
