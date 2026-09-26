using ITAM.Domain.Entities.Catalog;

namespace ITAM.Domain.Interfaces.Repositories.Catalog;

public interface ICategoryRepository
{
    Task<List<Category>> ListAsync(bool? onlyActive, CancellationToken ct = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Category?> FindByNameAsync(string name, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default);
    Task<Category> AddAsync(Category entity, CancellationToken ct = default);
    Task UpdateAsync(Category entity, CancellationToken ct = default);
}
