namespace ITAM.Shared.Services.Cors;

/// <summary>
/// Orígenes permitidos para llamadas browser→API (SPA / JS).
/// ApiConnect server-side (WebApp→API) NO requiere CORS.
/// Sección <c>CorsSettings</c> en appsettings.
/// </summary>
public sealed class CorsSettings
{
    public const string SectionName = "CorsSettings";

    /// <summary>Orígenes exactos, ej. https://itam-web.monsterasp.com</summary>
    public string[] AllowedOrigins { get; set; } = [];
}
