using System.Net;
using System.Security.Claims;
using ITAM.Domain.Interfaces.Services;
using ITAM.Domain.Interfaces.Services.Assets;
using ITAM.Infrastructure.Services.Assets;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assets;
using ITAM.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.Api.Controllers;

/// <summary>
/// Inventario de activos. Lectura: Admin/Operador. Alta: ambos. Edición/baja: Administrador.
/// Assigned se gestiona por flujo de asignación (no por este CRUD).
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _service;
    private readonly IAssetExportService _export;

    public AssetsController(IAssetService service, IAssetExportService export)
    {
        _service = service;
        _export = export;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AssetListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetListDto>>>> List([FromQuery] AssetListQuery query)
        => ApiResponseFactory.FromResult(await _service.ListAsync(query), HttpStatusCode.OK);

    [HttpGet("summary")]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<AssetSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AssetSummaryDto>>> Summary()
        => ApiResponseFactory.FromResult(await _service.GetSummaryAsync(), HttpStatusCode.OK);

    [HttpGet("export.xlsx")]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> Export([FromQuery] AssetListQuery query, CancellationToken ct = default)
    {
        var result = await _export.ExportAsync(query, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            var code = result.Error == "Validation"
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status404NotFound;
            return new ObjectResult(new ApiResponse<object>
            {
                Code = (HttpStatusCode)code,
                Message = result.Message,
                Error = result.Error
            })
            {
                StatusCode = code
            };
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpGet("import/template.xlsx")]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public IActionResult ImportTemplate()
    {
        var file = AssetExportService.BuildImportTemplate();
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPost("import")]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [RequestSizeLimit(10_000_000)]
    [ProducesResponseType(typeof(ApiResponse<AssetImportResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AssetImportResultDto>>> Import(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ApiResponse<AssetImportResultDto>
            {
                Code = HttpStatusCode.BadRequest,
                Message = "Archivo Excel requerido.",
                Error = "Validation"
            });
        }

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        await using var stream = file.OpenReadStream();
        var rows = AssetExportService.ParseImportWorkbook(stream);
        return ApiResponseFactory.FromResult(
            await _service.ImportAsync(new AssetImportRequestDto { Rows = rows }, userId.Value),
            HttpStatusCode.OK);
    }

    [HttpGet("by-code/{code}")]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<AssetListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AssetListDto>>> GetByCode(string code)
        => ApiResponseFactory.FromResult(await _service.GetByCodeAsync(code), HttpStatusCode.OK);

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<AssetListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AssetListDto>>> Get(Guid id)
        => ApiResponseFactory.FromResult(await _service.GetAsync(id), HttpStatusCode.OK);

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] AssetUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponseFactory.InvalidModel<Guid>();

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        return ApiResponseFactory.FromResult(await _service.CreateAsync(dto, userId.Value), HttpStatusCode.Created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> Update(Guid id, [FromBody] AssetUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponseFactory.InvalidModel<bool>();

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var isAdmin = User.IsInRole(AppRoles.Administrador);
        return ApiResponseFactory.FromResult(
            await _service.UpdateAsync(id, dto, userId.Value, isAdmin),
            HttpStatusCode.OK);
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue("identityUserId")
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
