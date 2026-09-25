using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Interfaces.Repositories.Catalog;
using ITAM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Infrastructure.Repositories.Catalog;

public class ModelRepository : IModelRepository
{
    private readonly ApplicationDbContext _db;

    public ModelRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Model>> ListAsync(bool? onlyActive, int? categoryId, int? brandId, CancellationToken ct = default)
    {
        var q = _db.Models
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .AsQueryable();

        if (onlyActive == true)
            q = q.Where(x => x.IsActive);
        if (categoryId.HasValue)
            q = q.Where(x => x.CategoryId == categoryId.Value);
        if (brandId.HasValue)
            q = q.Where(x => x.BrandId == brandId.Value);

        return q.OrderBy(x => x.Name).ToListAsync(ct);
    }

    public Task<Model?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.Models
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExistsAsync(int categoryId, int brandId, string name, int? excludeId, CancellationToken ct = default)
    {
        var normalized = name.Trim();
        return _db.Models.AnyAsync(
            x => x.CategoryId == categoryId
                 && x.BrandId == brandId
                 && x.Name == normalized
                 && (!excludeId.HasValue || x.Id != excludeId.Value),
            ct);
    }

    public async Task<Model> AddAsync(Model entity, CancellationToken ct = default)
    {
        _db.Models.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(Model entity, CancellationToken ct = default)
    {
        _db.Models.Update(entity);
        await _db.SaveChangesAsync(ct);
    }
}
