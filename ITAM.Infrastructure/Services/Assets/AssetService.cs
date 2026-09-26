using ITAM.Domain.Entities.Assets;
using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Interfaces.Repositories.Assets;
using ITAM.Domain.Interfaces.Repositories.Catalog;
using ITAM.Domain.Interfaces.Repositories.Company;
using ITAM.Domain.Interfaces.Services;
using ITAM.Domain.Interfaces.Services.Assets;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assets;
using ITAM.Shared.Enums;

namespace ITAM.Infrastructure.Services.Assets;

public class AssetService : IAssetService
{
    private readonly IAssetRepository _repo;
    private readonly IFolioCounterService _folios;
    private readonly IBrandRepository _brands;
    private readonly ICategoryRepository _categories;
    private readonly IModelRepository _models;
    private readonly ILocationRepository _locations;
    private readonly ISupplierRepository _suppliers;

    public AssetService(
        IAssetRepository repo,
        IFolioCounterService folios,
        IBrandRepository brands,
        ICategoryRepository categories,
        IModelRepository models,
        ILocationRepository locations,
        ISupplierRepository suppliers)
    {
        _repo = repo;
        _folios = folios;
        _brands = brands;
        _categories = categories;
        _models = models;
        _locations = locations;
        _suppliers = suppliers;
    }

