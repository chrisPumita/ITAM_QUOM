using System.Net;
using System.Security.Claims;
using ITAM.Domain.Interfaces.Services;
using ITAM.Domain.Interfaces.Services.Assets;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assets;
using ITAM.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.Api.Controllers;

/// <summary>
/// Inventario de activos. Lectura: Admin/Operador. Alta/edición: Administrador.
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
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AssetListDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetListDto>>>> List([FromQuery] AssetListQuery query)
        => ApiResponseFactory.FromResult(await _service.ListAsync(query), HttpStatusCode.OK);

    /// <summary>Excel del inventario (mismos filtros que el listado; sin paginar).</summary>
    [HttpGet("export.xlsx")]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
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

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<AssetListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssetListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AssetListDto>>> Get(Guid id)
        => ApiResponseFactory.FromResult(await _service.GetAsync(id), HttpStatusCode.OK);

    [HttpPost]
    [Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status409Conflict)]
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
    [Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<bool>>> Update(Guid id, [FromBody] AssetUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponseFactory.InvalidModel<bool>();

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        return ApiResponseFactory.FromResult(await _service.UpdateAsync(id, dto, userId.Value), HttpStatusCode.OK);
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue("identityUserId")
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
