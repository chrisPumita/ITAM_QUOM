using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Catalog;
using ITAM.Shared.Dtos.Company;
using ITAM.Shared.Enums;
using ITAM.WebApp.ApiConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAM.WebApp.Controllers;

[Authorize(Roles = AppRoles.Administrador)]
public class CatalogosController : Controller
{
    private readonly ApiConnectFactory _api;

    public CatalogosController(ApiConnectFactory api) => _api = api;

    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(Marcas));

    // ——— Marcas ———
    [HttpGet]
    public async Task<IActionResult> Marcas(string? search, bool? onlyActive, CancellationToken ct)
    {
        var items = await LoadBrandsAsync(onlyActive, ct);
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(x => x.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        ViewBag.Search = search;
        ViewBag.OnlyActive = onlyActive;
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBrand(BrandUpsertDto dto, int? id, CancellationToken ct)
    {
        dto.Name = (dto.Name ?? "").Trim().ToUpperInvariant();
        var ok = await SaveAsync(ApiEndpoints.Brands, ApiEndpoints.BrandById, dto, id, ct);
        TempData[ok ? "Success" : "Error"] = ok ? "Marca guardada." : "No se pudo guardar la marca.";
        return RedirectToAction(nameof(Marcas));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBrand(int id, bool isActive, CancellationToken ct)
    {
        var get = await _api.Create().WithEndpoint(ApiEndpoints.BrandById).WithRoute("id", id)
            .SendJsonAsync<ApiResponse<BrandListDto>>(ct);
        if (get?.Data is null) { TempData["Error"] = "Marca no encontrada."; return RedirectToAction(nameof(Marcas)); }
        var dto = new BrandUpsertDto { Name = get.Data.Name, IsActive = isActive };
        var ok = await SaveAsync(ApiEndpoints.Brands, ApiEndpoints.BrandById, dto, id, ct);
        TempData[ok ? "Success" : "Error"] = ok ? (isActive ? "Marca activada." : "Marca dada de baja.") : "Error al cambiar estado.";
        return RedirectToAction(nameof(Marcas));
    }

    // ——— Modelos ———
    [HttpGet]
    public async Task<IActionResult> Modelos(string? search, int? brandId, int? categoryId, bool? onlyActive, CancellationToken ct)
    {
        var b = _api.Create().WithEndpoint(ApiEndpoints.Models);
        if (onlyActive == true) b.WithQuery("onlyActive", "true");
        if (brandId is > 0) b.WithQuery("brandId", brandId.Value.ToString());
        if (categoryId is > 0) b.WithQuery("categoryId", categoryId.Value.ToString());
        var items = (await b.SendJsonAsync<ApiResponse<List<ModelListDto>>>(ct))?.Data ?? [];
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(x =>
                x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.BrandName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        ViewBag.Brands = await LoadBrandsAsync(true, ct);
        ViewBag.Categories = await LoadCategoriesAsync(true, ct);
        ViewBag.Search = search;
        ViewBag.BrandId = brandId;
        ViewBag.CategoryId = categoryId;
        ViewBag.OnlyActive = onlyActive;
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveModel(ModelUpsertDto dto, int? id, CancellationToken ct)
    {
        var ok = await SaveAsync(ApiEndpoints.Models, ApiEndpoints.ModelById, dto, id, ct);
        TempData[ok ? "Success" : "Error"] = ok ? "Modelo guardado." : "No se pudo guardar el modelo.";
        return RedirectToAction(nameof(Modelos));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleModel(int id, bool isActive, CancellationToken ct)
    {
        var get = await _api.Create().WithEndpoint(ApiEndpoints.ModelById).WithRoute("id", id)
            .SendJsonAsync<ApiResponse<ModelListDto>>(ct);
        if (get?.Data is null) { TempData["Error"] = "Modelo no encontrado."; return RedirectToAction(nameof(Modelos)); }
        var d = get.Data;
        var dto = new ModelUpsertDto
        {
            Name = d.Name, Specs = d.Specs, BrandId = d.BrandId, CategoryId = d.CategoryId, IsActive = isActive
        };
        var ok = await SaveAsync(ApiEndpoints.Models, ApiEndpoints.ModelById, dto, id, ct);
        TempData[ok ? "Success" : "Error"] = ok ? (isActive ? "Modelo activado." : "Modelo dado de baja.") : "Error.";
        return RedirectToAction(nameof(Modelos));
    }

    // ——— Categorías ———
    [HttpGet]
    public async Task<IActionResult> Categorias(string? search, bool? onlyActive, CancellationToken ct)
    {
        var items = await LoadCategoriesAsync(onlyActive, ct);
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(x => x.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        ViewBag.Search = search;
        ViewBag.OnlyActive = onlyActive;
        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(CategoryUpsertDto dto, int? id, CancellationToken ct)
    {
        var ok = await SaveAsync(ApiEndpoints.Categories, ApiEndpoints.CategoryById, dto, id, ct);
        TempData[ok ? "Success" : "Error"] = ok ? "Categoría guardada." : "Error al guardar.";
        return RedirectToAction(nameof(Categorias));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCategory(int id, bool isActive, CancellationToken ct)
    {
        var get = await _api.Create().WithEndpoint(ApiEndpoints.CategoryById).WithRoute("id", id)
            .SendJsonAsync<ApiResponse<CategoryListDto>>(ct);
        if (get?.Data is null) { TempData["Error"] = "No encontrada."; return RedirectToAction(nameof(Categorias)); }
        var dto = new CategoryUpsertDto { Name = get.Data.Name, SortOrder = get.Data.SortOrder, IsActive = isActive, ParentCategoryId = get.Data.ParentCategoryId };
        var ok = await SaveAsync(ApiEndpoints.Categories, ApiEndpoints.CategoryById, dto, id, ct);
        TempData[ok ? "Success" : "Error"] = ok ? (isActive ? "Activada." : "Dada de baja.") : "Error.";
        return RedirectToAction(nameof(Categorias));
    }

    // ——— Ubicaciones ———
    [HttpGet]
    public async Task<IActionResult> Ubicaciones(string? search, bool? onlyActive, CancellationToken ct)
    {
        var b = _api.Create().WithEndpoint(ApiEndpoints.Locations);
        if (onlyActive == true) b.WithQuery("onlyActive", "true");
        var items = (await b.SendJsonAsync<ApiResponse<List<LocationListDto>>>(ct))?.Data ?? [];
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(x => x.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        ViewBag.Search = search;
        ViewBag.OnlyActive = onlyActive;
        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLocation(LocationUpsertDto dto, int? id, CancellationToken ct)
    {
        var ok = await SaveAsync(ApiEndpoints.Locations, ApiEndpoints.LocationById, dto, id, ct);
        TempData[ok ? "Success" : "Error"] = ok ? "Ubicación guardada." : "Error.";
        return RedirectToAction(nameof(Ubicaciones));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLocation(int id, bool isActive, CancellationToken ct)
    {
        var get = await _api.Create().WithEndpoint(ApiEndpoints.LocationById).WithRoute("id", id)
            .SendJsonAsync<ApiResponse<LocationListDto>>(ct);
        if (get?.Data is null) { TempData["Error"] = "No encontrada."; return RedirectToAction(nameof(Ubicaciones)); }
        var d = get.Data;
        var dto = new LocationUpsertDto { Name = d.Name, IsWarehouse = d.IsWarehouse, ParentLocationId = d.ParentLocationId, IsActive = isActive };
        var ok = await SaveAsync(ApiEndpoints.Locations, ApiEndpoints.LocationById, dto, id, ct);
        TempData[ok ? "Success" : "Error"] = ok ? (isActive ? "Activada." : "Dada de baja.") : "Error.";
        return RedirectToAction(nameof(Ubicaciones));
    }

    // ——— Empleados ———
    [HttpGet]
    public async Task<IActionResult> Empleados(string? search, bool? onlyActive, CancellationToken ct)
    {
        var b = _api.Create().WithEndpoint(ApiEndpoints.Employees)
            .WithQuery("onlyActive", onlyActive == true ? "true" : "false");
        var items = (await b.SendJsonAsync<ApiResponse<List<EmployeeListDto>>>(ct))?.Data ?? [];
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(x =>
                x.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.EmployeeNumber.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        ViewBag.Search = search;
        ViewBag.OnlyActive = onlyActive;
        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEmployee(EmployeeUpsertDto dto, Guid? id, CancellationToken ct)
    {
        var ok = await SaveGuidAsync(ApiEndpoints.Employees, ApiEndpoints.EmployeeById, dto, id, ct);
        TempData[ok ? "Success" : "Error"] = ok ? "Empleado guardado." : "Error.";
        return RedirectToAction(nameof(Empleados));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEmployee(Guid id, bool isActive, CancellationToken ct)
    {
        var get = await _api.Create().WithEndpoint(ApiEndpoints.EmployeeById).WithRoute("id", id)
            .SendJsonAsync<ApiResponse<EmployeeListDto>>(ct);
        if (get?.Data is null) { TempData["Error"] = "No encontrado."; return RedirectToAction(nameof(Empleados)); }
        var d = get.Data;
        var dto = new EmployeeUpsertDto
        {
            EmployeeNumber = d.EmployeeNumber, FullName = d.FullName, Email = d.Email,
            Department = d.Department, IsActive = isActive, IdentityUserId = d.IdentityUserId
        };
        var ok = await SaveGuidAsync(ApiEndpoints.Employees, ApiEndpoints.EmployeeById, dto, id, ct);
        TempData[ok ? "Success" : "Error"] = ok ? (isActive ? "Activado." : "Dado de baja.") : "Error.";
        return RedirectToAction(nameof(Empleados));
    }

    // ——— Proveedores ———
    [HttpGet]
    public async Task<IActionResult> Proveedores(string? search, bool? onlyActive, CancellationToken ct)
    {
        var b = _api.Create().WithEndpoint(ApiEndpoints.Suppliers)
            .WithQuery("onlyActive", onlyActive == true ? "true" : "false");
        var items = (await b.SendJsonAsync<ApiResponse<List<SupplierListDto>>>(ct))?.Data ?? [];
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(x => x.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        ViewBag.Search = search;
        ViewBag.OnlyActive = onlyActive;
        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSupplier(SupplierUpsertDto dto, Guid? id, CancellationToken ct)
    {
        var ok = await SaveGuidAsync(ApiEndpoints.Suppliers, ApiEndpoints.SupplierById, dto, id, ct);
        TempData[ok ? "Success" : "Error"] = ok ? "Proveedor guardado." : "Error.";
        return RedirectToAction(nameof(Proveedores));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSupplier(Guid id, bool isActive, CancellationToken ct)
    {
        var get = await _api.Create().WithEndpoint(ApiEndpoints.SupplierById).WithRoute("id", id)
            .SendJsonAsync<ApiResponse<SupplierListDto>>(ct);
        if (get?.Data is null) { TempData["Error"] = "No encontrado."; return RedirectToAction(nameof(Proveedores)); }
        var d = get.Data;
        var dto = new SupplierUpsertDto
        {
            Name = d.Name, Contact = d.Contact, Email = d.Email, Phone = d.Phone,
            OffersPurchase = d.OffersPurchase, OffersMaintenance = d.OffersMaintenance,
            OffersRental = d.OffersRental, IsActive = isActive
        };
        var ok = await SaveGuidAsync(ApiEndpoints.Suppliers, ApiEndpoints.SupplierById, dto, id, ct);
        TempData[ok ? "Success" : "Error"] = ok ? (isActive ? "Activado." : "Dado de baja.") : "Error.";
        return RedirectToAction(nameof(Proveedores));
    }

    // ——— helpers ———
    private async Task<List<BrandListDto>> LoadBrandsAsync(bool? onlyActive, CancellationToken ct)
    {
        var b = _api.Create().WithEndpoint(ApiEndpoints.Brands);
        if (onlyActive == true) b.WithQuery("onlyActive", "true");
        return (await b.SendJsonAsync<ApiResponse<List<BrandListDto>>>(ct))?.Data ?? [];
    }

    private async Task<List<CategoryListDto>> LoadCategoriesAsync(bool? onlyActive, CancellationToken ct)
    {
        var b = _api.Create().WithEndpoint(ApiEndpoints.Categories);
        if (onlyActive == true) b.WithQuery("onlyActive", "true");
        return (await b.SendJsonAsync<ApiResponse<List<CategoryListDto>>>(ct))?.Data ?? [];
    }

    private async Task<bool> SaveAsync<T>(string createKey, string byIdKey, T dto, int? id, CancellationToken ct)
    {
        try
        {
            if (id is > 0)
            {
                var r = await _api.Create().WithEndpoint(byIdKey).WithRoute("id", id.Value)
                    .WithMethod(HttpMethod.Put).WithJsonBody(dto!).SendJsonAsync<ApiResponse<bool>>(ct);
                return r is { IsSuccess: true };
            }
            var c = await _api.Create().WithEndpoint(createKey).WithMethod(HttpMethod.Post).WithJsonBody(dto!)
                .SendJsonAsync<ApiResponse<int>>(ct);
            return c is { IsSuccess: true };
        }
        catch { return false; }
    }

    private async Task<bool> SaveGuidAsync<T>(string createKey, string byIdKey, T dto, Guid? id, CancellationToken ct)
    {
        try
        {
            if (id is { } gid && gid != Guid.Empty)
            {
                var r = await _api.Create().WithEndpoint(byIdKey).WithRoute("id", gid)
                    .WithMethod(HttpMethod.Put).WithJsonBody(dto!).SendJsonAsync<ApiResponse<bool>>(ct);
                return r is { IsSuccess: true };
            }
            var c = await _api.Create().WithEndpoint(createKey).WithMethod(HttpMethod.Post).WithJsonBody(dto!)
                .SendJsonAsync<ApiResponse<Guid>>(ct);
            return c is { IsSuccess: true };
        }
        catch { return false; }
    }
}