    public async Task<Result<PagedResult<AssetListDto>>> ListAsync(AssetListQuery query)
    {
        query.Normalize();

        var filterResult = TryBuildFilter(query);
        if (!filterResult.IsSuccess)
            return Fail<PagedResult<AssetListDto>>(filterResult.Message, filterResult.Error!);

        var (items, total) = await _repo.SearchAsync(filterResult.Data!, query.Page, query.PageSize);

        return Ok(new PagedResult<AssetListDto>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        }, "Activos obtenidos.");
    }

    public async Task<Result<AssetSummaryDto>> GetSummaryAsync()
    {
        var data = await _repo.GetSummaryAsync();
        return Ok(data, "OK");
    }

    public async Task<Result<AssetListDto>> GetAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<AssetListDto>("Activo no encontrado.", "NotFound");
        return Ok(Map(entity), "OK");
    }

    public async Task<Result<AssetListDto>> GetByCodeAsync(string assetCode)
    {
        if (string.IsNullOrWhiteSpace(assetCode))
            return Fail<AssetListDto>("Código de activo requerido.", "Validation");

        var entity = await _repo.GetByCodeAsync(assetCode.Trim());
        if (entity is null)
            return Fail<AssetListDto>("Activo no encontrado.", "NotFound");
        return Ok(Map(entity), "OK");
    }

    public async Task<Result<Guid>> CreateAsync(AssetUpsertDto dto, Guid performedByUserId)
    {
        if (performedByUserId == Guid.Empty)
            return Fail<Guid>("Usuario autenticado requerido.", "Validation");

        // Alta: siempre Available + New; código auto si vacío.
        dto.Status = AssetStatus.Available;

        if (dto.Kind == AssetKind.Equipment && string.IsNullOrWhiteSpace(dto.SerialNumber))
            return Fail<Guid>("El número de serie es obligatorio para equipos.", "Validation");

        if (string.IsNullOrWhiteSpace(dto.AssetCode))
            dto.AssetCode = await _folios.NextAsync(FolioPrefixes.ForKind(dto.Kind));

        var validation = await ValidateAsync(dto, excludeId: null, isCreate: true);
        if (validation is not null)
            return validation;

        var entity = MapToEntity(dto, new Asset());
        entity.Condition = AssetCondition.New;
        entity.Status = AssetStatus.Available;

        var now = DateTime.UtcNow;
        var audit = new AssetMovement
        {
            AssetId = entity.Id,
            MovementType = MovementType.Created,
            FromStatus = null,
            ToStatus = entity.Status,
            FromLocationId = null,
            ToLocationId = entity.LocationId,
            PerformedByUserId = performedByUserId,
            Notes = $"Alta de activo {entity.AssetCode}",
            OccurredAt = now
        };

        await _repo.AddAsync(entity, audit);
        return Ok(entity.Id, "Activo creado.");
    }

    public async Task<Result<bool>> UpdateAsync(Guid id, AssetUpsertDto dto, Guid performedByUserId, bool isAdmin)
    {
        if (performedByUserId == Guid.Empty)
            return Fail<bool>("Usuario autenticado requerido.", "Validation");

        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<bool>("Activo no encontrado.", "NotFound");

        if (entity.Status == AssetStatus.Retired && dto.Status == AssetStatus.Retired)
            return Fail<bool>("Un activo dado de baja no se edita. Reactívelo primero.", "Validation");

        if (entity.Status == AssetStatus.Assigned && dto.Status != AssetStatus.Assigned)
            return Fail<bool>("Un activo asignado solo se libera con devolución.", "Validation");

        if (entity.Status != AssetStatus.Assigned && dto.Status == AssetStatus.Assigned)
            return Fail<bool>("Use el flujo de asignación para marcar Asignado.", "Validation");

        var transitionError = ValidateStatusTransition(entity.Status, dto.Status, isAdmin);
        if (transitionError is not null)
            return Fail<bool>(transitionError, "Validation");

        // Código: no regenerar; mantener el existente si el cliente manda vacío.
        if (string.IsNullOrWhiteSpace(dto.AssetCode))
            dto.AssetCode = entity.AssetCode;
        else if (!isAdmin && !string.Equals(dto.AssetCode.Trim(), entity.AssetCode, StringComparison.OrdinalIgnoreCase))
            return Fail<bool>("Solo un administrador puede cambiar el código de etiqueta.", "Validation");

        if (entity.Kind == AssetKind.Equipment || dto.Kind == AssetKind.Equipment)
        {
            if (string.IsNullOrWhiteSpace(dto.SerialNumber))
                return Fail<bool>("El número de serie es obligatorio para equipos.", "Validation");
        }

        var validation = await ValidateAsync(dto, excludeId: id, isCreate: false);
        if (validation is not null)
            return Fail<bool>(validation.Message, validation.Error);

        var fromStatus = entity.Status;
        var fromLocationId = entity.LocationId;
        var previousCondition = entity.Condition;

        MapToEntity(dto, entity);
        // Condition nunca vuelve a New por update.
        entity.Condition = previousCondition;
        if (dto.Status == AssetStatus.Available && fromStatus == AssetStatus.Retired)
            entity.Condition = AssetCondition.Used;

        entity.UpdatedAt = DateTime.UtcNow;

        var audits = BuildUpdateAudits(entity, fromStatus, fromLocationId, performedByUserId);
        await _repo.UpdateAsync(entity, audits);
        return Ok(true, "Activo actualizado.");
    }

    public async Task<Result<AssetImportResultDto>> ImportAsync(
        AssetImportRequestDto request,
        Guid performedByUserId)
    {
        if (performedByUserId == Guid.Empty)
            return Fail<AssetImportResultDto>("Usuario autenticado requerido.", "Validation");

        if (request.Rows is null || request.Rows.Count == 0)
            return Fail<AssetImportResultDto>("No hay filas para importar.", "Validation");

        var result = new AssetImportResultDto();
        foreach (var row in request.Rows)
        {
            try
            {
                var resolved = await ResolveImportRowAsync(row);
                if (!resolved.IsSuccess)
                {
                    result.Failed++;
                    result.Errors.Add($"Fila {row.RowNumber}: {resolved.Message}");
                    continue;
                }

                var dto = new AssetUpsertDto
                {
                    Kind = row.Kind,
                    ModelId = resolved.Data!.ModelId,
                    SerialNumber = row.SerialNumber,
                    OwnershipType = row.OwnershipType,
                    SupplierId = resolved.Data.SupplierId,
                    Status = AssetStatus.Available,
                    LocationId = resolved.Data.LocationId,
                    PurchaseDate = row.PurchaseDate,
                    WarrantyEndDate = row.WarrantyEndDate,
                    Imei = row.Imei,
                    ContractNumber = row.ContractNumber
                };

                var created = await CreateAsync(dto, performedByUserId);
                if (created.IsSuccess)
                {
                    result.Created++;
                    var asset = await _repo.GetByIdAsync(created.Data);
                    if (asset is not null)
                        result.CreatedCodes.Add(asset.AssetCode);
                }
                else
                {
                    result.Failed++;
                    result.Errors.Add($"Fila {row.RowNumber}: {created.Message}");
                }
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"Fila {row.RowNumber}: {ex.Message}");
            }
        }

        return Ok(result, result.Failed == 0
            ? $"Se crearon {result.Created} activos."
            : $"Creados {result.Created}, con errores {result.Failed}.");
    }

    private async Task<Result<AssetImportRowDto>> ResolveImportRowAsync(AssetImportRowDto row)
    {
        if (string.IsNullOrWhiteSpace(row.BrandName) || string.IsNullOrWhiteSpace(row.ModelName))
            return Fail<AssetImportRowDto>("Marca y Modelo son obligatorios.", "Validation");

        var brandName = row.BrandName.Trim().ToUpperInvariant();
        var brand = await _brands.FindByNameAsync(brandName);
        if (brand is null)
        {
            brand = await _brands.AddAsync(new Brand { Name = brandName, IsActive = true });
        }

        var categoryName = string.IsNullOrWhiteSpace(row.CategoryName) ? "General" : row.CategoryName.Trim();
        var category = await _categories.FindByNameAsync(categoryName);
        if (category is null)
        {
            category = await _categories.AddAsync(new Category
            {
                Name = categoryName,
                SortOrder = 100,
                IsActive = true
            });
        }

        var modelName = row.ModelName.Trim();
        var model = await _models.FindByBrandAndNameAsync(brand.Id, modelName);
        if (model is null)
        {
            model = await _models.AddAsync(new Model
            {
                Name = modelName,
                Specs = string.IsNullOrWhiteSpace(row.Specs) ? null : row.Specs.Trim(),
                BrandId = brand.Id,
                CategoryId = category.Id,
                IsActive = true
            });
        }

        row.ModelId = model.Id;

        if (!string.IsNullOrWhiteSpace(row.LocationName))
        {
            var loc = await _locations.FindByNameAsync(row.LocationName);
            if (loc is null)
                return Fail<AssetImportRowDto>($"Ubicación '{row.LocationName}' no existe. Créela en Catálogos.", "Validation");
            row.LocationId = loc.Id;
        }

        if (row.OwnershipType == OwnershipType.Rented)
        {
            if (string.IsNullOrWhiteSpace(row.SupplierName))
                return Fail<AssetImportRowDto>("Propiedad Rentado requiere Proveedor.", "Validation");
            var sup = await _suppliers.FindByNameAsync(row.SupplierName);
            if (sup is null)
                return Fail<AssetImportRowDto>($"Proveedor '{row.SupplierName}' no existe. Créelo en Catálogos.", "Validation");
            row.SupplierId = sup.Id;
        }
        else if (!string.IsNullOrWhiteSpace(row.SupplierName))
        {
            var sup = await _suppliers.FindByNameAsync(row.SupplierName);
            if (sup is not null)
                row.SupplierId = sup.Id;
        }

        return Ok(row, "OK");
    }

    /// <summary>Parsea facetas de status; el resto ya viene tipado en el query.</summary>
    internal static Result<AssetFilterCriteria> TryBuildFilter(AssetListQuery query)
    {
        List<AssetStatus>? statuses = null;
        if (query.Statuses is { Length: > 0 })
        {
            statuses = [];
            foreach (var raw in query.Statuses)
            {
                if (!EnumDisplayHelper.TryParseAssetStatus(raw, out var parsed))
                {
                    return new Result<AssetFilterCriteria>
                    {
                        IsSuccess = false,
                        Message =
                            $"Status '{raw}' no válido. Use Disponible, Asignado, Mantenimiento, Baja (o el enum).",
                        Error = "Validation"
                    };
                }

                if (!statuses.Contains(parsed))
                    statuses.Add(parsed);
            }
        }

        return new Result<AssetFilterCriteria>
        {
            IsSuccess = true,
            Message = "OK",
            Data = new AssetFilterCriteria
            {
                Search = query.Search,
                Statuses = statuses,
                Conditions = query.Conditions,
                CategoryNames = query.Categories,
                Kinds = query.Kinds,
                ModelIds = query.ModelIds,
                LocationIds = query.LocationIds,
                CategoryIds = query.CategoryIds,
                BrandIds = query.BrandIds
            }
        };
    }

    private static string? ValidateStatusTransition(AssetStatus from, AssetStatus to, bool isAdmin)
    {
        if (from == to)
            return null;

        if (!isAdmin)
        {
            if ((from == AssetStatus.Available && to == AssetStatus.Maintenance) ||
                (from == AssetStatus.Maintenance && to == AssetStatus.Available))
                return null;
            return "Solo puede enviar a garantía o marcar disponible.";
        }

        return (from, to) switch
        {
            (AssetStatus.Available, AssetStatus.Maintenance) => null,
            (AssetStatus.Maintenance, AssetStatus.Available) => null,
            (AssetStatus.Available, AssetStatus.Retired) => null,
            (AssetStatus.Maintenance, AssetStatus.Retired) => null,
            (AssetStatus.Retired, AssetStatus.Available) => null,
            (AssetStatus.Assigned, _) => "Un activo asignado solo se libera con devolución.",
            (_, AssetStatus.Assigned) => "Use el flujo de asignación para marcar Asignado.",
            (AssetStatus.Retired, _) => "Reactive el activo antes de cambiar su estado.",
            _ => $"Transición de estado no permitida: {from.ToSpanish()} → {to.ToSpanish()}."
        };
    }

    private static List<AssetMovement> BuildUpdateAudits(
        Asset entity,
        AssetStatus fromStatus,
        int? fromLocationId,
        Guid performedByUserId)
    {
        var now = DateTime.UtcNow;
        var audits = new List<AssetMovement>();

        if (fromStatus != entity.Status)
        {
            audits.Add(new AssetMovement
            {
                AssetId = entity.Id,
                MovementType = MovementType.StatusChanged,
                FromStatus = fromStatus,
                ToStatus = entity.Status,
                FromLocationId = fromLocationId,
                ToLocationId = entity.LocationId,
                PerformedByUserId = performedByUserId,
                Notes = $"Cambio de estado: {fromStatus.ToSpanish()} → {entity.Status.ToSpanish()}",
                OccurredAt = now
            });
        }

        if (fromLocationId != entity.LocationId)
        {
            audits.Add(new AssetMovement
            {
                AssetId = entity.Id,
                MovementType = MovementType.LocationChanged,
                FromStatus = entity.Status,
                ToStatus = entity.Status,
                FromLocationId = fromLocationId,
                ToLocationId = entity.LocationId,
                PerformedByUserId = performedByUserId,
                Notes = "Cambio de ubicación",
                OccurredAt = now
            });
        }

        return audits;
    }

    private async Task<Result<Guid>?> ValidateAsync(AssetUpsertDto dto, Guid? excludeId, bool isCreate)
    {
        var code = dto.AssetCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code))
            return Fail<Guid>("El código de activo es obligatorio.", "Validation");

        if (!Enum.IsDefined(dto.Kind) || !Enum.IsDefined(dto.OwnershipType) || !Enum.IsDefined(dto.Status))
            return Fail<Guid>("Kind, OwnershipType o Status no válidos.", "Validation");

        // En update se permite Status=Assigned si ya estaba (mantener custodia); el alta nunca.
        if (isCreate && dto.Status == AssetStatus.Assigned)
            return Fail<Guid>("Use el flujo de asignación para marcar Asignado.", "Validation");

        if (isCreate && dto.Status is not AssetStatus.Available)
            return Fail<Guid>("El alta siempre inicia como disponible (nuevo).", "Validation");

        if (dto.OwnershipType == OwnershipType.Rented && !dto.SupplierId.HasValue)
            return Fail<Guid>("Un activo rentado requiere proveedor.", "Validation");

        if (!await _repo.ModelExistsAsync(dto.ModelId))
            return Fail<Guid>("El modelo no existe.", "Validation");

        if (dto.LocationId.HasValue && !await _repo.LocationExistsAsync(dto.LocationId.Value))
            return Fail<Guid>("La ubicación no existe.", "Validation");

        if (dto.SupplierId.HasValue && !await _repo.SupplierExistsAsync(dto.SupplierId.Value))
            return Fail<Guid>("El proveedor no existe.", "Validation");

        if (await _repo.CodeExistsAsync(code, excludeId))
            return Fail<Guid>("Ya existe un activo con ese código.", "Duplicate");

        if (!string.IsNullOrWhiteSpace(dto.SerialNumber))
        {
            if (await _repo.SerialExistsAsync(dto.SerialNumber, excludeId))
                return Fail<Guid>("Ya existe un activo con ese número de serie.", "Duplicate");
        }

        return null;
    }

    private static Asset MapToEntity(AssetUpsertDto dto, Asset entity)
    {
        entity.AssetCode = dto.AssetCode!.Trim();
        entity.SerialNumber = string.IsNullOrWhiteSpace(dto.SerialNumber) ? null : dto.SerialNumber.Trim();
        entity.Kind = dto.Kind;
        entity.ModelId = dto.ModelId;
        entity.OwnershipType = dto.OwnershipType;
        entity.SupplierId = dto.SupplierId;
        entity.Status = dto.Status;
        entity.LocationId = dto.LocationId;
        entity.PurchaseDate = dto.PurchaseDate;
        entity.RentalEndDate = dto.OwnershipType == OwnershipType.Rented ? dto.RentalEndDate : null;
        entity.WarrantyEndDate = dto.WarrantyEndDate;
        entity.Imei = string.IsNullOrWhiteSpace(dto.Imei) ? null : dto.Imei.Trim();
        entity.ContractNumber = string.IsNullOrWhiteSpace(dto.ContractNumber) ? null : dto.ContractNumber.Trim();
        return entity;
    }

    private static AssetListDto Map(Asset x) => new()
    {
        Id = x.Id,
        AssetCode = x.AssetCode,
        SerialNumber = x.SerialNumber,
        Kind = x.Kind,
        OwnershipType = x.OwnershipType,
        Status = x.Status,
        Condition = x.Condition,
        ModelId = x.ModelId,
        BrandId = x.Model?.BrandId ?? 0,
        ModelName = x.Model?.Name ?? string.Empty,
        BrandName = x.Model?.Brand?.Name ?? string.Empty,
        CategoryName = x.Model?.Category?.Name ?? string.Empty,
        Specs = x.Model?.Specs,
        SupplierId = x.SupplierId,
        SupplierName = x.Supplier?.Name,
        LocationId = x.LocationId,
        LocationName = x.Location?.Name,
        CurrentEmployeeId = x.CurrentEmployeeId,
        CurrentEmployeeName = x.CurrentEmployee?.FullName,
        PurchaseDate = x.PurchaseDate,
        RentalEndDate = x.RentalEndDate,
        WarrantyEndDate = x.WarrantyEndDate,
        Imei = x.Imei,
        ContractNumber = x.ContractNumber
    };

    private static Result<T> Ok<T>(T data, string message) => new()
    {
        IsSuccess = true,
        Message = message,
        Data = data
    };

    private static Result<T> Fail<T>(string message, string error) => new()
    {
        IsSuccess = false,
        Message = message,
        Error = error
    };
}
