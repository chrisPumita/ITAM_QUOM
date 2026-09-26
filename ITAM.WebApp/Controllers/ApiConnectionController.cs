using System.Text.Json;
using ITAM.WebApp.ApiConnect;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.WebApp.Controllers;

/// <summary>Proxy local del health de la API (evita CORS; el navegador llama a la WebApp).</summary>
[Route("api/connection")]
[ApiController]
public sealed class ApiConnectionController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ApiConnectFactory _api;

    public ApiConnectionController(ApiConnectFactory api) => _api = api;

    /// <summary>Comprueba <c>GET /api/Status</c> en la API remota.</summary>
    [HttpGet("ping")]
    [ResponseCache(NoStore = true, Duration = 0)]
    public async Task<IActionResult> Ping(CancellationToken ct)
    {
        try
        {
            using var response = await _api.Create()
                .WithEndpoint(ApiEndpoints.Status)
                .WithMethod(HttpMethod.Get)
                .SendAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                return Ok(new
                {
                    connected = false,
                    message = $"API respondió {(int)response.StatusCode}"
                });
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var payload = await JsonSerializer.DeserializeAsync<StatusPayload>(stream, JsonOptions, ct);
            var dbOk = string.Equals(payload?.ApiAccess, "OK", StringComparison.OrdinalIgnoreCase);

            return Ok(new
            {
                connected = dbOk,
                message = dbOk
                    ? (payload?.AccessDb ?? "API y base de datos OK")
                    : (payload?.AccessDb ?? "API sin conexión a BD"),
                service = payload?.Service,
                version = payload?.Version
            });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                connected = false,
                message = $"Sin conexión a la API: {ex.Message}"
            });
        }
    }

    private sealed class StatusPayload
    {
        public string? Service { get; set; }
        public string? Version { get; set; }
        public string? ApiAccess { get; set; }
        public string? AccessDb { get; set; }
    }
}
