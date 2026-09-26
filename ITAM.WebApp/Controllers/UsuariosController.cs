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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(Guid id, string? newPassword, bool sendEmail = true, CancellationToken ct = default)
    {
        try
        {
            var result = await _api.Create()
                .WithEndpoint(ApiEndpoints.AuthResetPassword)
                .WithRoute("id", id)
                .WithMethod(HttpMethod.Post)
                .WithJsonBody(new AdminResetPasswordDto
                {
                    NewPassword = string.IsNullOrWhiteSpace(newPassword) ? null : newPassword,
                    SendEmail = sendEmail,
                    PublicAppBaseUrl = $"{Request.Scheme}://{Request.Host}"
                })
                .SendJsonAsync<ApiResponse<AdminResetPasswordResultDto>>(ct);

            if (result is not { IsSuccess: true, Data: not null })
            {
                TempData["Error"] = result?.Message ?? "No se pudo restablecer la contraseña.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = result.Message;
            TempData["ResetPassword"] = result.Data.TemporaryPassword;
            TempData["ResetEmail"] = result.Data.Email;
            if (!string.IsNullOrWhiteSpace(result.Data.EmailError))
                TempData["Error"] = result.Data.EmailError;

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            TempData["Error"] = "Sin conexión con la API.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestEmail(string? toEmail, CancellationToken ct)
    {
        try
        {
            var result = await _api.Create()
                .WithEndpoint(ApiEndpoints.AuthTestEmail)
                .WithMethod(HttpMethod.Post)
                .WithJsonBody(new TestEmailDto { ToEmail = toEmail })
                .SendJsonAsync<ApiResponse<TestEmailResultDto>>(ct);

            if (result?.Data is null)
            {
                TempData["Error"] = result?.Message ?? "No se pudo probar el correo.";
                return RedirectToAction(nameof(Index));
            }

            var d = result.Data;
            if (d.Sent)
            {
                TempData["Success"] = $"Correo de prueba enviado a {d.ToEmail}.";
            }
            else
            {
                var hints = d.Diagnostics.Hints.Count > 0
                    ? " " + string.Join(" ", d.Diagnostics.Hints)
                    : "";
                TempData["Error"] =
                    $"No se envió el correo: {d.Error ?? result.Message}.{hints} " +
                    $"(Host={d.Diagnostics.Host}, User={d.Diagnostics.UserNameHint}, " +
                    $"Password={(d.Diagnostics.HasPassword ? "sí" : "no")})";
            }

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            TempData["Error"] = "Sin conexión con la API.";
            return RedirectToAction(nameof(Index));
        }
    }
}
