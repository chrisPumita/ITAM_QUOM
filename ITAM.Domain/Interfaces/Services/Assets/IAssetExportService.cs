using ITAM.Domain.Interfaces.Services;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assets;

namespace ITAM.Domain.Interfaces.Services.Assets;

public interface IAssetExportService
{
    Task<Result<ExportFile>> ExportAsync(AssetListQuery query, CancellationToken ct = default);
}
