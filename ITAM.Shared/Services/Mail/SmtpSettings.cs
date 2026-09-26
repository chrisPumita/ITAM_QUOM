namespace ITAM.Shared.Services.Mail;

/// <summary>Configuración SMTP.</summary>
public class SmtpSettings
{
    public const string SectionName = "SmtpSettings";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromDisplayName { get; set; } = "ITAM QUOM";

    /// <summary>URL base del sitio para enlaces en correos.</summary>
    public string PublicAppBaseUrl { get; set; } = string.Empty;
}
