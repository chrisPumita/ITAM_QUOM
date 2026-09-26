using ITAM.Domain.Entities.Assets;
using ITAM.Domain.Interfaces.Repositories.Assets;
using ITAM.Shared.Enums;

namespace ITAM.Domain.Interfaces.Repositories.Assets;

public interface IAssetRepository
{
    Task<List<Asset>> ListAsync(
        AssetStatus? status,
        AssetKind? kind,
        int? modelId,
        int? locationId,
        IReadOnlyList<int>? categoryIds,
        CancellationToken ct = default);

    Task<Asset?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string assetCode, Guid? excludeId, CancellationToken ct = default);
    Task<bool> SerialExistsAsync(string serialNumber, Guid? excludeId, CancellationToken ct = default);
    Task<bool> ModelExistsAsync(int modelId, CancellationToken ct = default);
    Task<bool> LocationExistsAsync(int locationId, CancellationToken ct = default);
    Task<bool> SupplierExistsAsync(Guid supplierId, CancellationToken ct = default);
    Task<Asset> AddAsync(Asset entity, CancellationToken ct = default);
    Task UpdateAsync(Asset entity, CancellationToken ct = default);
}
