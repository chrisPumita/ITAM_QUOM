using System.Net;
using System.Security.Claims;
using ITAM.Domain.Interfaces.Services.Assignments;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assignments;
using ITAM.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.Api.Controllers;

/// <summary>
/// Asignación / devolución (ADO.NET + SP) y consultas (Dapper). Admin y Operador.
/// </summary>
[Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
[Route("api/[controller]")]
[ApiController]
public class AssignmentsController : ControllerBase
{
    private readonly IAssetAssignmentService _service;

    public AssignmentsController(IAssetAssignmentService service) => _service = service;

    /// <summary>Listado de asignaciones. Por defecto solo activas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AssignmentListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<AssignmentListDto>>>> List(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? assetId = null,
        [FromQuery] bool onlyActive = true)
        => ApiResponseFactory.FromResult(
            await _service.ListAssignmentsAsync(employeeId, assetId, onlyActive),
            HttpStatusCode.OK);

    /// <summary>Historial de movimientos (auditoría).</summary>
    [HttpGet("movements")]
    [ProducesResponseType(typeof(ApiResponse<List<AssetMovementListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<AssetMovementListDto>>>> ListMovements(
        [FromQuery] Guid? assetId = null,
        [FromQuery] Guid? employeeId = null)
        => ApiResponseFactory.FromResult(
            await _service.ListMovementsAsync(assetId, employeeId),
            HttpStatusCode.OK);

    /// <summary>Listado de responsivas (CustodyForm).</summary>
    [HttpGet("custody")]
    [ProducesResponseType(typeof(ApiResponse<List<CustodyFormListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CustodyFormListDto>>>> ListCustody(
        [FromQuery] Guid? employeeId = null)
        => ApiResponseFactory.FromResult(
            await _service.ListCustodyFormsAsync(employeeId),
            HttpStatusCode.OK);

    /// <summary>Detalle de responsiva con renglones.</summary>
    [HttpGet("custody/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustodyFormDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustodyFormDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CustodyFormDetailDto>>> GetCustody(Guid id)
        => ApiResponseFactory.FromResult(await _service.GetCustodyFormAsync(id), HttpStatusCode.OK);

    /// <summary>Detalle de una asignación.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AssignmentListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssignmentListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AssignmentListDto>>> Get(Guid id)
        => ApiResponseFactory.FromResult(await _service.GetAssignmentAsync(id), HttpStatusCode.OK);

    /// <summary>Asigna uno o más activos a un empleado y emite folio RES-yyyy-####. HTTP 201.</summary>
    [HttpPost("assign")]
    [ProducesResponseType(typeof(ApiResponse<AssignAssetsResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AssignAssetsResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AssignAssetsResultDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AssignAssetsResultDto>>> Assign([FromBody] AssignAssetsDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponseFactory.InvalidModel<AssignAssetsResultDto>();

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        return ApiResponseFactory.FromResult(
            await _service.AssignAsync(dto, userId.Value),
            HttpStatusCode.Created);
    }

    /// <summary>Devuelve un activo asignado. HTTP 200.</summary>
    [HttpPost("return")]
    [ProducesResponseType(typeof(ApiResponse<ReturnAssetResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReturnAssetResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ReturnAssetResultDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReturnAssetResultDto>>> Return([FromBody] ReturnAssetDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponseFactory.InvalidModel<ReturnAssetResultDto>();

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        return ApiResponseFactory.FromResult(
            await _service.ReturnAsync(dto, userId.Value),
            HttpStatusCode.OK);
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue("identityUserId")
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
