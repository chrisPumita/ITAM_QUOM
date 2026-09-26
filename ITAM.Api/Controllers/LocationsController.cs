using System.Net;
using ITAM.Domain.Interfaces.Services.Catalog;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Catalog;
using ITAM.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.Api.Controllers;

/// <summary>Ubicaciones.</summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _service;

    public LocationsController(ILocationService service) => _service = service;

    /// <summary>Lista ubicaciones.</summary>
    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<List<LocationListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<LocationListDto>>>> List(
        [FromQuery] bool? onlyActive = true,
        [FromQuery] bool? onlyWarehouses = null)
        => ApiResponseFactory.FromResult(
            await _service.ListAsync(onlyActive, onlyWarehouses),
            HttpStatusCode.OK);

    /// <summary>Obtiene por id.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<LocationListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LocationListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LocationListDto>>> Get(int id)
        => ApiResponseFactory.FromResult(await _service.GetAsync(id), HttpStatusCode.OK);

    /// <summary>Alta.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<int>>> Create([FromBody] LocationUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponseFactory.InvalidModel<int>();
        return ApiResponseFactory.FromResult(await _service.CreateAsync(dto), HttpStatusCode.Created);
    }

    /// <summary>Actualización.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] LocationUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponseFactory.InvalidModel<bool>();
        return ApiResponseFactory.FromResult(await _service.UpdateAsync(id, dto), HttpStatusCode.OK);
    }
}
