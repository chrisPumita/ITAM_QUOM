using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ITAM.WebApp.ApiConnect;

/// <summary>
/// Cliente fluent hacia ITAM.Api. Uso típico:
/// <code>
/// var response = await _api.Create()
///     .WithEndpoint(ApiEndpoints.AuthLogin)
///     .WithMethod(HttpMethod.Post)
///     .WithJsonBody(dto)
///     .SendAsync(ct);
/// </code>
/// </summary>
public sealed class HttpRequestBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiConnectSettings _settings;

    private string? _endpointKey;
    private string? _relativePath;
    private HttpMethod _method = HttpMethod.Get;
    private object? _body;
    private string? _bearerToken;
    private readonly List<KeyValuePair<string, string>> _query = [];
    private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string?> _routeValues = new(StringComparer.OrdinalIgnoreCase);

    public HttpRequestBuilder(IHttpClientFactory httpClientFactory, IOptions<ApiConnectSettings> options)
    {
        _httpClientFactory = httpClientFactory;
        _settings = options.Value;
    }

    /// <summary>Clave de <see cref="ApiConnectSettings.Endpoints"/>.</summary>
    public HttpRequestBuilder WithEndpoint(string endpointKey)
    {
        _endpointKey = endpointKey;
        return this;
    }

    /// <summary>Ruta relativa explícita (si no usas clave de appsettings).</summary>
    public HttpRequestBuilder WithPath(string relativePath)
    {
        _relativePath = relativePath;
        return this;
    }

    public HttpRequestBuilder WithMethod(HttpMethod method)
    {
        _method = method;
        return this;
    }

    public HttpRequestBuilder WithBearer(string? token)
    {
        _bearerToken = token;
        return this;
    }

    public HttpRequestBuilder WithJsonBody(object body)
    {
        _body = body;
        return this;
    }

    public HttpRequestBuilder WithQuery(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            _query.Add(new(name, value));
        return this;
    }

    public HttpRequestBuilder WithQuery(string name, IEnumerable<string>? values)
    {
        if (values is null) return this;
        foreach (var v in values)
            WithQuery(name, v);
        return this;
    }

    /// <summary>Reemplaza <c>{id}</c>, <c>{employeeId}</c>, etc. en la plantilla del endpoint.</summary>
    public HttpRequestBuilder WithRoute(string name, object value)
    {
        _routeValues[name] = value?.ToString();
        return this;
    }

    public HttpRequestBuilder WithHeader(string name, string value)
    {
        _headers[name] = value;
        return this;
    }

    public async Task<HttpResponseMessage> SendAsync(CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(ApiConnectDefaults.HttpClientName);
        var path = ResolvePath();
        var uri = BuildUri(path);

        using var request = new HttpRequestMessage(_method, uri);

        if (!string.IsNullOrWhiteSpace(_bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);

        foreach (var (k, v) in _headers)
            request.Headers.TryAddWithoutValidation(k, v);

        if (_body is not null)
        {
            var json = JsonSerializer.Serialize(_body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    public async Task<T?> SendJsonAsync<T>(CancellationToken ct = default)
    {
        using var response = await SendAsync(ct);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        if (response.Content.Headers.ContentLength == 0)
            return default;

        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct);
    }

    public async Task<ApiFileDownload?> SendFileAsync(CancellationToken ct = default)
    {
        var response = await SendAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            return null;
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? "download.bin";
        var contentType = response.Content.Headers.ContentType?.ToString()
                          ?? "application/octet-stream";
        response.Dispose();

        return new ApiFileDownload(bytes, contentType, fileName);
    }

    private string ResolvePath()
    {
        var template = !string.IsNullOrWhiteSpace(_relativePath)
            ? _relativePath!
            : _settings.GetEndpoint(_endpointKey
                ?? throw new InvalidOperationException("ApiConnect: indique WithEndpoint o WithPath."));

        foreach (var (name, value) in _routeValues)
            template = template.Replace($"{{{name}}}", Uri.EscapeDataString(value ?? string.Empty),
                StringComparison.OrdinalIgnoreCase);

        if (template.Contains('{', StringComparison.Ordinal))
            throw new InvalidOperationException($"ApiConnect: faltan route values en '{template}'.");

        return template;
    }

    private Uri BuildUri(string path)
    {
        if (_query.Count == 0)
            return new Uri(path, UriKind.Relative);

        var qs = string.Join("&", _query.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        var separator = path.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return new Uri($"{path}{separator}{qs}", UriKind.Relative);
    }
}

public sealed record ApiFileDownload(byte[] Content, string ContentType, string FileName);

public static class ApiConnectDefaults
{
    public const string HttpClientName = "ITAM.Api";
}

/// <summary>Claves de endpoints (deben coincidir con appsettings ApiConnect:Endpoints).</summary>
public static class ApiEndpoints
{
    public const string Status = "Status";

    public const string AuthLogin = "Auth.Login";
    public const string AuthMe = "Auth.Me";
    public const string AuthUsers = "Auth.Users";
    public const string AuthUnlock = "Auth.Unlock";

    public const string Categories = "Categories";
    public const string CategoryById = "Categories.ById";
    public const string Brands = "Brands";
    public const string BrandById = "Brands.ById";
    public const string Models = "Models";
    public const string ModelById = "Models.ById";
    public const string Locations = "Locations";
    public const string LocationById = "Locations.ById";

    public const string Employees = "Employees";
    public const string EmployeeById = "Employees.ById";
    public const string Suppliers = "Suppliers";
    public const string SupplierById = "Suppliers.ById";

    public const string Assets = "Assets";
    public const string AssetById = "Assets.ById";
    public const string AssetsExport = "Assets.Export";

    public const string Assignments = "Assignments";
    public const string AssignmentById = "Assignments.ById";
    public const string AssignmentsAssign = "Assignments.Assign";
    public const string AssignmentsReturn = "Assignments.Return";
    public const string AssignmentsMovements = "Assignments.Movements";
    public const string AssignmentsMovementsExport = "Assignments.MovementsExport";
    public const string AssignmentsCustody = "Assignments.Custody";
    public const string AssignmentsCustodyById = "Assignments.CustodyById";
    public const string AssignmentsCustodyPdf = "Assignments.CustodyPdf";
}

/// <summary>Factory DI: cada llamada a <see cref="Create"/> inicia un builder limpio.</summary>
public sealed class ApiConnectFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<ApiConnectSettings> _options;

    public ApiConnectFactory(IHttpClientFactory httpClientFactory, IOptions<ApiConnectSettings> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public HttpRequestBuilder Create() => new(_httpClientFactory, _options);
}
