namespace ITAM.Shared.Services.Company;

/// <summary>
/// Datos de la empresa para documentos (responsiva PDF, etc.). Sección <see cref="SectionName"/>.
/// </summary>
public class CompanySettings
{
    public const string SectionName = "Company";

    public string Name { get; set; } = "ITAM QUOM";
    public string LegalName { get; set; } = string.Empty;
    public string Rfc { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;

    /// <summary>Ruta relativa al ContentRoot (ej. wwwroot/branding/logo.png). Opcional.</summary>
    public string? LogoPath { get; set; }
}
