using ITAM.Domain.Entities.Assets;
using ITAM.Domain.Interfaces.Repositories.Assets;
using ITAM.Infrastructure.Services.Assets;
using ITAM.Shared.Dtos.Assets;
using ITAM.Shared.Enums;
using Moq;

namespace ITAM.Tests.Assets;

public class AssetServiceTests
{
    private readonly Mock<IAssetRepository> _repo = new();
    private AssetService Sut => new(_repo.Object);

    private static AssetUpsertDto ValidDto() => new()
    {
        AssetCode = "IT-001",
        Kind = AssetKind.Equipment,
        ModelId = 1,
        OwnershipType = OwnershipType.Owned,
        Status = AssetStatus.Available,
        LocationId = 1
    };

    [Fact]
    public async Task CreateAsync_WhenValid_ReturnsId()
    {
        var dto = ValidDto();
        _repo.Setup(r => r.ModelExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.LocationExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.CodeExistsAsync("IT-001", null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<Asset>(), default))
            .ReturnsAsync((Asset e, CancellationToken _) => e);

        var result = await Sut.CreateAsync(dto);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Data);
    }

    [Fact]
    public async Task CreateAsync_WhenRentedWithoutSupplier_ReturnsValidation()
    {
        var dto = ValidDto();
        dto.OwnershipType = OwnershipType.Rented;
        dto.SupplierId = null;

        var result = await Sut.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal("Validation", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WhenAssignedStatus_ReturnsValidation()
    {
        var dto = ValidDto();
        dto.Status = AssetStatus.Assigned;
        _repo.Setup(r => r.ModelExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.LocationExistsAsync(1, default)).ReturnsAsync(true);

        var result = await Sut.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Contains("asignación", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateCode_ReturnsDuplicate()
    {
        var dto = ValidDto();
        _repo.Setup(r => r.ModelExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.LocationExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.CodeExistsAsync("IT-001", null, default)).ReturnsAsync(true);

        var result = await Sut.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal("Duplicate", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenTryingToForceAssigned_ReturnsValidation()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(new Asset
        {
            Id = id,
            AssetCode = "IT-001",
            Status = AssetStatus.Available,
            ModelId = 1
        });

        var dto = ValidDto();
        dto.Status = AssetStatus.Assigned;

        var result = await Sut.UpdateAsync(id, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal("Validation", result.Error);
    }
}
