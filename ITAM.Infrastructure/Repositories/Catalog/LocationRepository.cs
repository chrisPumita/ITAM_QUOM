using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Interfaces.Repositories.Catalog;
using ITAM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Infrastructure.Repositories.Catalog;

public class LocationRepository : ILocationRepository
{
    private readonly ApplicationDbContext _db;

    public LocationRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Location>> ListAsync(bool? onlyActive, bool? onlyWarehouses, CancellationToken ct = default)
    {
        var q = _db.Locations
            .AsNoTracking()
            .Include(x => x.ParentLocation)
            .AsQueryable();

        if (onlyActive == true)
            q = q.Where(x => x.IsActive);
        if (onlyWarehouses == true)
            q = q.Where(x => x.IsWarehouse);

        return q.OrderBy(x => x.Name).ToListAsync(ct);
    }

    public Task<Location?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.Locations
            .Include(x => x.ParentLocation)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExistsAsync(string name, int? parentLocationId, int? excludeId, CancellationToken ct = default)
    {
        var normalized = name.Trim();
        return _db.Locations.AnyAsync(
            x => x.Name == normalized
                 && x.ParentLocationId == parentLocationId
                 && (!excludeId.HasValue || x.Id != excludeId.Value),
            ct);
    }

    public async Task<Location> AddAsync(Location entity, CancellationToken ct = default)
    {
        _db.Locations.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(Location entity, CancellationToken ct = default)
    {
        _db.Locations.Update(entity);
        await _db.SaveChangesAsync(ct);
    }
}
