using System.Net;
using ITAM.Domain.Interfaces.Services.Company;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Company;
using ITAM.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.Api.Controllers;

/// <summary>
/// Proveedores. Lectura: Admin/Operador. Alta/edición: Administrador.
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _service;

    public SuppliersController(ISupplierService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<List<SupplierListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<SupplierListDto>>>> List([FromQuery] bool? onlyActive = true)
        => ApiResponseFactory.FromResult(await _service.ListAsync(onlyActive), HttpStatusCode.OK);

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [ProducesResponseType(typeof(ApiResponse<SupplierListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SupplierListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SupplierListDto>>> Get(Guid id)
        => ApiResponseFactory.FromResult(await _service.GetAsync(id), HttpStatusCode.OK);

    [HttpPost]
    [Authorize(Roles = AppRoles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] SupplierUpsertDto dto)
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
    public async Task<ActionResult<ApiResponse<bool>>> Update(Guid id, [FromBody] SupplierUpsertDto dto)
    {
        if (!ModelState.IsValid)
            return ApiResponseFactory.InvalidModel<bool>();
        return ApiResponseFactory.FromResult(await _service.UpdateAsync(id, dto), HttpStatusCode.OK);
    }
}
