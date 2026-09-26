namespace ITAM.Shared.Services.Identity;

/// <summary>
/// Configuración JWT leída de <c>appsettings.json</c> sección <see cref="SectionName"/>.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>Clave simétrica HMAC-SHA256 (mín. 32 caracteres en desarrollo/producción).</summary>
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Vigencia del token en horas (default 12).</summary>
    public int ExpirationHours { get; set; } = 12;
}
