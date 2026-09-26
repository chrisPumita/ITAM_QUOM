using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Interfaces.Repositories.Catalog;
using ITAM.Domain.Interfaces.Services.Catalog;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Catalog;

namespace ITAM.Infrastructure.Services.Catalog;

public class BrandService : IBrandService
{
    private readonly IBrandRepository _repo;

    public BrandService(IBrandRepository repo) => _repo = repo;

    public async Task<Result<List<BrandListDto>>> ListAsync(bool? onlyActive)
    {
        var items = await _repo.ListAsync(onlyActive);
        return Ok(items.Select(Map).ToList(), "Marcas obtenidas.");
    }

    public async Task<Result<BrandListDto>> GetAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<BrandListDto>("Marca no encontrada.", "NotFound");
        return Ok(Map(entity), "OK");
    }

    public async Task<Result<int>> CreateAsync(BrandUpsertDto dto)
    {
        var name = dto.Name.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name))
            return Fail<int>("El nombre es obligatorio.", "Validation");

        if (await _repo.NameExistsAsync(name, null))
            return Fail<int>("Ya existe una marca con ese nombre.", "Duplicate");

        var entity = new Brand { Name = name, IsActive = dto.IsActive };
        await _repo.AddAsync(entity);
        return Ok(entity.Id, "Marca creada.");
    }

    public async Task<Result<bool>> UpdateAsync(int id, BrandUpsertDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<bool>("Marca no encontrada.", "NotFound");

        var name = dto.Name.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name))
            return Fail<bool>("El nombre es obligatorio.", "Validation");

        if (await _repo.NameExistsAsync(name, id))
            return Fail<bool>("Ya existe una marca con ese nombre.", "Duplicate");

        entity.Name = name;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(entity);
        return Ok(true, "Marca actualizada.");
    }

    private static BrandListDto Map(Brand x) => new()
    {
        Id = x.Id,
        Name = x.Name,
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
