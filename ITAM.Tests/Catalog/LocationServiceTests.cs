using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Interfaces.Repositories.Catalog;
using ITAM.Application.Services.Catalog;
using ITAM.Shared.Dtos.Catalog;
using Moq;

namespace ITAM.Tests.Catalog;

public class LocationServiceTests
{
    private readonly Mock<ILocationRepository> _repo = new();
    private LocationService Sut => new(_repo.Object);

    [Fact]
    public async Task CreateAsync_WhenParentMissing_ReturnsValidation()
    {
        _repo.Setup(r => r.GetByIdAsync(50, default)).ReturnsAsync((Location?)null);

        var result = await Sut.CreateAsync(new LocationUpsertDto
        {
            Name = "Piso 1",
            ParentLocationId = 50
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Validation", result.Error);
        _repo.Verify(r => r.AddAsync(It.IsAny<Location>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateUnderSameParent_ReturnsDuplicate()
    {
        _repo.Setup(r => r.ExistsAsync("Piso 1", null, null, default)).ReturnsAsync(true);

        var result = await Sut.CreateAsync(new LocationUpsertDto { Name = "Piso 1" });

        Assert.False(result.IsSuccess);
        Assert.Equal("Duplicate", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenSelfParent_ReturnsValidation()
    {
        _repo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(new Location { Id = 1, Name = "Bodega" });
        _repo.Setup(r => r.ExistsAsync("Bodega", 1, 1, default)).ReturnsAsync(false);

        var result = await Sut.UpdateAsync(1, new LocationUpsertDto
        {
            Name = "Bodega",
            ParentLocationId = 1
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Validation", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_ReturnsId()
    {
        _repo.Setup(r => r.ExistsAsync("Bodega Central", null, null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<Location>(), default))
            .ReturnsAsync((Location e, CancellationToken _) =>
            {
                e.Id = 7;
                return e;
            });

        var result = await Sut.CreateAsync(new LocationUpsertDto
        {
            Name = "Bodega Central",
            IsWarehouse = true
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Data);
    }
}
