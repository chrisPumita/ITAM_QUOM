using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assets;
using ITAM.Shared.Dtos.Assignments;
using ITAM.Shared.Dtos.Catalog;
using ITAM.Shared.Dtos.Company;
using ITAM.Shared.Enums;
using ITAM.WebApp.ApiConnect;
using ITAM.WebApp.Models.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.WebApp.Controllers;

[Authorize]
public class AssetsController : Controller
{
    private readonly ApiConnectFactory _api;

    public AssetsController(ApiConnectFactory api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? status,
        string? condition,
        AssetKind? kind,
        int? brandId,
        int? modelId,
        int? categoryId,
        bool showRetired = false,
        int page = 1,
        CancellationToken ct = default)
    {
        var isAdmin = User.IsInRole(AppRoles.Administrador);
        if (!isAdmin)
            showRetired = false;

        var builder = _api.Create()
            .WithEndpoint(ApiEndpoints.Assets)
            .WithQuery("search", search)
            .WithQuery("page", page.ToString())
            .WithQuery("pageSize", "100");

        if (!string.IsNullOrWhiteSpace(status))
            builder.WithQuery("status", status);
        else if (!showRetired)
            builder.WithQuery("statuses", new[] { "Available", "Assigned", "Maintenance" });

        if (!string.IsNullOrWhiteSpace(condition))
            builder.WithQuery("condition", condition);
        if (kind.HasValue)
            builder.WithQuery("kind", ((int)kind.Value).ToString());
        if (categoryId is > 0)
            builder.WithQuery("categoryIds", categoryId.Value.ToString());
        if (brandId is > 0)
            builder.WithQuery("brandId", brandId.Value.ToString());
        else
            modelId = null;
        if (modelId is > 0)
            builder.WithQuery("modelId", modelId.Value.ToString());

        ApiResponse<PagedResult<AssetListDto>>? listResp = null;
        try
        {
            listResp = await builder.SendJsonAsync<ApiResponse<PagedResult<AssetListDto>>>(ct);
        }
        catch
        {
            TempData["Error"] = "No hay conexión con la API.";
        }

        var vm = new AssetIndexViewModel
        {
            Search = search,
            Status = status,
            Condition = condition,
            Kind = kind,
            BrandId = brandId,
            ModelId = modelId,
            CategoryId = categoryId,
            ShowRetired = showRetired,
            Page = page,
            PageSize = 100,
            TotalCount = listResp?.Data?.TotalCount ?? 0,
            Items = listResp?.Data?.Items?.ToList() ?? []
        };

        await FillCatalogFiltersAsync(vm, ct);

        if (listResp is { IsSuccess: false })
            TempData["Error"] = listResp.Message;

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        string? search,
        string? status,
        string? condition,
        AssetKind? kind,
        int? brandId,
        int? modelId,
        int? categoryId,
        bool showRetired = false,
        CancellationToken ct = default)
    {
        var isAdmin = User.IsInRole(AppRoles.Administrador);
        if (!isAdmin)
            showRetired = false;

        var b = _api.Create()
            .WithEndpoint(ApiEndpoints.AssetsExport)
            .WithQuery("search", search)
            .WithQuery("condition", condition)
            .WithQuery("kind", kind.HasValue ? ((int)kind.Value).ToString() : null);

        if (categoryId is > 0)
            b.WithQuery("categoryIds", categoryId.Value.ToString());
        if (brandId is > 0)
            b.WithQuery("brandId", brandId.Value.ToString());
        else
            modelId = null;
        if (modelId is > 0)
            b.WithQuery("modelId", modelId.Value.ToString());

        if (!string.IsNullOrWhiteSpace(status))
            b.WithQuery("status", status);
        else if (!showRetired)
            b.WithQuery("statuses", new[] { "Available", "Assigned", "Maintenance" });

        var file = await b.SendFileAsync(ct);

        if (file is null)
        {
            TempData["Error"] = "No hay datos para exportar.";
            return RedirectToAction(nameof(Index));
        }

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var assetTask = _api.Create()
            .WithEndpoint(ApiEndpoints.AssetById)
            .WithRoute("id", id)
            .SendJsonAsync<ApiResponse<AssetListDto>>(ct);

        var movementsTask = _api.Create()
            .WithEndpoint(ApiEndpoints.AssignmentsMovements)
            .WithQuery("assetId", id.ToString())
            .SendJsonAsync<ApiResponse<List<AssetMovementListDto>>>(ct);

        await Task.WhenAll(assetTask, movementsTask);

        if (assetTask.Result is not { IsSuccess: true, Data: not null })
            return NotFound();

        return View(new AssetDetailViewModel
        {
            Asset = assetTask.Result.Data,
            Movements = movementsTask.Result?.Data?.OrderByDescending(m => m.OccurredAt).ToList() ?? [],
            IsAdmin = User.IsInRole(AppRoles.Administrador)
        });
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var vm = new AssetCreateViewModel
        {
            PurchaseDate = today,
            WarrantyEndDate = today.AddYears(1)
        };
        await FillCreateOptionsAsync(vm, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    public async Task<IActionResult> Create(AssetCreateViewModel vm, CancellationToken ct)
    {
        if (vm.Kind == AssetKind.Equipment && string.IsNullOrWhiteSpace(vm.SerialNumber))
            ModelState.AddModelError(nameof(vm.SerialNumber), "Serie obligatoria para equipos.");

        if (vm.OwnershipType == OwnershipType.Rented && vm.SupplierId is null)
            ModelState.AddModelError(nameof(vm.SupplierId), "Un activo rentado requiere proveedor.");

        if (!ModelState.IsValid)
        {
            await FillCreateOptionsAsync(vm, ct);
            return View(vm);
        }

        var dto = new AssetUpsertDto
        {
            Kind = vm.Kind,
            ModelId = vm.ModelId,
            SerialNumber = string.IsNullOrWhiteSpace(vm.SerialNumber) ? null : vm.SerialNumber.Trim(),
            OwnershipType = vm.OwnershipType,
            SupplierId = vm.SupplierId,
            Status = AssetStatus.Available,
            LocationId = vm.LocationId,
            PurchaseDate = vm.PurchaseDate,
            RentalEndDate = vm.RentalEndDate,
            WarrantyEndDate = vm.WarrantyEndDate,
            Imei = string.IsNullOrWhiteSpace(vm.Imei) ? null : vm.Imei.Trim(),
            ContractNumber = string.IsNullOrWhiteSpace(vm.ContractNumber) ? null : vm.ContractNumber.Trim()
        };

        try
        {
            var result = await _api.Create()
                .WithEndpoint(ApiEndpoints.Assets)
                .WithMethod(HttpMethod.Post)
                .WithJsonBody(dto)
                .SendJsonAsync<ApiResponse<Guid>>(ct);

            if (result is not { IsSuccess: true, Data: var id })
            {
                ModelState.AddModelError(string.Empty, result?.Message ?? "No se pudo registrar.");
                await FillCreateOptionsAsync(vm, ct);
                return View(vm);
            }

            TempData["Success"] = "Activo registrado.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Sin conexión con la API.");
            await FillCreateOptionsAsync(vm, ct);
            return View(vm);
        }
    }

    public record QuickBrandRequest(string Name);
    public record QuickModelRequest(string Name, int BrandId, int CategoryId, string? Specs);

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    public async Task<IActionResult> QuickCreateBrand([FromBody] QuickBrandRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Json(new { ok = false, message = "Nombre requerido." });

        var dto = new BrandUpsertDto { Name = req.Name.Trim(), IsActive = true };
        try
        {
            var result = await _api.Create()
                .WithEndpoint(ApiEndpoints.Brands)
                .WithMethod(HttpMethod.Post)
                .WithJsonBody(dto)
                .SendJsonAsync<ApiResponse<int>>(ct);

            if (result is not { IsSuccess: true, Data: var id })
                return Json(new { ok = false, message = result?.Message ?? "No se pudo crear la marca." });

            return Json(new { ok = true, id, name = req.Name.Trim().ToUpperInvariant() });
        }
        catch
        {
            return Json(new { ok = false, message = "Sin conexión con la API." });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    public async Task<IActionResult> QuickCreateModel([FromBody] QuickModelRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || req.BrandId <= 0 || req.CategoryId <= 0)
            return Json(new { ok = false, message = "Marca, categoría y nombre son obligatorios." });

        var dto = new ModelUpsertDto
        {
            Name = req.Name.Trim(),
            BrandId = req.BrandId,
            CategoryId = req.CategoryId,
            Specs = string.IsNullOrWhiteSpace(req.Specs) ? null : req.Specs.Trim(),
            IsActive = true
        };

        try
        {
            var result = await _api.Create()
                .WithEndpoint(ApiEndpoints.Models)
                .WithMethod(HttpMethod.Post)
                .WithJsonBody(dto)
                .SendJsonAsync<ApiResponse<int>>(ct);

            if (result is not { IsSuccess: true, Data: var id })
                return Json(new { ok = false, message = result?.Message ?? "No se pudo crear el modelo." });

            var brand = await _api.Create().WithEndpoint(ApiEndpoints.BrandById).WithRoute("id", req.BrandId)
                .SendJsonAsync<ApiResponse<BrandListDto>>(ct);
            var cat = await _api.Create().WithEndpoint(ApiEndpoints.CategoryById).WithRoute("id", req.CategoryId)
                .SendJsonAsync<ApiResponse<CategoryListDto>>(ct);

            return Json(new
            {
                ok = true,
                id,
                name = dto.Name,
                brandId = req.BrandId,
                brandName = brand?.Data?.Name ?? "",
                categoryName = cat?.Data?.Name ?? ""
            });
        }
        catch
        {
            return Json(new { ok = false, message = "Sin conexión con la API." });
        }
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    public IActionResult Import() => View(new AssetImportViewModel());

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    public async Task<IActionResult> ImportTemplate(CancellationToken ct)
    {
        var file = await _api.Create()
            .WithEndpoint(ApiEndpoints.AssetsImportTemplate)
            .SendFileAsync(ct);
        if (file is null)
        {
            TempData["Error"] = "No se pudo descargar la plantilla.";
            return RedirectToAction(nameof(Import));
        }

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        var vm = new AssetImportViewModel();
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Seleccione un archivo Excel.");
            return View(vm);
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _api.Create()
                .WithEndpoint(ApiEndpoints.AssetsImport)
                .WithMethod(HttpMethod.Post)
                .WithMultipartFile("file", stream, file.FileName, file.ContentType)
                .SendJsonAsync<ApiResponse<AssetImportResultDto>>(ct);

            if (result is { IsSuccess: true, Data: not null })
            {
                vm.Created = result.Data.Created;
                vm.Failed = result.Data.Failed;
                vm.Errors = result.Data.Errors;
                vm.CreatedCodes = result.Data.CreatedCodes;
                vm.ResultMessage = result.Message;
                TempData["Success"] = result.Message;
            }
            else
            {
                ModelState.AddModelError(string.Empty, result?.Message ?? "Importación fallida.");
            }
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Sin conexión con la API.");
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    public async Task<IActionResult> SetStatus(Guid id, AssetStatus status, string? returnTo, CancellationToken ct)
    {
        var isAdmin = User.IsInRole(AppRoles.Administrador);
        if (!isAdmin && status is not (AssetStatus.Available or AssetStatus.Maintenance))
            return Forbid();

        var get = await _api.Create()
            .WithEndpoint(ApiEndpoints.AssetById)
            .WithRoute("id", id)
            .SendJsonAsync<ApiResponse<AssetListDto>>(ct);

        if (get is not { IsSuccess: true, Data: not null })
        {
            TempData["Error"] = "Activo no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        var a = get.Data;
        var dto = new AssetUpsertDto
        {
            AssetCode = a.AssetCode,
            SerialNumber = a.SerialNumber,
            Kind = a.Kind,
            ModelId = a.ModelId,
            OwnershipType = a.OwnershipType,
            SupplierId = a.SupplierId,
            Status = status,
            LocationId = a.LocationId,
            PurchaseDate = a.PurchaseDate,
            RentalEndDate = a.RentalEndDate,
            WarrantyEndDate = a.WarrantyEndDate,
            Imei = a.Imei,
            ContractNumber = a.ContractNumber
        };

        var result = await _api.Create()
            .WithEndpoint(ApiEndpoints.AssetById)
            .WithRoute("id", id)
            .WithMethod(HttpMethod.Put)
            .WithJsonBody(dto)
            .SendJsonAsync<ApiResponse<bool>>(ct);

        TempData[result is { IsSuccess: true } ? "Success" : "Error"] =
            result?.Message ?? "No se pudo actualizar el estado.";

        if (string.Equals(returnTo, "Index", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index));

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    public async Task<IActionResult> SetMaintenance(Guid id, string? returnTo, CancellationToken ct)
        => await SetStatus(id, AssetStatus.Maintenance, returnTo, ct);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Administrador},{AppRoles.Operador}")]
    public async Task<IActionResult> ExitMaintenance(Guid id, string? returnTo, CancellationToken ct)
        => await SetStatus(id, AssetStatus.Available, returnTo, ct);

    private async Task FillCatalogFiltersAsync(AssetIndexViewModel vm, CancellationToken ct)
    {
        try
        {
            var brands = await _api.Create().WithEndpoint(ApiEndpoints.Brands)
                .WithQuery("onlyActive", "true")
                .SendJsonAsync<ApiResponse<List<BrandListDto>>>(ct);
            var models = await _api.Create().WithEndpoint(ApiEndpoints.Models)
                .WithQuery("onlyActive", "true")
                .SendJsonAsync<ApiResponse<List<ModelListDto>>>(ct);
            var categories = await _api.Create().WithEndpoint(ApiEndpoints.Categories)
                .WithQuery("onlyActive", "true")
                .SendJsonAsync<ApiResponse<List<CategoryListDto>>>(ct);
            vm.Brands = brands?.Data ?? [];
            vm.Models = models?.Data ?? [];
            vm.Categories = categories?.Data ?? [];
        }
        catch
        {
            // empty
        }
    }

    private async Task FillCreateOptionsAsync(AssetCreateViewModel vm, CancellationToken ct)
    {
        try
        {
            var brands = await _api.Create().WithEndpoint(ApiEndpoints.Brands)
                .WithQuery("onlyActive", "true")
                .SendJsonAsync<ApiResponse<List<BrandListDto>>>(ct);
            var models = await _api.Create().WithEndpoint(ApiEndpoints.Models)
                .WithQuery("onlyActive", "true")
                .SendJsonAsync<ApiResponse<List<ModelListDto>>>(ct);
            var categories = await _api.Create().WithEndpoint(ApiEndpoints.Categories)
                .WithQuery("onlyActive", "true")
                .SendJsonAsync<ApiResponse<List<CategoryListDto>>>(ct);
            var locations = await _api.Create().WithEndpoint(ApiEndpoints.Locations)
                .SendJsonAsync<ApiResponse<List<LocationListDto>>>(ct);
            var suppliers = await _api.Create().WithEndpoint(ApiEndpoints.Suppliers)
                .WithQuery("onlyActive", "true")
                .SendJsonAsync<ApiResponse<List<SupplierListDto>>>(ct);

            vm.Brands = brands?.Data ?? [];
            vm.Models = models?.Data ?? [];
            vm.Categories = categories?.Data ?? [];
            vm.Locations = locations?.Data ?? [];
            vm.Suppliers = suppliers?.Data ?? [];
        }
        catch
        {
            // options vacías
        }
    }
}
