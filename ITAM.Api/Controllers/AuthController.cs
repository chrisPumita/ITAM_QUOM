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
using Microsoft.AspNetCore.RateLimiting;

namespace ITAM.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtSettings _jwt;

    public AuthController(IAuthService authService, IOptions<JwtSettings> jwtOptions)
    {
        _authService = authService;
        _jwt = jwtOptions.Value;
    }

    /// <summary>Autentica un usuario y retorna JWT con claims de rol.</summary>
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

    /// <summary>Endpoint de prueba protegido: valida JWT y expone claims.</summary>
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

    /// <summary>
    /// Lista usuarios Identity para asociar a empleados (identityUserId).
    /// Solo Administrador. Filtros: onlyActive, onlyUnlinked.
    /// </summary>
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

    /// <summary>Desbloquea un usuario bloqueado por intentos fallidos de login. Solo Administrador.</summary>
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
