using ITAM.Domain.Interfaces.Services;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Enums;

namespace ITAM.Domain.Interfaces.Services.Assets;

/// <summary>Export Excel del inventario de activos.</summary>
public interface IAssetExportService
{
    Task<Result<ExportFile>> ExportAsync(
        AssetStatus? status,
        AssetKind? kind,
        int? modelId,
        int? locationId,
        IReadOnlyList<int>? categoryIds,
        CancellationToken ct = default);
}
