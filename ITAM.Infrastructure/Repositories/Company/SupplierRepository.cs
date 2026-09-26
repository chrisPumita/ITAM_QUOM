using ITAM.Domain.Entities.Company;
using ITAM.Domain.Interfaces.Repositories.Company;
using ITAM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Infrastructure.Repositories.Company;

public class SupplierRepository : ISupplierRepository
{
    private readonly ApplicationDbContext _db;

    public SupplierRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Supplier>> ListAsync(bool? onlyActive, CancellationToken ct = default)
    {
        var q = _db.Suppliers.AsNoTracking();
        if (onlyActive == true)
            q = q.Where(x => x.IsActive);
        return q.OrderBy(x => x.Name).ToListAsync(ct);
    }

    public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Suppliers.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Supplier?> FindByNameAsync(string name, CancellationToken ct = default)
    {
        var normalized = name.Trim();
        return _db.Suppliers.FirstOrDefaultAsync(
            x => x.Name.ToLower() == normalized.ToLower(), ct);
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken ct = default)
    {
        var normalized = name.Trim();
        return _db.Suppliers.AnyAsync(
            x => x.Name == normalized && (!excludeId.HasValue || x.Id != excludeId.Value), ct);
    }

    public async Task<Supplier> AddAsync(Supplier entity, CancellationToken ct = default)
    {
        _db.Suppliers.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(Supplier entity, CancellationToken ct = default)
    {
        _db.Suppliers.Update(entity);
        await _db.SaveChangesAsync(ct);
    }
}
