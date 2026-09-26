using ITAM.Domain.Entities.Company;

namespace ITAM.Domain.Interfaces.Repositories.Company;

public interface ISupplierRepository
{
    Task<List<Supplier>> ListAsync(bool? onlyActive, CancellationToken ct = default);
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Supplier?> FindByNameAsync(string name, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken ct = default);
    Task<Supplier> AddAsync(Supplier entity, CancellationToken ct = default);
    Task UpdateAsync(Supplier entity, CancellationToken ct = default);
}
