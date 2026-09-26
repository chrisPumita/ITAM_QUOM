using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Auth;
using ITAM.Shared.Enums;
using ITAM.Shared.Services.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using ITAM.Domain.Interfaces.Services;
using ITAM.Infrastructure.Services.Mail;
using Microsoft.AspNetCore.RateLimiting;

namespace ITAM.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ISmtpMailSender _mail;
    private readonly JwtSettings _jwt;

    public AuthController(IAuthService authService, ISmtpMailSender mail, IOptions<JwtSettings> jwtOptions)
    {
        _authService = authService;
        _mail = mail;
        _jwt = jwtOptions.Value;
    }

    /// <summary>Login y emisión de JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<LoginResponseDto>
            {
                Code = HttpStatusCode.BadRequest,
                Message = "Datos de inicio de sesión no válidos.",
                Error = "ModelState inválido"
            });

        var result = await _authService.LoginAsync(model);
        if (!result.IsSuccess)
        {
            if (result.Error is "Usuario inactivo" or "LockedOut")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<LoginResponseDto>
                {
                    Code = HttpStatusCode.Forbidden,
                    Message = result.Message,
                    Error = result.Error
                });
            }

            return Unauthorized(new ApiResponse<LoginResponseDto>
            {
                Code = HttpStatusCode.Unauthorized,
                Message = result.Message,
                Error = result.Error
            });
        }

        var data = result.Data!;
        var expiraEn = DateTime.UtcNow.AddHours(_jwt.ExpirationHours);
        var token = GenerateToken(data, expiraEn);

        return Ok(new ApiResponse<LoginResponseDto>
        {
            Code = HttpStatusCode.OK,
            Message = result.Message,
            Data = new LoginResponseDto
            {
                IdentityUserId = data.IdentityUserId,
                Email = data.Email,
                DisplayName = data.DisplayName,
                Role = data.Role,
                Token = token,
                ExpiraEn = expiraEn
            }
        });
    }

    /// <summary>Usuario autenticado actual.</summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<object> Me()
    {
        return Ok(new
        {
            Id = User.FindFirstValue("identityUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
            Email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(JwtRegisteredClaimNames.Email),
            Name = User.FindFirstValue("displayName"),
            Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
        });
    }

    /// <summary>Lista usuarios.</summary>
    [HttpGet("users")]
    [Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<List<IdentityUserListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<IdentityUserListDto>>>> ListUsers(
        [FromQuery] bool? onlyActive = true,
        [FromQuery] bool? onlyUnlinked = false)
    {
        var result = await _authService.ListUsersAsync(onlyActive, onlyUnlinked);
        return Ok(new ApiResponse<List<IdentityUserListDto>>
        {
            Code = HttpStatusCode.OK,
            Message = result.Message,
            Data = result.Data
        });
    }

    /// <summary>Desbloquea un usuario.</summary>
    [HttpPost("users/{id:guid}/unlock")]
    [Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> UnlockUser(Guid id)
    {
        var result = await _authService.UnlockUserAsync(id);
        if (!result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status404NotFound, new ApiResponse<bool>
            {
                Code = HttpStatusCode.NotFound,
                Message = result.Message,
                Error = result.Error
            });
        }

        return Ok(new ApiResponse<bool>
        {
            Code = HttpStatusCode.OK,
            Message = result.Message,
            Data = true
        });
    }

    /// <summary>Crea Administrador u Operador.</summary>
    [HttpPost("users")]
    [Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<CreateAdminUserResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CreateAdminUserResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CreateAdminUserResultDto>>> CreateUser(
        [FromBody] CreateAdminUserDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<CreateAdminUserResultDto>
            {
                Code = HttpStatusCode.BadRequest,
                Message = "Datos inválidos.",
                Error = "Validation"
            });
        }

        var result = await _authService.CreateAdminUserAsync(dto, ct);
        if (!result.IsSuccess)
        {
            var code = result.Error == "Duplicate" ? HttpStatusCode.Conflict : HttpStatusCode.BadRequest;
            return StatusCode((int)code, new ApiResponse<CreateAdminUserResultDto>
            {
                Code = code,
                Message = result.Message,
                Error = result.Error
            });
        }

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<CreateAdminUserResultDto>
        {
            Code = HttpStatusCode.Created,
            Message = result.Message,
            Data = result.Data
        });
    }

    /// <summary>Envía un correo de prueba.</summary>
    [HttpPost("test-email")]
    [Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<TestEmailResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TestEmailResultDto>>> TestEmail(
        [FromBody] TestEmailDto? dto, CancellationToken ct)
    {
        var diagnostics = _mail.GetDiagnostics();
        var to = string.IsNullOrWhiteSpace(dto?.ToEmail)
            ? diagnostics.FromEmail
            : dto!.ToEmail!.Trim();

        if (string.IsNullOrWhiteSpace(to))
        {
            return BadRequest(new ApiResponse<TestEmailResultDto>
            {
                Code = HttpStatusCode.BadRequest,
                Message = "Indique un correo destinatario o configure el remitente SMTP.",
                Data = new TestEmailResultDto
                {
                    Sent = false,
                    Error = "Sin destinatario",
                    Diagnostics = diagnostics
                }
            });
        }

        var (sent, err) = await _mail.SendAsync(
            to,
            "ITAM QUOM — prueba de correo",
            $"""
             <p>Correo de prueba enviado correctamente.</p>
             <p>Host: <code>{System.Net.WebUtility.HtmlEncode(diagnostics.Host)}</code>
             · Puerto: <code>{diagnostics.Port}</code></p>
             <p>Si recibió este mensaje, las credenciales SMTP funcionan.</p>
             """,
            ct);

        return Ok(new ApiResponse<TestEmailResultDto>
        {
            Code = HttpStatusCode.OK,
            Message = sent ? "Correo de prueba enviado." : (err ?? "No se pudo enviar."),
            Data = new TestEmailResultDto
            {
                Sent = sent,
                Error = err,
                ToEmail = to,
                Diagnostics = diagnostics
            }
        });
    }

    /// <summary>Cambia la contraseña del usuario autenticado.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<bool>
            {
                Code = HttpStatusCode.BadRequest,
                Message = "Datos inválidos.",
                Error = "Validation"
            });
        }

        var idRaw = User.FindFirstValue("identityUserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idRaw, out var userId))
        {
            return Unauthorized(new ApiResponse<bool>
            {
                Code = HttpStatusCode.Unauthorized,
                Message = "Sesión inválida.",
                Error = "Unauthorized"
            });
        }

        var result = await _authService.ChangePasswordAsync(userId, dto);
        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<bool>
            {
                Code = HttpStatusCode.BadRequest,
                Message = result.Message,
                Error = result.Error
            });
        }

        return Ok(new ApiResponse<bool>
        {
            Code = HttpStatusCode.OK,
            Message = result.Message,
            Data = true
        });
    }

    /// <summary>Restablece la contraseña de un usuario.</summary>
    [HttpPost("users/{id:guid}/reset-password")]
    [Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<AdminResetPasswordResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdminResetPasswordResultDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AdminResetPasswordResultDto>>> ResetPassword(
        Guid id, [FromBody] AdminResetPasswordDto? dto, CancellationToken ct)
    {
        dto ??= new AdminResetPasswordDto();
        var result = await _authService.AdminResetPasswordAsync(id, dto, ct);
        if (!result.IsSuccess)
        {
            var code = result.Error == "NotFound" ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest;
            return StatusCode((int)code, new ApiResponse<AdminResetPasswordResultDto>
            {
                Code = code,
                Message = result.Message,
                Error = result.Error
            });
        }

        return Ok(new ApiResponse<AdminResetPasswordResultDto>
        {
            Code = HttpStatusCode.OK,
            Message = result.Message,
            Data = result.Data
        });
    }

    private string GenerateToken(LoginResultDto data, DateTime expiraEn)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, data.IdentityUserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, data.Email),
            new(ClaimTypes.NameIdentifier, data.IdentityUserId.ToString()),
            new(ClaimTypes.Email, data.Email),
            new("identityUserId", data.IdentityUserId.ToString()),
            new("displayName", data.DisplayName)
        };

        if (data.Role.HasValue)
            claims.Add(new Claim(ClaimTypes.Role, data.Role.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiraEn,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
