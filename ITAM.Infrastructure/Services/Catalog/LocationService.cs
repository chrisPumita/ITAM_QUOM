using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Interfaces.Repositories.Catalog;
using ITAM.Domain.Interfaces.Services.Catalog;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Catalog;

namespace ITAM.Infrastructure.Services.Catalog;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _repo;

    public LocationService(ILocationRepository repo) => _repo = repo;

    public async Task<Result<List<LocationListDto>>> ListAsync(bool? onlyActive, bool? onlyWarehouses)
    {
        var items = await _repo.ListAsync(onlyActive, onlyWarehouses);
        return Ok(items.Select(Map).ToList(), "Ubicaciones obtenidas.");
    }

    public async Task<Result<LocationListDto>> GetAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<LocationListDto>("Ubicación no encontrada.", "NotFound");
        return Ok(Map(entity), "OK");
    }

    public async Task<Result<int>> CreateAsync(LocationUpsertDto dto)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Fail<int>("El nombre es obligatorio.", "Validation");

        if (dto.ParentLocationId.HasValue)
        {
            if (await _repo.GetByIdAsync(dto.ParentLocationId.Value) is null)
                return Fail<int>("La ubicación padre no existe.", "Validation");
        }

        if (await _repo.ExistsAsync(name, dto.ParentLocationId, null))
            return Fail<int>("Ya existe una ubicación con ese nombre bajo el mismo padre.", "Duplicate");

        var entity = new Location
        {
            Name = name,
            IsWarehouse = dto.IsWarehouse,
            ParentLocationId = dto.ParentLocationId,
            IsActive = dto.IsActive
        };
        await _repo.AddAsync(entity);
        return Ok(entity.Id, "Ubicación creada.");
    }

    public async Task<Result<bool>> UpdateAsync(int id, LocationUpsertDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<bool>("Ubicación no encontrada.", "NotFound");

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Fail<bool>("El nombre es obligatorio.", "Validation");

        if (dto.ParentLocationId == id)
            return Fail<bool>("Una ubicación no puede ser padre de sí misma.", "Validation");

        if (dto.ParentLocationId.HasValue)
        {
            if (await _repo.GetByIdAsync(dto.ParentLocationId.Value) is null)
                return Fail<bool>("La ubicación padre no existe.", "Validation");
        }

        if (await _repo.ExistsAsync(name, dto.ParentLocationId, id))
            return Fail<bool>("Ya existe una ubicación con ese nombre bajo el mismo padre.", "Duplicate");

        entity.Name = name;
        entity.IsWarehouse = dto.IsWarehouse;
        entity.ParentLocationId = dto.ParentLocationId;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(entity);
        return Ok(true, "Ubicación actualizada.");
    }

    private static LocationListDto Map(Location x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        IsWarehouse = x.IsWarehouse,
        ParentLocationId = x.ParentLocationId,
        ParentLocationName = x.ParentLocation?.Name,
        IsActive = x.IsActive
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
