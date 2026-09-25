using System.Net;
using ITAM.Domain.Interfaces.Services;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Catalog;
using ITAM.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.Api.Controllers;

/// <summary>
/// Catálogo de marcas. Lectura: Admin/Operador. Alta/edición: Administrador.
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class BrandsController : ControllerBase
{
    private readonly IBrandService _service;

    public BrandsController(IBrandService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    public async Task<ActionResult<ApiResponse<List<BrandListDto>>>> List([FromQuery] bool? onlyActive = true)
    {
        var result = await _service.ListAsync(onlyActive);
        return Ok(ApiResponseFactory.From(result, HttpStatusCode.OK));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    public async Task<ActionResult<ApiResponse<BrandListDto>>> Get(int id)
    {
        var result = await _service.GetAsync(id);
        if (!result.IsSuccess)
            return NotFound(ApiResponseFactory.From(result, HttpStatusCode.NotFound));
        return Ok(ApiResponseFactory.From(result, HttpStatusCode.OK));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Administrador)]
    public async Task<ActionResult<ApiResponse<int>>> Create([FromBody] BrandUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponseFactory.InvalidModel<int>());

        var result = await _service.CreateAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(ApiResponseFactory.From(result, HttpStatusCode.BadRequest));
        return StatusCode(StatusCodes.Status201Created, ApiResponseFactory.From(result, HttpStatusCode.Created));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Administrador)]
    public async Task<ActionResult<ApiResponse<bool>>> Update(int id, [FromBody] BrandUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponseFactory.InvalidModel<bool>());

        var result = await _service.UpdateAsync(id, dto);
        if (!result.IsSuccess && result.Error == "NotFound")
            return NotFound(ApiResponseFactory.From(result, HttpStatusCode.NotFound));
        if (!result.IsSuccess)
            return BadRequest(ApiResponseFactory.From(result, HttpStatusCode.BadRequest));
        return Ok(ApiResponseFactory.From(result, HttpStatusCode.OK));
    }
}
