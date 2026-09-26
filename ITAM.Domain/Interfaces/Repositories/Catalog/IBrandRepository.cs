using ITAM.Domain.Entities.Catalog;

namespace ITAM.Domain.Interfaces.Repositories.Catalog;

public interface IBrandRepository
{
    Task<List<Brand>> ListAsync(bool? onlyActive, CancellationToken ct = default);
    Task<Brand?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default);
    Task<Brand> AddAsync(Brand entity, CancellationToken ct = default);
    Task UpdateAsync(Brand entity, CancellationToken ct = default);
}
