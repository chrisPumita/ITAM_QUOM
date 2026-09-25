using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Catalog;

namespace ITAM.Domain.Interfaces.Services;

public interface ICategoryService
{
    Task<Result<List<CategoryListDto>>> ListAsync(bool? onlyActive);
    Task<Result<CategoryListDto>> GetAsync(int id);
    Task<Result<int>> CreateAsync(CategoryUpsertDto dto);
    Task<Result<bool>> UpdateAsync(int id, CategoryUpsertDto dto);
}

public interface IBrandService
{
    Task<Result<List<BrandListDto>>> ListAsync(bool? onlyActive);
    Task<Result<BrandListDto>> GetAsync(int id);
    Task<Result<int>> CreateAsync(BrandUpsertDto dto);
    Task<Result<bool>> UpdateAsync(int id, BrandUpsertDto dto);
}

public interface IModelService
{
    Task<Result<List<ModelListDto>>> ListAsync(bool? onlyActive, int? categoryId, int? brandId);
    Task<Result<ModelListDto>> GetAsync(int id);
    Task<Result<int>> CreateAsync(ModelUpsertDto dto);
    Task<Result<bool>> UpdateAsync(int id, ModelUpsertDto dto);
}
