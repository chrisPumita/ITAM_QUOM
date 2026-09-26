using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Interfaces.Repositories.Catalog;
using ITAM.Infrastructure.Services.Catalog;
using ITAM.Shared.Dtos.Catalog;
using Moq;

namespace ITAM.Tests.Catalog;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _repo = new();
    private CategoryService Sut => new(_repo.Object);

    [Fact]
    public async Task CreateAsync_WhenValid_ReturnsId()
    {
        _repo.Setup(r => r.NameExistsAsync("Laptop", null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<Category>(), default))
            .ReturnsAsync((Category e, CancellationToken _) =>
            {
                e.Id = 5;
                return e;
            });

        var result = await Sut.CreateAsync(new CategoryUpsertDto
        {
            Name = "  Laptop  ",
            SortOrder = 1,
            IsActive = true
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Data);
        _repo.Verify(r => r.AddAsync(It.Is<Category>(c => c.Name == "Laptop"), default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicate_ReturnsDuplicateError()
    {
        _repo.Setup(r => r.NameExistsAsync("Laptop", null, default)).ReturnsAsync(true);

        var result = await Sut.CreateAsync(new CategoryUpsertDto { Name = "Laptop" });

        Assert.False(result.IsSuccess);
        Assert.Equal("Duplicate", result.Error);
        _repo.Verify(r => r.AddAsync(It.IsAny<Category>(), default), Times.Never);
    }

    [Fact]
    public async Task GetAsync_WhenMissing_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Category?)null);

        var result = await Sut.GetAsync(99);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenSelfParent_ReturnsValidation()
    {
        _repo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(new Category { Id = 1, Name = "A" });
        _repo.Setup(r => r.NameExistsAsync("A", 1, default)).ReturnsAsync(false);

        var result = await Sut.UpdateAsync(1, new CategoryUpsertDto
        {
            Name = "A",
            ParentCategoryId = 1
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Validation", result.Error);
    }
}
