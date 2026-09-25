using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Interfaces.Repositories;
using ITAM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _db;

    public CategoryRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Category>> ListAsync(bool? onlyActive, CancellationToken ct = default)
    {
        var q = _db.Categories.AsNoTracking();
        if (onlyActive == true)
            q = q.Where(x => x.IsActive);
        return q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(ct);
    }

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.Categories.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default)
    {
        var normalized = name.Trim();
        return _db.Categories.AnyAsync(
            x => x.Name == normalized && (!excludeId.HasValue || x.Id != excludeId.Value), ct);
    }

    public async Task<Category> AddAsync(Category entity, CancellationToken ct = default)
    {
        _db.Categories.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(Category entity, CancellationToken ct = default)
    {
        _db.Categories.Update(entity);
        await _db.SaveChangesAsync(ct);
    }
}

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
