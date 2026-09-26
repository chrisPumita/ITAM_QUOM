using System.Diagnostics;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assets;
using ITAM.Shared.Enums;
using ITAM.WebApp.ApiConnect;
using ITAM.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.WebApp.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApiConnectFactory _api;

    public HomeController(ApiConnectFactory api) => _api = api;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var isAdmin = User.IsInRole(AppRoles.Administrador);
        var vm = new HomeDashboardViewModel { IsAdmin = isAdmin };

        if (isAdmin)
        {
            try
            {
                var resp = await _api.Create()
                    .WithEndpoint(ApiEndpoints.AssetsSummary)
                    .SendJsonAsync<ApiResponse<AssetSummaryDto>>(ct);

                if (resp is { IsSuccess: true, Data: not null })
                {
                    vm.SummaryLoaded = true;
                    vm.Nuevos = resp.Data.Nuevos;
                    vm.Disponibles = resp.Data.Disponibles;
                    vm.Asignados = resp.Data.Asignados;
                    vm.Mantenimiento = resp.Data.Mantenimiento;
                    vm.Baja = resp.Data.Baja;
                }
                else
                {
                    vm.SummaryError = resp?.Message ?? "No se pudo cargar el resumen.";
                }
            }
            catch
            {
                vm.SummaryError = "Sin conexión con la API.";
            }
        }

        return View(vm);
    }

    public IActionResult Privacy() => View();
}

/// <summary>Página de error anónima (fuera de Home [Authorize]) para no provocar bucles de login.</summary>
[AllowAnonymous]
public class ErrorController : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("Home/Error")]
    [Route("Error")]
    public IActionResult Index()
    {
        return View("~/Views/Shared/Error.cshtml",
            new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
