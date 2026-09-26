using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Auth;
using ITAM.Shared.Enums;
using ITAM.WebApp.ApiConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.WebApp.Controllers;

[Authorize(Roles = AppRoles.Administrador)]
public class UsuariosController : Controller
{
    private readonly ApiConnectFactory _api;

    public UsuariosController(ApiConnectFactory api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var list = await _api.Create()
            .WithEndpoint(ApiEndpoints.AuthUsers)
            .WithQuery("onlyActive", "false")
            .SendJsonAsync<ApiResponse<List<IdentityUserListDto>>>(ct);

        return View(list?.Data ?? []);
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateAdminUserDto
    {
        Role = AppRoles.Administrador,
        PublicAppBaseUrl = $"{Request.Scheme}://{Request.Host}"
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAdminUserDto dto, CancellationToken ct)
    {
        dto.PublicAppBaseUrl ??= $"{Request.Scheme}://{Request.Host}";
        if (string.IsNullOrWhiteSpace(dto.Role))
            dto.Role = AppRoles.Administrador;

        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            var result = await _api.Create()
                .WithEndpoint(ApiEndpoints.AuthUsers)
                .WithMethod(HttpMethod.Post)
                .WithJsonBody(dto)
                .SendJsonAsync<ApiResponse<CreateAdminUserResultDto>>(ct);

            if (result is not { IsSuccess: true, Data: not null })
            {
                ModelState.AddModelError(string.Empty, result?.Message ?? "No se pudo crear el usuario.");
                return View(dto);
            }

            TempData["Success"] = result.Message;
            return View("Created", result.Data);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Sin conexión con la API.");
            return View(dto);
        }
    }
}
