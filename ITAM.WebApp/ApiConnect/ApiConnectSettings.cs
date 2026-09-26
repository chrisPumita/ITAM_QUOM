namespace ITAM.WebApp.ApiConnect;

/// <summary>Sección <c>ApiConnect</c> en appsettings (BaseUrl + rutas relativas).</summary>
public sealed class ApiConnectSettings
{
    public const string SectionName = "ApiConnect";

    /// <summary>URL base de la API (sin slash final). Ej: https://localhost:7xxx</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Timeout HTTP en segundos.</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Rutas relativas indexadas por clave (ver appsettings).</summary>
    public Dictionary<string, string> Endpoints { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string GetEndpoint(string key)
    {
        if (!Endpoints.TryGetValue(key, out var path) || string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException($"ApiConnect: endpoint '{key}' no configurado.");
        return path;
    }
}
