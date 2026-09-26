using System.Net;
using System.Security.Claims;
using ITAM.Domain.Interfaces.Services;
using ITAM.Domain.Interfaces.Services.Assignments;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assignments;
using ITAM.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.Api.Controllers;

/// <summary>Asignaciones, devoluciones y responsivas.</summary>
[Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
[Route("api/[controller]")]
[ApiController]
public class AssignmentsController : ControllerBase
{
    private readonly IAssetAssignmentService _service;
    private readonly ICustodyPdfService _custodyPdf;
    private readonly IAssignmentExportService _export;

    public AssignmentsController(
        IAssetAssignmentService service,
        ICustodyPdfService custodyPdf,
        IAssignmentExportService export)
    {
        _service = service;
        _custodyPdf = custodyPdf;
        _export = export;
    }

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

    /// <summary>Historial de movimientos.</summary>
    [HttpGet("movements")]
    [ProducesResponseType(typeof(ApiResponse<List<AssetMovementListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<AssetMovementListDto>>>> ListMovements(
        [FromQuery] Guid? assetId = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
        => ApiResponseFactory.FromResult(
            await _service.ListMovementsAsync(assetId, employeeId, from, to),
            HttpStatusCode.OK);

    /// <summary>Exporta movimientos a Excel.</summary>
    [HttpGet("export/movements.xlsx")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportMovements(
        [FromQuery] Guid? assetId = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var result = await _export.ExportMovementsAsync(assetId, employeeId, from, to, ct);
        return ToExportFileResult(result);
    }

    /// <summary>Listado de responsivas.</summary>
    [HttpGet("custody")]
    [ProducesResponseType(typeof(ApiResponse<List<CustodyFormListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CustodyFormListDto>>>> ListCustody(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
        => ApiResponseFactory.FromResult(
            await _service.ListCustodyFormsAsync(employeeId, from, to),
            HttpStatusCode.OK);

    /// <summary>Detalle de responsiva.</summary>
    [HttpGet("custody/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustodyFormDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustodyFormDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CustodyFormDetailDto>>> GetCustody(Guid id)
        => ApiResponseFactory.FromResult(await _service.GetCustodyFormAsync(id), HttpStatusCode.OK);

    /// <summary>PDF de responsiva.</summary>
    [HttpGet("custody/{id:guid}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustodyPdf(Guid id, CancellationToken ct)
    {
        var result = await _custodyPdf.GenerateAsync(id, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            return new ObjectResult(new ApiResponse<object>
            {
                Code = HttpStatusCode.NotFound,
                Message = result.Message,
                Error = result.Error
            })
            {
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    /// <summary>Detalle de una asignación.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AssignmentListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssignmentListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AssignmentListDto>>> Get(Guid id)
        => ApiResponseFactory.FromResult(await _service.GetAssignmentAsync(id), HttpStatusCode.OK);

    /// <summary>Asigna activos y emite responsiva.</summary>
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

    /// <summary>Devuelve un activo asignado.</summary>
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

    private static IActionResult ToExportFileResult(Result<ExportFile> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return new ObjectResult(new ApiResponse<object>
            {
                Code = HttpStatusCode.NotFound,
                Message = result.Message,
                Error = result.Error
            })
            {
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        return new FileContentResult(result.Data.Content, result.Data.ContentType)
        {
            FileDownloadName = result.Data.FileName
        };
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue("identityUserId")
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
