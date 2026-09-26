using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ITAM.Api.Controllers;

/// <summary>Estado del servicio y conexión a base de datos.</summary>
[Route("api/[controller]")]
[ApiController]
public class StatusController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StatusController> _logger;

    public StatusController(IConfiguration configuration, ILogger<StatusController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Estado del servicio. Anónimo.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAsync([FromServices] ApiMetaData metaData)
    {
        _logger.LogInformation("Petición a Status de ITAM.Api");

        var conectDb = false;
        var accessDb = "No hay cadena de conexión configurada";

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                conectDb = true;
                accessDb = $"Base de datos: {connection.Database}, Proveedor: SQL Server, Versión: {connection.ServerVersion}";
                _logger.LogInformation("Conexión a la base de datos exitosa");
            }
            catch (Exception ex)
            {
                accessDb = $"SIN CONEXIÓN: {ex.Message}";
                _logger.LogError(ex, "Error en la prueba de conexión");
            }
        }

        return Ok(new
        {
            metaData.Service,
            metaData.Version,
            status = metaData.Status,
            ApiAccess = conectDb ? "OK" : "SIN CONEXIÓN",
            AccessDb = accessDb,
            metaData.LastUpdate,
            metaData.LastUpdateUtc,
            metaData.PoweredBy
        });
    }
}
