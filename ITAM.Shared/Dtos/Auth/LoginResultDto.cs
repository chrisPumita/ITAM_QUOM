using ITAM.Shared.Enums;

namespace ITAM.Shared.Dtos.Auth;

public class LoginResultDto
{
    public Guid IdentityUserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AppRole? Role { get; set; }
}
