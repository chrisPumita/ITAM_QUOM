using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Auth;

namespace ITAM.Domain.Interfaces.Services;

/// <summary>
/// Contrato de autenticación de la API.
/// Implementación: <c>ITAM.Infrastructure.Services.AuthService</c> (ASP.NET Identity).
/// </summary>
/// <remarks>
/// Flujo esperado:
/// <list type="number">
/// <item>Validar email/password con Identity (hash, nunca texto plano).</item>
/// <item>Rechazar usuarios inactivos.</item>
/// <item>Resolver el rol desde claims (<c>ClaimTypes.Role</c>) o roles de Identity.</item>
/// <item>Devolver <see cref="LoginResultDto"/> sin JWT; el token lo emite el AuthController.</item>
/// </list>
/// Roles: <c>Administrador</c>, <c>Operador</c> (<c>ITAM.Shared.Enums.AppRoles</c>).
/// </remarks>
public interface IAuthService
{
    /// <summary>
    /// Autentica un usuario por correo y contraseña.
    /// </summary>
    /// <param name="dto">Credenciales (<see cref="LoginDto.Email"/>, <see cref="LoginDto.Password"/>).</param>
    /// <returns>
    /// <see cref="Result{T}.IsSuccess"/> = true con datos de sesión;
    /// false con <c>Error</c> "Email o contraseña incorrectos" o "Usuario inactivo".
    /// </returns>
    Task<Result<LoginResultDto>> LoginAsync(LoginDto dto);

    /// <summary>
    /// Lista usuarios Identity para vincular a empleados.
    /// </summary>
    /// <param name="onlyActive">Si true, solo cuentas activas.</param>
    /// <param name="onlyUnlinked">Si true, excluye usuarios ya asociados a un Employee.</param>
    Task<Result<List<IdentityUserListDto>>> ListUsersAsync(bool? onlyActive, bool? onlyUnlinked);

    /// <summary>
    /// Quita el bloqueo temporal por intentos fallidos (Admin).
    /// </summary>
    Task<Result<bool>> UnlockUserAsync(Guid userId);
}
