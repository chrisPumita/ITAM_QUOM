namespace ITAM.Api;

/// <summary>
/// Metadatos del servicio expuestos en GET /api/Status.
/// Bump <see cref="Version"/> y <see cref="LastUpdate"/> en cada publish a producción.
/// </summary>
public class ApiMetaData
{
    public string Service => "ITAM QUOM API";

    /// <summary>SemVer del API. Subir al publicar reglas/endpoints nuevos.</summary>
    public string Version => "1.3.0";

    public int Status => 200;

    /// <summary>
    /// Fecha/hora local (America/Mexico_City) de la última actualización publicada.
    /// Formato: yyyy-MM-dd HH:mm
    /// </summary>
    public string LastUpdate => "2026-09-26 12:45";

    /// <summary>Misma marca en UTC para clientes/monitoreo.</summary>
    public string LastUpdateUtc => "2026-09-26T18:45:00Z";

    public string PoweredBy => "ITAM QUOM";
    public string ContactUrl => "https://localhost";
}
