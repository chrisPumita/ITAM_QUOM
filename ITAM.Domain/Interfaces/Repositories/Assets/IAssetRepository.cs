using ITAM.Domain.Entities.Assets;
using ITAM.Shared.Dtos.Assets;

namespace ITAM.Domain.Interfaces.Repositories.Assets;

public interface IAssetRepository
{
    /// <summary>
    /// Filtros facetados + búsqueda libre; Count antes de paginar (patrón Daikin / ecommerce).
    /// </summary>
    Task<(IReadOnlyList<Asset> Items, int TotalCount)> SearchAsync(
        AssetFilterCriteria filter,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Mismos filtros que Search, sin paginar (export Excel).</summary>
    Task<IReadOnlyList<Asset>> ListAsync(
        AssetFilterCriteria filter,
        CancellationToken ct = default);

    Task<Asset?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Asset?> GetByCodeAsync(string assetCode, CancellationToken ct = default);
    Task<AssetSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string assetCode, Guid? excludeId, CancellationToken ct = default);
    Task<bool> SerialExistsAsync(string serialNumber, Guid? excludeId, CancellationToken ct = default);
    Task<bool> ModelExistsAsync(int modelId, CancellationToken ct = default);
    Task<bool> LocationExistsAsync(int locationId, CancellationToken ct = default);
    Task<bool> SupplierExistsAsync(Guid supplierId, CancellationToken ct = default);

    /// <summary>Alta de activo + movimiento de auditoría en la misma transacción.</summary>
    Task<Asset> AddAsync(Asset entity, AssetMovement audit, CancellationToken ct = default);

    /// <summary>Actualización + movimientos de auditoría (0..N) en la misma transacción.</summary>
    Task UpdateAsync(Asset entity, IReadOnlyList<AssetMovement> audits, CancellationToken ct = default);
}
