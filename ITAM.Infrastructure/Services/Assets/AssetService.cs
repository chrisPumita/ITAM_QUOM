using ITAM.Domain.Entities.Assets;
using ITAM.Domain.Interfaces.Repositories.Assets;
using ITAM.Domain.Interfaces.Services.Assets;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assets;
using ITAM.Shared.Enums;

namespace ITAM.Infrastructure.Services.Assets;

public class AssetService : IAssetService
{
    private readonly IAssetRepository _repo;

    public AssetService(IAssetRepository repo) => _repo = repo;

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

    public async Task<Result<AssetListDto>> GetAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<AssetListDto>("Activo no encontrado.", "NotFound");
        return Ok(Map(entity), "OK");
    }

    public async Task<Result<Guid>> CreateAsync(AssetUpsertDto dto, Guid performedByUserId)
    {
        if (performedByUserId == Guid.Empty)
            return Fail<Guid>("Usuario autenticado requerido.", "Validation");

        var validation = await ValidateAsync(dto, excludeId: null);
        if (validation is not null)
            return validation;

        var entity = MapToEntity(dto, new Asset());
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

    public async Task<Result<bool>> UpdateAsync(Guid id, AssetUpsertDto dto, Guid performedByUserId)
    {
        if (performedByUserId == Guid.Empty)
            return Fail<bool>("Usuario autenticado requerido.", "Validation");

        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<bool>("Activo no encontrado.", "NotFound");

        if (entity.Status == AssetStatus.Assigned && dto.Status != AssetStatus.Assigned)
            return Fail<bool>("Un activo asignado solo se libera con devolución.", "Validation");

        if (entity.Status != AssetStatus.Assigned && dto.Status == AssetStatus.Assigned)
            return Fail<bool>("Use el flujo de asignación para marcar Assigned.", "Validation");

        var validation = await ValidateAsync(dto, excludeId: id);
        if (validation is not null)
            return Fail<bool>(validation.Message, validation.Error);

        var fromStatus = entity.Status;
        var fromLocationId = entity.LocationId;

        MapToEntity(dto, entity);
        entity.UpdatedAt = DateTime.UtcNow;

        var audits = BuildUpdateAudits(entity, fromStatus, fromLocationId, performedByUserId);
        await _repo.UpdateAsync(entity, audits);
        return Ok(true, "Activo actualizado.");
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
                CategoryNames = query.Categories,
                Kinds = query.Kinds,
                ModelIds = query.ModelIds,
                LocationIds = query.LocationIds,
                CategoryIds = query.CategoryIds
            }
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

    private async Task<Result<Guid>?> ValidateAsync(AssetUpsertDto dto, Guid? excludeId)
    {
        var code = dto.AssetCode.Trim();
        if (string.IsNullOrWhiteSpace(code))
            return Fail<Guid>("El código de activo es obligatorio.", "Validation");

        if (!Enum.IsDefined(dto.Kind) || !Enum.IsDefined(dto.OwnershipType) || !Enum.IsDefined(dto.Status))
            return Fail<Guid>("Kind, OwnershipType o Status no válidos.", "Validation");

        if (dto.Status == AssetStatus.Assigned)
            return Fail<Guid>("Use el flujo de asignación para marcar Assigned.", "Validation");

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
        entity.AssetCode = dto.AssetCode.Trim();
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
        ModelId = x.ModelId,
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
