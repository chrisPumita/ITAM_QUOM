using System.Net;
using System.Text.Json;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Auth;
using ITAM.WebApp.ApiConnect;
using ITAM.WebApp.Models;
using ITAM.WebApp.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.WebApp.Controllers;

public class AccountController : Controller
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ApiConnectFactory _api;
    private readonly IWebAuthSession _auth;

    public AccountController(ApiConnectFactory api, IWebAuthSession auth)
    {
        _api = api;
        _auth = auth;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_auth.IsAuthenticated(User))
            return RedirectToLocal(returnUrl);

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (_auth.IsAuthenticated(User))
            return RedirectToLocal(model.ReturnUrl);

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            using var response = await _api.Create()
                .WithEndpoint(ApiEndpoints.AuthLogin)
                .WithMethod(HttpMethod.Post)
                .WithJsonBody(new LoginDto
                {
                    Email = model.Email.Trim(),
                    Password = model.Password
                })
                .SendAsync(ct);

            var body = await response.Content.ReadAsStringAsync(ct);
            var api = string.IsNullOrWhiteSpace(body)
                ? null
                : JsonSerializer.Deserialize<ApiResponse<LoginResponseDto>>(body, JsonOptions);

            if (response.StatusCode == HttpStatusCode.OK
                && api?.Data is { Token.Length: > 0 } login)
            {
                await _auth.SignInAsync(HttpContext, login, model.RememberMe);
                return RedirectToLocal(model.ReturnUrl);
            }

            var message = !string.IsNullOrWhiteSpace(api?.Message)
                ? api.Message
                : response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "Correo o contraseña incorrectos.",
                    HttpStatusCode.Forbidden => api?.Message ?? "Usuario bloqueado o inactivo.",
                    HttpStatusCode.TooManyRequests => "Demasiados intentos. Intente más tarde.",
                    _ => "No se pudo iniciar sesión. Verifique la conexión con la API."
                };

            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "No hay conexión con la API. Intente más tarde.");
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _auth.SignOutAsync(HttpContext);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordDto());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var result = await _api.Create()
                .WithEndpoint(ApiEndpoints.AuthChangePassword)
                .WithMethod(HttpMethod.Post)
                .WithJsonBody(model)
                .SendJsonAsync<ApiResponse<bool>>(ct);

            if (result is not { IsSuccess: true })
            {
                ModelState.AddModelError(string.Empty, result?.Message ?? "No se pudo cambiar la contraseña.");
                return View(model);
            }

            TempData["Success"] = "Contraseña actualizada.";
            return RedirectToAction("Index", "Home");
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Sin conexión con la API.");
            return View(model);
        }
    }

    [AllowAnonymous]
    [HttpGet("/Account/Logout")]
    public async Task<IActionResult> LogoutLink()
    {
        if (_auth.IsAuthenticated(User))
            await _auth.SignOutAsync(HttpContext);
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl)
            && Url.IsLocalUrl(returnUrl)
            && !returnUrl.Contains("/Home/Error", StringComparison.OrdinalIgnoreCase)
            && !returnUrl.Contains("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase)
            && !returnUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }
}
