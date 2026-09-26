using System.ComponentModel.DataAnnotations;
using ITAM.Shared.Enums;

namespace ITAM.Shared.Dtos.Auth;

public class CreateAdminUserDto
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Role { get; set; } = AppRoles.Administrador;

    [MaxLength(300)]
    public string? PublicAppBaseUrl { get; set; }

    public bool SendEmail { get; set; } = true;
}

public class CreateAdminUserResultDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public string LoginUrl { get; set; } = string.Empty;
    public bool EmailSent { get; set; }
    public string? EmailError { get; set; }
}
