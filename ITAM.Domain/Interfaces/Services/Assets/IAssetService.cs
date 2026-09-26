using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assets;

namespace ITAM.Domain.Interfaces.Services.Assets;

public interface IAssetService
{
    Task<Result<PagedResult<AssetListDto>>> ListAsync(AssetListQuery query);
    Task<Result<AssetSummaryDto>> GetSummaryAsync();
    Task<Result<AssetListDto>> GetAsync(Guid id);
    Task<Result<AssetListDto>> GetByCodeAsync(string assetCode);
    Task<Result<Guid>> CreateAsync(AssetUpsertDto dto, Guid performedByUserId);
    Task<Result<bool>> UpdateAsync(Guid id, AssetUpsertDto dto, Guid performedByUserId, bool isAdmin);
    Task<Result<AssetImportResultDto>> ImportAsync(AssetImportRequestDto request, Guid performedByUserId);
}
