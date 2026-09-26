using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Catalog;

namespace ITAM.Domain.Interfaces.Services.Catalog;

public interface IModelService
{
    Task<Result<List<ModelListDto>>> ListAsync(bool? onlyActive, int? categoryId, int? brandId);
    Task<Result<ModelListDto>> GetAsync(int id);
    Task<Result<int>> CreateAsync(ModelUpsertDto dto);
    Task<Result<bool>> UpdateAsync(int id, ModelUpsertDto dto);
}
