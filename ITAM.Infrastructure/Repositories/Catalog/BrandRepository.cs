using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Interfaces.Repositories.Catalog;
using ITAM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Infrastructure.Repositories.Catalog;

public class BrandRepository : IBrandRepository
{
    private readonly ApplicationDbContext _db;

    public BrandRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Brand>> ListAsync(bool? onlyActive, CancellationToken ct = default)
    {
        var q = _db.Brands.AsNoTracking();
        if (onlyActive == true)
            q = q.Where(x => x.IsActive);
        return q.OrderBy(x => x.Name).ToListAsync(ct);
    }

    public Task<Brand?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.Brands.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default)
    {
        var normalized = name.Trim();
        return _db.Brands.AnyAsync(
            x => x.Name == normalized && (!excludeId.HasValue || x.Id != excludeId.Value), ct);
    }

    public async Task<Brand> AddAsync(Brand entity, CancellationToken ct = default)
    {
        _db.Brands.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(Brand entity, CancellationToken ct = default)
    {
        _db.Brands.Update(entity);
        await _db.SaveChangesAsync(ct);
    }
}
