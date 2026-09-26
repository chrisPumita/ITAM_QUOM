namespace ITAM.WebApp.ApiConnect;

public sealed class ApiConnectSettings
{
    public const string SectionName = "ApiConnect";

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 60;

    public Dictionary<string, string> Endpoints { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string GetEndpoint(string key)
    {
        if (!Endpoints.TryGetValue(key, out var path) || string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException($"ApiConnect: endpoint '{key}' no configurado.");
        return path;
    }
}
