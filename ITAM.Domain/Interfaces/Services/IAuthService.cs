using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Auth;

namespace ITAM.Domain.Interfaces.Services;

/// <summary>Autenticación y usuarios.</summary>
public interface IAuthService
{
    /// <summary>Login por correo y contraseña.</summary>
    Task<Result<LoginResultDto>> LoginAsync(LoginDto dto);

    /// <summary>Lista usuarios Identity (filtro activos / sin vincular).</summary>
    Task<Result<List<IdentityUserListDto>>> ListUsersAsync(bool? onlyActive, bool? onlyUnlinked);

    /// <summary>Quita el bloqueo por intentos fallidos.</summary>
    Task<Result<bool>> UnlockUserAsync(Guid userId);

    /// <summary>Crea Administrador u Operador con contraseña temporal.</summary>
    Task<Result<CreateAdminUserResultDto>> CreateAdminUserAsync(CreateAdminUserDto dto, CancellationToken ct = default);

    /// <summary>Cambia la contraseña del usuario autenticado.</summary>
    Task<Result<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);

    /// <summary>Restablece la contraseña de otro usuario.</summary>
    Task<Result<AdminResetPasswordResultDto>> AdminResetPasswordAsync(
        Guid userId, AdminResetPasswordDto dto, CancellationToken ct = default);
}
