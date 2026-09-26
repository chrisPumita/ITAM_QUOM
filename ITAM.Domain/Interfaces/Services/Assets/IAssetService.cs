using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assets;
using ITAM.Shared.Enums;

namespace ITAM.Domain.Interfaces.Services.Assets;

public interface IAssetService
{
    Task<Result<List<AssetListDto>>> ListAsync(
        AssetStatus? status,
        AssetKind? kind,
        int? modelId,
        int? locationId);

    Task<Result<AssetListDto>> GetAsync(Guid id);
    Task<Result<Guid>> CreateAsync(AssetUpsertDto dto);
    Task<Result<bool>> UpdateAsync(Guid id, AssetUpsertDto dto);
}
