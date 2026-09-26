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

    public async Task<Result<List<AssetListDto>>> ListAsync(
        AssetStatus? status,
        AssetKind? kind,
        int? modelId,
        int? locationId)
    {
        var items = await _repo.ListAsync(status, kind, modelId, locationId);
        return Ok(items.Select(Map).ToList(), "Activos obtenidos.");
    }

    public async Task<Result<AssetListDto>> GetAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<AssetListDto>("Activo no encontrado.", "NotFound");
        return Ok(Map(entity), "OK");
    }

    public async Task<Result<Guid>> CreateAsync(AssetUpsertDto dto)
    {
        var validation = await ValidateAsync(dto, excludeId: null);
        if (validation is not null)
            return validation;

        var entity = MapToEntity(dto, new Asset());
        await _repo.AddAsync(entity);
        return Ok(entity.Id, "Activo creado.");
    }

    public async Task<Result<bool>> UpdateAsync(Guid id, AssetUpsertDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<bool>("Activo no encontrado.", "NotFound");

        // Asignación/devolución cambia Assigned vía SP; no forzar Assigned desde CRUD.
        if (entity.Status == AssetStatus.Assigned && dto.Status != AssetStatus.Assigned)
            return Fail<bool>("Un activo asignado solo se libera con devolución.", "Validation");

        if (entity.Status != AssetStatus.Assigned && dto.Status == AssetStatus.Assigned)
            return Fail<bool>("Use el flujo de asignación para marcar Assigned.", "Validation");

        var validation = await ValidateAsync(dto, excludeId: id);
        if (validation is not null)
            return Fail<bool>(validation.Message, validation.Error);

        MapToEntity(dto, entity);
        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(entity);
        return Ok(true, "Activo actualizado.");
    }

    private async Task<Result<Guid>?> ValidateAsync(AssetUpsertDto dto, Guid? excludeId)
    {
        var code = dto.AssetCode.Trim();
        if (string.IsNullOrWhiteSpace(code))
            return Fail<Guid>("El código de activo es obligatorio.", "Validation");

        if (!Enum.IsDefined(dto.Kind) || !Enum.IsDefined(dto.OwnershipType) || !Enum.IsDefined(dto.Status))
            return Fail<Guid>("Kind, OwnershipType o Status no válidos.", "Validation");

        if (dto.OwnershipType == OwnershipType.Rented && !dto.SupplierId.HasValue)
            return Fail<Guid>("Un activo rentado requiere proveedor.", "Validation");

        if (dto.OwnershipType == OwnershipType.Owned && dto.RentalEndDate.HasValue)
            return Fail<Guid>("RentalEndDate solo aplica a activos rentados.", "Validation");

        if (excludeId is null && dto.Status == AssetStatus.Assigned)
            return Fail<Guid>("Use el flujo de asignación para marcar Assigned.", "Validation");

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
