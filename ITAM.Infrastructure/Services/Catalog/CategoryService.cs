using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Interfaces.Repositories.Catalog;
using ITAM.Domain.Interfaces.Services.Catalog;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Catalog;

namespace ITAM.Infrastructure.Services.Catalog;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repo;

    public CategoryService(ICategoryRepository repo) => _repo = repo;

    public async Task<Result<List<CategoryListDto>>> ListAsync(bool? onlyActive)
    {
        var items = await _repo.ListAsync(onlyActive);
        return Ok(items.Select(Map).ToList(), "Categorías obtenidas.");
    }

    public async Task<Result<CategoryListDto>> GetAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<CategoryListDto>("Categoría no encontrada.", "NotFound");
        return Ok(Map(entity), "OK");
    }

    public async Task<Result<int>> CreateAsync(CategoryUpsertDto dto)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Fail<int>("El nombre es obligatorio.", "Validation");

        if (await _repo.NameExistsAsync(name, null))
            return Fail<int>("Ya existe una categoría con ese nombre.", "Duplicate");

        var entity = new Category
        {
            Name = name,
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive,
            ParentCategoryId = dto.ParentCategoryId
        };
        await _repo.AddAsync(entity);
        return Ok(entity.Id, "Categoría creada.");
    }

    public async Task<Result<bool>> UpdateAsync(int id, CategoryUpsertDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<bool>("Categoría no encontrada.", "NotFound");

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Fail<bool>("El nombre es obligatorio.", "Validation");

        if (await _repo.NameExistsAsync(name, id))
            return Fail<bool>("Ya existe una categoría con ese nombre.", "Duplicate");

        if (dto.ParentCategoryId == id)
            return Fail<bool>("Una categoría no puede ser padre de sí misma.", "Validation");

        entity.Name = name;
        entity.SortOrder = dto.SortOrder;
        entity.IsActive = dto.IsActive;
        entity.ParentCategoryId = dto.ParentCategoryId;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(entity);
        return Ok(true, "Categoría actualizada.");
    }

    private static CategoryListDto Map(Category x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        SortOrder = x.SortOrder,
        IsActive = x.IsActive,
        ParentCategoryId = x.ParentCategoryId
    };

    private static Result<T> Ok<T>(T data, string message) => new()
    {
        IsSuccess = true,
        Message = message,
        Data = data
    };

    private static Result<T> Fail<T>(string message, string error) => new()
    {
        IsSuccess = false,
        Message = message,
        Error = error
    };
}
