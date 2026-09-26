using System.Net;
using ITAM.Domain.Interfaces.Services.Catalog;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Catalog;
using ITAM.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.Api.Controllers;

/// <summary>Categorías.</summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _service;

    public CategoriesController(ICategoryService service) => _service = service;

    /// <summary>Lista categorías.</summary>
    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<List<CategoryListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CategoryListDto>>>> List([FromQuery] bool? onlyActive = true)
        => ApiResponseFactory.FromResult(await _service.ListAsync(onlyActive), HttpStatusCode.OK);

    /// <summary>Obtiene por id.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CategoryListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryListDto>>> Get(int id)
        => ApiResponseFactory.FromResult(await _service.GetAsync(id), HttpStatusCode.OK);

    /// <summary>Alta.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<int>>> Create([FromBody] CategoryUpsertDto dto)
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
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] CategoryUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponseFactory.InvalidModel<bool>();
        return ApiResponseFactory.FromResult(await _service.UpdateAsync(id, dto), HttpStatusCode.OK);
    }
}
