using ITAM.Domain.Entities.Assets;
using ITAM.Domain.Interfaces.Repositories.Assets;
using ITAM.Infrastructure.Persistence;
using ITAM.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Infrastructure.Repositories.Assets;

public class AssetRepository : IAssetRepository
{
    private readonly ApplicationDbContext _db;

    public AssetRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Asset>> ListAsync(
        AssetStatus? status,
        AssetKind? kind,
        int? modelId,
        int? locationId,
        CancellationToken ct = default)
    {
        var q = _db.Assets
            .AsNoTracking()
            .Include(x => x.Model).ThenInclude(m => m.Brand)
            .Include(x => x.Model).ThenInclude(m => m.Category)
            .Include(x => x.Supplier)
            .Include(x => x.Location)
            .Include(x => x.CurrentEmployee)
            .AsQueryable();

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);
        if (kind.HasValue)
            q = q.Where(x => x.Kind == kind.Value);
        if (modelId.HasValue)
            q = q.Where(x => x.ModelId == modelId.Value);
        if (locationId.HasValue)
            q = q.Where(x => x.LocationId == locationId.Value);

        return q.OrderBy(x => x.AssetCode).ToListAsync(ct);
    }

    public Task<Asset?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Assets
            .Include(x => x.Model).ThenInclude(m => m.Brand)
            .Include(x => x.Model).ThenInclude(m => m.Category)
            .Include(x => x.Supplier)
            .Include(x => x.Location)
            .Include(x => x.CurrentEmployee)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> CodeExistsAsync(string assetCode, Guid? excludeId, CancellationToken ct = default)
    {
        var normalized = assetCode.Trim();
        return _db.Assets.AnyAsync(
            x => x.AssetCode == normalized && (!excludeId.HasValue || x.Id != excludeId.Value), ct);
    }

    public Task<bool> SerialExistsAsync(string serialNumber, Guid? excludeId, CancellationToken ct = default)
    {
        var normalized = serialNumber.Trim();
        return _db.Assets.AnyAsync(
            x => x.SerialNumber == normalized && (!excludeId.HasValue || x.Id != excludeId.Value), ct);
    }

    public Task<bool> ModelExistsAsync(int modelId, CancellationToken ct = default)
        => _db.Models.AnyAsync(x => x.Id == modelId, ct);

    public Task<bool> LocationExistsAsync(int locationId, CancellationToken ct = default)
        => _db.Locations.AnyAsync(x => x.Id == locationId, ct);

    public Task<bool> SupplierExistsAsync(Guid supplierId, CancellationToken ct = default)
        => _db.Suppliers.AnyAsync(x => x.Id == supplierId, ct);

    public async Task<Asset> AddAsync(Asset entity, CancellationToken ct = default)
    {
        _db.Assets.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(Asset entity, CancellationToken ct = default)
    {
        _db.Assets.Update(entity);
        await _db.SaveChangesAsync(ct);
    }
}
