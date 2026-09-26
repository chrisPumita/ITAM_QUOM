using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assets;
using ITAM.Shared.Dtos.Assignments;
using ITAM.Shared.Dtos.Catalog;
using ITAM.Shared.Dtos.Company;
using ITAM.Shared.Enums;
using ITAM.WebApp.ApiConnect;
using ITAM.WebApp.Models.Assignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.WebApp.Controllers;

[Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
public class AssignmentsController : Controller
{
    private readonly ApiConnectFactory _api;

    public AssignmentsController(ApiConnectFactory api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Assign(
        string? search,
        AssetKind? kind,
        AssetCondition? condition,
        int? brandId,
        int? modelId,
        CancellationToken ct)
    {
        var vm = new AssignViewModel
        {
            Search = search,
            Kind = kind,
            Condition = condition,
            BrandId = brandId,
            ModelId = modelId
        };
        await FillAssignAsync(vm, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignViewModel vm, CancellationToken ct)
    {
        if (vm.EmployeeId is null || vm.EmployeeId == Guid.Empty)
            ModelState.AddModelError(nameof(vm.EmployeeId), "Seleccione un colaborador.");
        if (vm.SelectedAssetIds is null || vm.SelectedAssetIds.Count == 0)
            ModelState.AddModelError(nameof(vm.SelectedAssetIds), "Seleccione al menos un activo.");

        if (!ModelState.IsValid)
        {
            await FillAssignAsync(vm, ct);
            return View(vm);
        }

        var available = await LoadAvailableAsync(vm, ct);
        var selected = vm.SelectedAssetIds ?? [];
        var lines = selected.Select(id =>
        {
            var asset = available.FirstOrDefault(a => a.Id == id);
            return new AssignAssetLineDto
            {
                AssetId = id,
                Quantity = 1,
                ConditionOnDelivery = asset?.Condition ?? AssetCondition.Used
            };
        }).ToList();

        var dto = new AssignAssetsDto
        {
            EmployeeId = vm.EmployeeId!.Value,
            Notes = vm.Notes,
            Lines = lines
        };

        try
        {
            var result = await _api.Create()
                .WithEndpoint(ApiEndpoints.AssignmentsAssign)
                .WithMethod(HttpMethod.Post)
                .WithJsonBody(dto)
                .SendJsonAsync<ApiResponse<AssignAssetsResultDto>>(ct);

            if (result is { IsSuccess: true, Data: not null })
            {
                TempData["Success"] = $"Asignación OK. Folio {result.Data.Folio}.";
                return RedirectToAction(nameof(Assign));
            }

            ModelState.AddModelError(string.Empty, result?.Message ?? "No se pudo asignar.");
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Sin conexión con la API.");
        }

        await FillAssignAsync(vm, ct);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Return(string? mode, string? folio, string? assetCode, CancellationToken ct)
    {
        var vm = new ReturnViewModel
        {
            Mode = string.IsNullOrWhiteSpace(mode) ? "folio" : mode,
            Folio = folio,
            AssetCode = assetCode
        };
        await ResolveReturnAsync(vm, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(ReturnViewModel vm, CancellationToken ct)
    {
        if (vm.SelectedAssetIds is null || vm.SelectedAssetIds.Count == 0)
        {
            ModelState.AddModelError(nameof(vm.SelectedAssetIds), "Seleccione al menos un ítem.");
            await ResolveReturnAsync(vm, ct);
            return View(vm);
        }

        var ok = 0;
        var errors = new List<string>();
        foreach (var assetId in vm.SelectedAssetIds.Distinct())
        {
            try
            {
                var result = await _api.Create()
                    .WithEndpoint(ApiEndpoints.AssignmentsReturn)
                    .WithMethod(HttpMethod.Post)
                    .WithJsonBody(new ReturnAssetDto
                    {
                        AssetId = assetId,
                        ReturnCondition = AssetCondition.Used,
                        Notes = vm.Notes
                    })
                    .SendJsonAsync<ApiResponse<ReturnAssetResultDto>>(ct);

                if (result is { IsSuccess: true })
                    ok++;
                else
                    errors.Add(result?.Message ?? assetId.ToString());
            }
            catch
            {
                errors.Add($"Error de red en {assetId}");
            }
        }

        if (ok > 0)
            TempData["Success"] = $"Devueltos: {ok}.";
        if (errors.Count > 0)
            TempData["Error"] = string.Join(" · ", errors.Take(3));

        return RedirectToAction(nameof(Return), new { mode = vm.Mode, folio = vm.Folio, assetCode = vm.AssetCode });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendToWarranty(string assetCode, CancellationToken ct)
    {
        var assetResp = await _api.Create()
            .WithEndpoint(ApiEndpoints.AssetByCode)
            .WithRoute("code", assetCode)
            .SendJsonAsync<ApiResponse<AssetListDto>>(ct);

        if (assetResp is not { IsSuccess: true, Data: not null })
        {
            TempData["Error"] = assetResp?.Message ?? "Activo no encontrado.";
            return RedirectToAction(nameof(Return), new { mode = "code", assetCode });
        }

        var a = assetResp.Data;
        if (a.Status == AssetStatus.Assigned)
        {
            TempData["Error"] = "Primero devuelva el activo; luego envíelo a garantía.";
            return RedirectToAction(nameof(Return), new { mode = "code", assetCode });
        }

        if (a.Status is not AssetStatus.Available)
        {
            TempData["Error"] = "Solo se puede enviar a garantía un activo disponible.";
            return RedirectToAction(nameof(Return), new { mode = "code", assetCode });
        }

        var dto = new AssetUpsertDto
        {
            AssetCode = a.AssetCode,
            SerialNumber = a.SerialNumber,
            Kind = a.Kind,
            ModelId = a.ModelId,
            OwnershipType = a.OwnershipType,
            SupplierId = a.SupplierId,
            Status = AssetStatus.Maintenance,
            LocationId = a.LocationId,
            PurchaseDate = a.PurchaseDate,
            RentalEndDate = a.RentalEndDate,
            WarrantyEndDate = a.WarrantyEndDate,
            Imei = a.Imei,
            ContractNumber = a.ContractNumber
        };

        var result = await _api.Create()
            .WithEndpoint(ApiEndpoints.AssetById)
            .WithRoute("id", a.Id)
            .WithMethod(HttpMethod.Put)
            .WithJsonBody(dto)
            .SendJsonAsync<ApiResponse<bool>>(ct);

        TempData[result is { IsSuccess: true } ? "Success" : "Error"] =
            result?.Message ?? "No se pudo enviar a garantía.";
        return RedirectToAction("Details", "Assets", new { id = a.Id });
    }

    [HttpGet]
    public async Task<IActionResult> CustodyPdf(Guid id, CancellationToken ct)
    {
        var file = await _api.Create()
            .WithEndpoint(ApiEndpoints.AssignmentsCustodyPdf)
            .WithRoute("id", id)
            .SendFileAsync(ct);
        if (file is null)
        {
            TempData["Error"] = "No se pudo descargar el PDF.";
            return RedirectToAction(nameof(Assign));
        }

        return File(file.Content, file.ContentType, file.FileName);
    }

    private async Task FillAssignAsync(AssignViewModel vm, CancellationToken ct)
    {
        try
        {
            var emp = await _api.Create()
                .WithEndpoint(ApiEndpoints.Employees)
                .WithQuery("onlyActive", "true")
                .SendJsonAsync<ApiResponse<List<EmployeeListDto>>>(ct);
            var brands = await _api.Create()
                .WithEndpoint(ApiEndpoints.Brands)
                .WithQuery("onlyActive", "true")
                .SendJsonAsync<ApiResponse<List<BrandListDto>>>(ct);
            var models = await _api.Create()
                .WithEndpoint(ApiEndpoints.Models)
                .WithQuery("onlyActive", "true")
                .SendJsonAsync<ApiResponse<List<ModelListDto>>>(ct);

            vm.Employees = emp?.Data ?? [];
            vm.Brands = brands?.Data ?? [];
            vm.Models = models?.Data ?? [];
            vm.AvailableAssets = await LoadAvailableAsync(vm, ct);
        }
        catch
        {
            // empty
        }
    }

    private async Task<List<AssetListDto>> LoadAvailableAsync(AssignViewModel vm, CancellationToken ct)
    {
        var b = _api.Create()
            .WithEndpoint(ApiEndpoints.Assets)
            .WithQuery("status", "Available")
            .WithQuery("pageSize", "100")
            .WithQuery("search", vm.Search);
        if (vm.Kind.HasValue)
            b.WithQuery("kind", ((int)vm.Kind.Value).ToString());
        if (vm.Condition.HasValue)
            b.WithQuery("condition", vm.Condition.Value.ToString());
        if (vm.BrandId is > 0)
            b.WithQuery("brandId", vm.BrandId.Value.ToString());
        if (vm.ModelId is > 0)
            b.WithQuery("modelId", vm.ModelId.Value.ToString());

        var resp = await b.SendJsonAsync<ApiResponse<PagedResult<AssetListDto>>>(ct);
        return resp?.Data?.Items?.ToList() ?? [];
    }

    private async Task ResolveReturnAsync(ReturnViewModel vm, CancellationToken ct)
    {
        try
        {
            if (string.Equals(vm.Mode, "folio", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(vm.Folio))
            {
                var list = await _api.Create()
                    .WithEndpoint(ApiEndpoints.AssignmentsCustody)
                    .SendJsonAsync<ApiResponse<List<CustodyFormListDto>>>(ct);

                var match = list?.Data?.FirstOrDefault(c =>
                    string.Equals(c.Folio, vm.Folio.Trim(), StringComparison.OrdinalIgnoreCase));

                if (match is null)
                {
                    ModelState.AddModelError(nameof(vm.Folio), "Responsiva no encontrada.");
                    return;
                }

                var detail = await _api.Create()
                    .WithEndpoint(ApiEndpoints.AssignmentsCustodyById)
                    .WithRoute("id", match.Id)
                    .SendJsonAsync<ApiResponse<CustodyFormDetailDto>>(ct);

                vm.Custody = detail?.Data;
                if (vm.Custody is not null)
                {
                    foreach (var line in vm.Custody.Lines)
                    {
                        var asset = await _api.Create()
                            .WithEndpoint(ApiEndpoints.AssetById)
                            .WithRoute("id", line.AssetId)
                            .SendJsonAsync<ApiResponse<AssetListDto>>(ct);
                        if (asset?.Data?.Status == AssetStatus.Assigned)
                        {
                            vm.ActiveLines.Add(new AssignmentListDto
                            {
                                AssetId = line.AssetId,
                                AssetCode = line.AssetCode,
                                SerialNumber = line.SerialNumber,
                                AssetKind = line.AssetKind,
                                BrandName = line.BrandName,
                                ModelName = line.ModelName,
                                Specs = line.Specs,
                                EmployeeName = vm.Custody.EmployeeName
                            });
                        }
                    }
                }
            }
            else if (string.Equals(vm.Mode, "code", StringComparison.OrdinalIgnoreCase)
                     && !string.IsNullOrWhiteSpace(vm.AssetCode))
            {
                var asset = await _api.Create()
                    .WithEndpoint(ApiEndpoints.AssetByCode)
                    .WithRoute("code", vm.AssetCode.Trim())
                    .SendJsonAsync<ApiResponse<AssetListDto>>(ct);

                vm.LookupAsset = asset?.Data;
                if (vm.LookupAsset is null)
                    ModelState.AddModelError(nameof(vm.AssetCode), asset?.Message ?? "No encontrado.");
                else if (vm.LookupAsset.Status == AssetStatus.Assigned)
                {
                    vm.ActiveLines.Add(new AssignmentListDto
                    {
                        AssetId = vm.LookupAsset.Id,
                        AssetCode = vm.LookupAsset.AssetCode,
                        SerialNumber = vm.LookupAsset.SerialNumber,
                        AssetKind = vm.LookupAsset.Kind,
                        BrandName = vm.LookupAsset.BrandName,
                        ModelName = vm.LookupAsset.ModelName,
                        Specs = vm.LookupAsset.Specs,
                        EmployeeName = vm.LookupAsset.CurrentEmployeeName ?? ""
                    });
                }
            }
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Sin conexión con la API.");
        }
    }
}
