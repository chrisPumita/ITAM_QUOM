using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Interfaces.Repositories.Catalog;
using ITAM.Infrastructure.Services.Catalog;
using ITAM.Shared.Dtos.Catalog;
using Moq;

namespace ITAM.Tests.Catalog;

public class ModelServiceTests
{
    private readonly Mock<IModelRepository> _models = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IBrandRepository> _brands = new();

    private ModelService Sut => new(_models.Object, _categories.Object, _brands.Object);

    [Fact]
    public async Task CreateAsync_WhenCategoryMissing_ReturnsValidation()
    {
        _categories.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync((Category?)null);

        var result = await Sut.CreateAsync(new ModelUpsertDto
        {
            Name = "Latitude",
            CategoryId = 1,
            BrandId = 2
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Validation", result.Error);
        Assert.Contains("categoría", result.Message, StringComparison.OrdinalIgnoreCase);
        _models.Verify(r => r.AddAsync(It.IsAny<Model>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenBrandMissing_ReturnsValidation()
    {
        _categories.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(new Category { Id = 1, Name = "Laptop" });
        _brands.Setup(r => r.GetByIdAsync(2, default)).ReturnsAsync((Brand?)null);

        var result = await Sut.CreateAsync(new ModelUpsertDto
        {
            Name = "Latitude",
            CategoryId = 1,
            BrandId = 2
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("marca", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicate_ReturnsDuplicateError()
    {
        _categories.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(new Category { Id = 1 });
        _brands.Setup(r => r.GetByIdAsync(2, default)).ReturnsAsync(new Brand { Id = 2 });
        _models.Setup(r => r.ExistsAsync(1, 2, "Latitude", null, default)).ReturnsAsync(true);

        var result = await Sut.CreateAsync(new ModelUpsertDto
        {
            Name = "Latitude",
            CategoryId = 1,
            BrandId = 2
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Duplicate", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_ReturnsId()
    {
        _categories.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(new Category { Id = 1 });
        _brands.Setup(r => r.GetByIdAsync(2, default)).ReturnsAsync(new Brand { Id = 2 });
        _models.Setup(r => r.ExistsAsync(1, 2, "Latitude", null, default)).ReturnsAsync(false);
        _models.Setup(r => r.AddAsync(It.IsAny<Model>(), default))
            .ReturnsAsync((Model e, CancellationToken _) =>
            {
                e.Id = 9;
                return e;
            });

        var result = await Sut.CreateAsync(new ModelUpsertDto
        {
            Name = "Latitude",
            CategoryId = 1,
            BrandId = 2,
            Specs = "i7"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(9, result.Data);
    }
}
