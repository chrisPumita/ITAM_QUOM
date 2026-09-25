using ITAM.Domain.Entities.Catalog;

namespace ITAM.Domain.Interfaces.Repositories.Catalog;

public interface ILocationRepository
{
    Task<List<Location>> ListAsync(bool? onlyActive, bool? onlyWarehouses, CancellationToken ct = default);
    Task<Location?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(string name, int? parentLocationId, int? excludeId, CancellationToken ct = default);
    Task<Location> AddAsync(Location entity, CancellationToken ct = default);
    Task UpdateAsync(Location entity, CancellationToken ct = default);
}
