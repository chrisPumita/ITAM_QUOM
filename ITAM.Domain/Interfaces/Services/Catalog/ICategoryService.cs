using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Catalog;

namespace ITAM.Domain.Interfaces.Services.Catalog;

public interface ICategoryService
{
    Task<Result<List<CategoryListDto>>> ListAsync(bool? onlyActive);
    Task<Result<CategoryListDto>> GetAsync(int id);
    Task<Result<int>> CreateAsync(CategoryUpsertDto dto);
    Task<Result<bool>> UpdateAsync(int id, CategoryUpsertDto dto);
}
