using System.Net;
using ITAM.Domain.Interfaces.Services.Catalog;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Catalog;
using ITAM.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.Api.Controllers;

/// <summary>
/// Catálogo de modelos (categoría + marca). Lectura: Admin/Operador. Alta/edición: Administrador.
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ModelsController : ControllerBase
{
    private readonly IModelService _service;

    public ModelsController(IModelService service) => _service = service;

    /// <summary>Lista modelos. Filtros: onlyActive, categoryId, brandId. HTTP 200.</summary>
    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<List<ModelListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ModelListDto>>>> List(
        [FromQuery] bool? onlyActive = true,
        [FromQuery] int? categoryId = null,
        [FromQuery] int? brandId = null)
        => ApiResponseFactory.FromResult(
            await _service.ListAsync(onlyActive, categoryId, brandId),
            HttpStatusCode.OK);

    /// <summary>Obtiene por id. HTTP 200 / 404.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<ModelListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ModelListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ModelListDto>>> Get(int id)
        => ApiResponseFactory.FromResult(await _service.GetAsync(id), HttpStatusCode.OK);

    /// <summary>Alta. HTTP 201 / 400 / 409.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<int>>> Create([FromBody] ModelUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponseFactory.InvalidModel<int>();
        return ApiResponseFactory.FromResult(await _service.CreateAsync(dto), HttpStatusCode.Created);
    }

    /// <summary>Actualización. HTTP 200 / 400 / 404 / 409.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] ModelUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponseFactory.InvalidModel<bool>();
        return ApiResponseFactory.FromResult(await _service.UpdateAsync(id, dto), HttpStatusCode.OK);
    }
}
