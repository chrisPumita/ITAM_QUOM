using System.ComponentModel.DataAnnotations;

namespace ITAM.Shared.Dtos.Auth;

public class TestEmailDto
{
    /// <summary>Destino. Si vacío, se usa FromEmail de SmtpSettings.</summary>
    [EmailAddress, MaxLength(256)]
    public string? ToEmail { get; set; }
}

public class TestEmailResultDto
{
    public bool Sent { get; set; }
    public string? Error { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public SmtpDiagnosticsDto Diagnostics { get; set; } = new();
}

public class SmtpDiagnosticsDto
{
    public bool IsConfigured { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool EnableSsl { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromDisplayName { get; set; } = string.Empty;
    /// <summary>UserName enmascarado (sin revelar secreto).</summary>
    public string UserNameHint { get; set; } = string.Empty;
    public bool HasPassword { get; set; }
    public bool UserNameLooksLikeEmail { get; set; }
    public List<string> Hints { get; set; } = [];
}

public class ChangePasswordDto
{
    [Required, DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(8), MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class AdminResetPasswordDto
{
    /// <summary>Si vacío, se genera una temporal.</summary>
    [DataType(DataType.Password), MinLength(8), MaxLength(100)]
    public string? NewPassword { get; set; }

    public bool SendEmail { get; set; } = true;

    [MaxLength(300)]
    public string? PublicAppBaseUrl { get; set; }
}

public class AdminResetPasswordResultDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public bool EmailSent { get; set; }
    public string? EmailError { get; set; }
}
