using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Interfaces.Repositories;
using ITAM.Domain.Interfaces.Services;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Catalog;

namespace ITAM.Infrastructure.Services;

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

public class BrandService : IBrandService
{
    private readonly IBrandRepository _repo;

    public BrandService(IBrandRepository repo) => _repo = repo;

    public async Task<Result<List<BrandListDto>>> ListAsync(bool? onlyActive)
    {
        var items = await _repo.ListAsync(onlyActive);
        return Ok(items.Select(Map).ToList(), "Marcas obtenidas.");
    }

    public async Task<Result<BrandListDto>> GetAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<BrandListDto>("Marca no encontrada.", "NotFound");
        return Ok(Map(entity), "OK");
    }

    public async Task<Result<int>> CreateAsync(BrandUpsertDto dto)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Fail<int>("El nombre es obligatorio.", "Validation");

        if (await _repo.NameExistsAsync(name, null))
            return Fail<int>("Ya existe una marca con ese nombre.", "Duplicate");

        var entity = new Brand { Name = name, IsActive = dto.IsActive };
        await _repo.AddAsync(entity);
        return Ok(entity.Id, "Marca creada.");
    }

    public async Task<Result<bool>> UpdateAsync(int id, BrandUpsertDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<bool>("Marca no encontrada.", "NotFound");

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Fail<bool>("El nombre es obligatorio.", "Validation");

        if (await _repo.NameExistsAsync(name, id))
            return Fail<bool>("Ya existe una marca con ese nombre.", "Duplicate");

        entity.Name = name;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(entity);
        return Ok(true, "Marca actualizada.");
    }

    private static BrandListDto Map(Brand x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        IsActive = x.IsActive
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

public class ModelService : IModelService
{
    private readonly IModelRepository _repo;
    private readonly ICategoryRepository _categories;
    private readonly IBrandRepository _brands;

    public ModelService(
        IModelRepository repo,
        ICategoryRepository categories,
        IBrandRepository brands)
    {
        _repo = repo;
        _categories = categories;
        _brands = brands;
    }

    public async Task<Result<List<ModelListDto>>> ListAsync(bool? onlyActive, int? categoryId, int? brandId)
    {
        var items = await _repo.ListAsync(onlyActive, categoryId, brandId);
        return Ok(items.Select(Map).ToList(), "Modelos obtenidos.");
    }

    public async Task<Result<ModelListDto>> GetAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<ModelListDto>("Modelo no encontrado.", "NotFound");
        return Ok(Map(entity), "OK");
    }

    public async Task<Result<int>> CreateAsync(ModelUpsertDto dto)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Fail<int>("El nombre es obligatorio.", "Validation");

        if (await _categories.GetByIdAsync(dto.CategoryId) is null)
            return Fail<int>("La categoría no existe.", "Validation");

        if (await _brands.GetByIdAsync(dto.BrandId) is null)
            return Fail<int>("La marca no existe.", "Validation");

        if (await _repo.ExistsAsync(dto.CategoryId, dto.BrandId, name, null))
            return Fail<int>("Ya existe ese modelo para la categoría y marca.", "Duplicate");

        var entity = new Model
        {
            Name = name,
            Specs = string.IsNullOrWhiteSpace(dto.Specs) ? null : dto.Specs.Trim(),
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            IsActive = dto.IsActive
        };
        await _repo.AddAsync(entity);
        return Ok(entity.Id, "Modelo creado.");
    }

    public async Task<Result<bool>> UpdateAsync(int id, ModelUpsertDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<bool>("Modelo no encontrado.", "NotFound");

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Fail<bool>("El nombre es obligatorio.", "Validation");

        if (await _categories.GetByIdAsync(dto.CategoryId) is null)
            return Fail<bool>("La categoría no existe.", "Validation");

        if (await _brands.GetByIdAsync(dto.BrandId) is null)
            return Fail<bool>("La marca no existe.", "Validation");

        if (await _repo.ExistsAsync(dto.CategoryId, dto.BrandId, name, id))
            return Fail<bool>("Ya existe ese modelo para la categoría y marca.", "Duplicate");

        entity.Name = name;
        entity.Specs = string.IsNullOrWhiteSpace(dto.Specs) ? null : dto.Specs.Trim();
        entity.CategoryId = dto.CategoryId;
        entity.BrandId = dto.BrandId;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(entity);
        return Ok(true, "Modelo actualizado.");
    }

    private static ModelListDto Map(Model x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Specs = x.Specs,
        CategoryId = x.CategoryId,
        CategoryName = x.Category?.Name ?? string.Empty,
        BrandId = x.BrandId,
        BrandName = x.Brand?.Name ?? string.Empty,
        IsActive = x.IsActive
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
