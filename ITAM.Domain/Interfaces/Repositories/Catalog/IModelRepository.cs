using ITAM.Domain.Entities.Catalog;

namespace ITAM.Domain.Interfaces.Repositories.Catalog;

public interface IModelRepository
{
    Task<List<Model>> ListAsync(bool? onlyActive, int? categoryId, int? brandId, CancellationToken ct = default);
    Task<Model?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Model?> FindByBrandAndNameAsync(int brandId, string name, CancellationToken ct = default);
    Task<bool> ExistsAsync(int categoryId, int brandId, string name, int? excludeId, CancellationToken ct = default);
    Task<Model> AddAsync(Model entity, CancellationToken ct = default);
    Task UpdateAsync(Model entity, CancellationToken ct = default);
}
