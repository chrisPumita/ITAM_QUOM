using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Catalog;

namespace ITAM.Domain.Interfaces.Services.Catalog;

public interface ILocationService
{
    Task<Result<List<LocationListDto>>> ListAsync(bool? onlyActive, bool? onlyWarehouses);
    Task<Result<LocationListDto>> GetAsync(int id);
    Task<Result<int>> CreateAsync(LocationUpsertDto dto);
    Task<Result<bool>> UpdateAsync(int id, LocationUpsertDto dto);
}
