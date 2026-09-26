namespace ITAM.WebApp;

/// <summary>
/// Metadatos del MVC. Bump <see cref="Version"/> y <see cref="LastUpdate"/> en cada publish.
/// </summary>
public class WebAppMetaData
{
    public string Service => "ITAM QUOM WebApp";

    /// <summary>SemVer del front. Subir al publicar pantallas/flujos nuevos.</summary>
    public string Version => "1.3.0";

    /// <summary>
    /// Fecha/hora local (America/Mexico_City) de la última actualización publicada.
    /// Formato: yyyy-MM-dd HH:mm
    /// </summary>
    public string LastUpdate => "2026-09-26 12:45";

    /// <summary>Misma marca en UTC.</summary>
    public string LastUpdateUtc => "2026-09-26T18:45:00Z";

    public string PoweredBy => "ITAM QUOM";
}
