using ITAM.Domain.Interfaces.Services;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assets;

namespace ITAM.Domain.Interfaces.Services.Assets;

/// <summary>Export Excel del inventario de activos (mismos filtros que el listado, sin paginar).</summary>
public interface IAssetExportService
{
    Task<Result<ExportFile>> ExportAsync(AssetListQuery query, CancellationToken ct = default);
}
