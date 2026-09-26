using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Catalog;

namespace ITAM.Domain.Interfaces.Services.Catalog;

public interface IBrandService
{
    Task<Result<List<BrandListDto>>> ListAsync(bool? onlyActive);
    Task<Result<BrandListDto>> GetAsync(int id);
    Task<Result<int>> CreateAsync(BrandUpsertDto dto);
    Task<Result<bool>> UpdateAsync(int id, BrandUpsertDto dto);
}
