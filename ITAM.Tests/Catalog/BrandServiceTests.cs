using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Interfaces.Repositories.Catalog;
using ITAM.Infrastructure.Services.Catalog;
using ITAM.Shared.Dtos.Catalog;
using Moq;

namespace ITAM.Tests.Catalog;

public class BrandServiceTests
{
    private readonly Mock<IBrandRepository> _repo = new();
    private BrandService Sut => new(_repo.Object);

    [Fact]
    public async Task CreateAsync_WhenValid_ReturnsId()
    {
        _repo.Setup(r => r.NameExistsAsync("Dell", null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<Brand>(), default))
            .ReturnsAsync((Brand e, CancellationToken _) =>
            {
                e.Id = 3;
                return e;
            });

        var result = await Sut.CreateAsync(new BrandUpsertDto { Name = "Dell", IsActive = true });

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Data);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicate_ReturnsDuplicateError()
    {
        _repo.Setup(r => r.NameExistsAsync("Dell", null, default)).ReturnsAsync(true);

        var result = await Sut.CreateAsync(new BrandUpsertDto { Name = "Dell" });

        Assert.False(result.IsSuccess);
        Assert.Equal("Duplicate", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(10, default)).ReturnsAsync((Brand?)null);

        var result = await Sut.UpdateAsync(10, new BrandUpsertDto { Name = "HP" });

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.Error);
    }
}
