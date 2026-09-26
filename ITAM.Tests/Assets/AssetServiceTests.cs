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
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

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
    public async Task CreateAsync_WhenValid_WritesCreatedMovement()
    {
        var dto = ValidDto();
        _repo.Setup(r => r.ModelExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.LocationExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.CodeExistsAsync("IT-001", null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<Asset>(), It.IsAny<AssetMovement>(), default))
            .ReturnsAsync((Asset e, AssetMovement _, CancellationToken __) => e);

        var result = await Sut.CreateAsync(dto, UserId);

        Assert.True(result.IsSuccess);
        _repo.Verify(r => r.AddAsync(
            It.IsAny<Asset>(),
            It.Is<AssetMovement>(m =>
                m.MovementType == MovementType.Created &&
                m.PerformedByUserId == UserId &&
                m.ToStatus == AssetStatus.Available),
            default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenRentedWithoutSupplier_ReturnsValidation()
    {
        var dto = ValidDto();
        dto.OwnershipType = OwnershipType.Rented;
        dto.SupplierId = null;

        var result = await Sut.CreateAsync(dto, UserId);

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

        var result = await Sut.CreateAsync(dto, UserId);

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

        var result = await Sut.CreateAsync(dto, UserId);

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

        var result = await Sut.UpdateAsync(id, dto, UserId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Validation", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenStatusAndLocationChange_WritesBothMovements()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(new Asset
        {
            Id = id,
            AssetCode = "IT-001",
            Status = AssetStatus.Available,
            LocationId = 1,
            ModelId = 1,
            Kind = AssetKind.Equipment,
            OwnershipType = OwnershipType.Owned
        });
        _repo.Setup(r => r.ModelExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.LocationExistsAsync(2, default)).ReturnsAsync(true);
        _repo.Setup(r => r.CodeExistsAsync("IT-001", id, default)).ReturnsAsync(false);

        var dto = ValidDto();
        dto.Status = AssetStatus.Maintenance;
        dto.LocationId = 2;

        var result = await Sut.UpdateAsync(id, dto, UserId);

        Assert.True(result.IsSuccess);
        _repo.Verify(r => r.UpdateAsync(
            It.IsAny<Asset>(),
            It.Is<IReadOnlyList<AssetMovement>>(m =>
                m.Count == 2 &&
                m.Any(x => x.MovementType == MovementType.StatusChanged) &&
                m.Any(x => x.MovementType == MovementType.LocationChanged) &&
                m.All(x => x.PerformedByUserId == UserId)),
            default), Times.Once);
    }
}
