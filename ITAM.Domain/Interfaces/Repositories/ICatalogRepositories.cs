using ITAM.Domain.Entities.Catalog;

namespace ITAM.Domain.Interfaces.Repositories;

public interface ICategoryRepository
{
    Task<List<Category>> ListAsync(bool? onlyActive, CancellationToken ct = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default);
    Task<Category> AddAsync(Category entity, CancellationToken ct = default);
    Task UpdateAsync(Category entity, CancellationToken ct = default);
}

public interface IBrandRepository
{
    Task<List<Brand>> ListAsync(bool? onlyActive, CancellationToken ct = default);
    Task<Brand?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default);
    Task<Brand> AddAsync(Brand entity, CancellationToken ct = default);
    Task UpdateAsync(Brand entity, CancellationToken ct = default);
}
