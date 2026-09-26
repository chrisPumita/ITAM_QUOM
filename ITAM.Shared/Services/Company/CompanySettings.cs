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

    /// <summary>
    /// Leyenda legal de la responsiva (entre tabla y firmas). Editable por empresa.
    /// </summary>
    public string CustodyLegend { get; set; } =
        "Por medio de la presente, el colaborador abajo firmante declara haber recibido a su entera satisfacción " +
        "el(los) equipo(s) y/o accesorio(s) descritos en este documento, comprometiéndose a: (1) utilizarlos " +
        "exclusivamente para actividades laborales; (2) custodiarlos y mantenerlos en buen estado; (3) reportar " +
        "de inmediato cualquier falla, daño o extravío; y (4) devolverlos a la empresa al término de la relación " +
        "laboral o cuando le sea solicitado. El colaborador será responsable del costo de reposición o reparación " +
        "en caso de extravío, daño por negligencia o mal uso.";
}
