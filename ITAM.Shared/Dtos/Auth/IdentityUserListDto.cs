namespace ITAM.Shared.Dtos.Auth;

public class IdentityUserListDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public string? Role { get; set; }
}
