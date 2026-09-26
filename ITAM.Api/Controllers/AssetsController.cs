using System.Net;
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

    public AssetsController(IAssetService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<List<AssetListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<AssetListDto>>>> List(
        [FromQuery] AssetStatus? status = null,
        [FromQuery] AssetKind? kind = null,
        [FromQuery] int? modelId = null,
        [FromQuery] int? locationId = null)
        => ApiResponseFactory.FromResult(
            await _service.ListAsync(status, kind, modelId, locationId),
            HttpStatusCode.OK);

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
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] AssetUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponseFactory.InvalidModel<Guid>();
        return ApiResponseFactory.FromResult(await _service.CreateAsync(dto), HttpStatusCode.Created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<bool>>> Update(Guid id, [FromBody] AssetUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponseFactory.InvalidModel<bool>();
        return ApiResponseFactory.FromResult(await _service.UpdateAsync(id, dto), HttpStatusCode.OK);
    }
}
