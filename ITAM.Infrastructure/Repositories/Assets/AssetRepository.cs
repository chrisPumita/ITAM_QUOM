using ITAM.Domain.Entities.Assets;
using ITAM.Domain.Interfaces.Repositories.Assets;
using ITAM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Infrastructure.Repositories.Assets;

public class AssetRepository : IAssetRepository
{
    private readonly ApplicationDbContext _db;

    public AssetRepository(ApplicationDbContext db) => _db = db;

    public async Task<(IReadOnlyList<Asset> Items, int TotalCount)> SearchAsync(
        AssetFilterCriteria filter,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var resolved = await ResolveAsync(filter, ct);
        var q = BuildFilterQuery(resolved);
        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(x => x.AssetCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.Model).ThenInclude(m => m!.Brand)
            .Include(x => x.Model).ThenInclude(m => m!.Category)
            .Include(x => x.Supplier)
            .Include(x => x.Location)
            .Include(x => x.CurrentEmployee)
            .AsSplitQuery()
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<Asset>> ListAsync(
        AssetFilterCriteria filter,
        CancellationToken ct = default)
    {
        var resolved = await ResolveAsync(filter, ct);
        return await BuildFilterQuery(resolved)
            .OrderBy(x => x.AssetCode)
            .Include(x => x.Model).ThenInclude(m => m!.Brand)
            .Include(x => x.Model).ThenInclude(m => m!.Category)
            .Include(x => x.Supplier)
            .Include(x => x.Location)
            .Include(x => x.CurrentEmployee)
            .AsSplitQuery()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Convierte nombres de categoría (contains) en Ids — OR facetado traducible a SQL.
    /// </summary>
    private async Task<AssetFilterCriteria> ResolveAsync(AssetFilterCriteria filter, CancellationToken ct)
    {
        if (filter.CategoryNames is not { Count: > 0 })
            return filter;

        var matched = new HashSet<int>();
        foreach (var raw in filter.CategoryNames)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            var term = raw.Trim().ToLower();
            var ids = await _db.Categories.AsNoTracking()
                .Where(c => c.Name.ToLower().Contains(term))
                .Select(c => c.Id)
                .ToListAsync(ct);
            foreach (var id in ids)
                matched.Add(id);
        }

        if (filter.CategoryIds is { Count: > 0 })
        {
            foreach (var id in filter.CategoryIds)
                matched.Add(id);
        }

        // Si pidieron nombres y ninguno matcheó → resultado vacío (Id imposible).
        if (matched.Count == 0)
            matched.Add(-1);

        return new AssetFilterCriteria
        {
            Search = filter.Search,
            Statuses = filter.Statuses,
            CategoryNames = null,
            Kinds = filter.Kinds,
            ModelIds = filter.ModelIds,
            LocationIds = filter.LocationIds,
            CategoryIds = matched.ToArray()
        };
    }

    /// <summary>
    /// Dentro de cada faceta: OR. Entre facetas: AND. Sin Include (Count/Skip baratos).
    /// </summary>
    private IQueryable<Asset> BuildFilterQuery(AssetFilterCriteria filter)
    {
        var q = _db.Assets.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            q = q.Where(a =>
                a.AssetCode.ToLower().Contains(term) ||
                (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(term)) ||
                (a.Imei != null && a.Imei.ToLower().Contains(term)) ||
                (a.ContractNumber != null && a.ContractNumber.ToLower().Contains(term)) ||
                a.Model.Name.ToLower().Contains(term) ||
                (a.Model.Specs != null && a.Model.Specs.ToLower().Contains(term)) ||
                a.Model.Brand.Name.ToLower().Contains(term) ||
                a.Model.Category.Name.ToLower().Contains(term) ||
                (a.CurrentEmployee != null && a.CurrentEmployee.FullName.ToLower().Contains(term)));
        }

        if (filter.Statuses is { Count: > 0 })
        {
            var statuses = filter.Statuses.ToArray();
            q = q.Where(x => statuses.Contains(x.Status));
        }

        if (filter.Kinds is { Count: > 0 })
        {
            var kinds = filter.Kinds.ToArray();
            q = q.Where(x => kinds.Contains(x.Kind));
        }

        if (filter.ModelIds is { Count: > 0 })
        {
            var ids = filter.ModelIds.ToArray();
            q = q.Where(x => ids.Contains(x.ModelId));
        }

        if (filter.LocationIds is { Count: > 0 })
        {
            var ids = filter.LocationIds.ToArray();
            q = q.Where(x => x.LocationId.HasValue && ids.Contains(x.LocationId.Value));
        }

        if (filter.CategoryIds is { Count: > 0 })
        {
            var ids = filter.CategoryIds.ToArray();
            q = q.Where(x => ids.Contains(x.Model.CategoryId));
        }

        return q;
    }

    public Task<Asset?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Assets
            .Include(x => x.Model).ThenInclude(m => m!.Brand)
            .Include(x => x.Model).ThenInclude(m => m!.Category)
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

    public async Task<Asset> AddAsync(Asset entity, AssetMovement audit, CancellationToken ct = default)
    {
        _db.Assets.Add(entity);
        audit.AssetId = entity.Id;
        _db.AssetMovements.Add(audit);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(Asset entity, IReadOnlyList<AssetMovement> audits, CancellationToken ct = default)
    {
        _db.Assets.Update(entity);
        if (audits.Count > 0)
            _db.AssetMovements.AddRange(audits);
        await _db.SaveChangesAsync(ct);
    }
}
