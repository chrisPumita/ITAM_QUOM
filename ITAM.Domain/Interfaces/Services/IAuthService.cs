using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Auth;

namespace ITAM.Domain.Interfaces.Services;

public interface IAuthService
{
    Task<Result<LoginResultDto>> LoginAsync(LoginDto dto);

    Task<Result<List<IdentityUserListDto>>> ListUsersAsync(bool? onlyActive, bool? onlyUnlinked);

    Task<Result<bool>> UnlockUserAsync(Guid userId);

    Task<Result<CreateAdminUserResultDto>> CreateAdminUserAsync(CreateAdminUserDto dto, CancellationToken ct = default);

    Task<Result<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);

    Task<Result<AdminResetPasswordResultDto>> AdminResetPasswordAsync(
        Guid userId, AdminResetPasswordDto dto, CancellationToken ct = default);
}
