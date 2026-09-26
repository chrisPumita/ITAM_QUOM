using ITAM.Domain.Entities.Assets;
using ITAM.Domain.Entities.Catalog;
using ITAM.Domain.Interfaces.Repositories.Assets;
using ITAM.Domain.Interfaces.Repositories.Catalog;
using ITAM.Domain.Interfaces.Repositories.Company;
using ITAM.Domain.Interfaces.Services;
using ITAM.Infrastructure.Services.Assets;
using ITAM.Shared.Dtos.Assets;
using ITAM.Shared.Enums;
using Moq;

namespace ITAM.Tests.Assets;

public class AssetServiceTests
{
    private readonly Mock<IAssetRepository> _repo = new();
    private readonly Mock<IFolioCounterService> _folios = new();
    private readonly Mock<IBrandRepository> _brands = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IModelRepository> _models = new();
    private readonly Mock<ILocationRepository> _locations = new();
    private readonly Mock<ISupplierRepository> _suppliers = new();

    private AssetService Sut => new(
        _repo.Object, _folios.Object, _brands.Object, _categories.Object,
        _models.Object, _locations.Object, _suppliers.Object);

    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public AssetServiceTests()
    {
        _folios.Setup(f => f.NextAsync(It.IsAny<string>(), default))
            .ReturnsAsync("EQ-2026-0001");
    }

    private static AssetUpsertDto ValidDto() => new()
    {
        AssetCode = "IT-001",
        SerialNumber = "SN-1",
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
        _repo.Setup(r => r.SerialExistsAsync("SN-1", null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<Asset>(), It.IsAny<AssetMovement>(), default))
            .ReturnsAsync((Asset e, AssetMovement _, CancellationToken __) => e);

        var result = await Sut.CreateAsync(dto, UserId);

        Assert.True(result.IsSuccess);
        _repo.Verify(r => r.AddAsync(
            It.Is<Asset>(a => a.Condition == AssetCondition.New && a.Status == AssetStatus.Available),
            It.IsAny<AssetMovement>(),
            default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCodeEmpty_GeneratesFolio()
    {
        var dto = ValidDto();
        dto.AssetCode = null;
        _repo.Setup(r => r.ModelExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.LocationExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.CodeExistsAsync("EQ-2026-0001", null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.SerialExistsAsync("SN-1", null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<Asset>(), It.IsAny<AssetMovement>(), default))
            .ReturnsAsync((Asset e, AssetMovement _, CancellationToken __) => e);

        var result = await Sut.CreateAsync(dto, UserId);

        Assert.True(result.IsSuccess);
        _folios.Verify(f => f.NextAsync(FolioPrefixes.Equipment, default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenEquipmentWithoutSerial_ReturnsValidation()
    {
        var dto = ValidDto();
        dto.SerialNumber = null;
        var result = await Sut.CreateAsync(dto, UserId);
        Assert.False(result.IsSuccess);
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
    public async Task CreateAsync_WhenAssignedStatus_StillCreatesAsAvailable()
    {
        var dto = ValidDto();
        dto.Status = AssetStatus.Assigned;
        _repo.Setup(r => r.ModelExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.LocationExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.CodeExistsAsync("IT-001", null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.SerialExistsAsync("SN-1", null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<Asset>(), It.IsAny<AssetMovement>(), default))
            .ReturnsAsync((Asset e, AssetMovement _, CancellationToken __) => e);

        var result = await Sut.CreateAsync(dto, UserId);
        Assert.True(result.IsSuccess);
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
    public async Task UpdateAsync_WhenAssignedToAvailable_ReturnsValidation()
    {
        var entity = new Asset
        {
            Id = Guid.NewGuid(),
            AssetCode = "IT-001",
            Status = AssetStatus.Assigned,
            Condition = AssetCondition.New,
            Kind = AssetKind.Equipment,
            ModelId = 1
        };
        _repo.Setup(r => r.GetByIdAsync(entity.Id, default)).ReturnsAsync(entity);
        var dto = ValidDto();
        dto.Status = AssetStatus.Available;
        var result = await Sut.UpdateAsync(entity.Id, dto, UserId, isAdmin: true);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ImportAsync_ResolvesBrandModelByName()
    {
        _brands.Setup(b => b.FindByNameAsync("DELL", default)).ReturnsAsync((Brand?)null);
        _brands.Setup(b => b.AddAsync(It.IsAny<Brand>(), default))
            .ReturnsAsync((Brand b, CancellationToken _) => { b.Id = 10; return b; });
        _categories.Setup(c => c.FindByNameAsync("Laptops", default)).ReturnsAsync((Category?)null);
        _categories.Setup(c => c.AddAsync(It.IsAny<Category>(), default))
            .ReturnsAsync((Category c, CancellationToken _) => { c.Id = 5; return c; });
        _models.Setup(m => m.FindByBrandAndNameAsync(10, "Latitude 5440", default)).ReturnsAsync((Model?)null);
        _models.Setup(m => m.AddAsync(It.IsAny<Model>(), default))
            .ReturnsAsync((Model m, CancellationToken _) => { m.Id = 20; return m; });
        _locations.Setup(l => l.FindByNameAsync("Oficina", default))
            .ReturnsAsync(new Location { Id = 1, Name = "Oficina" });
        _repo.Setup(r => r.ModelExistsAsync(20, default)).ReturnsAsync(true);
        _repo.Setup(r => r.LocationExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.CodeExistsAsync(It.IsAny<string>(), null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.SerialExistsAsync(It.IsAny<string>(), null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<Asset>(), It.IsAny<AssetMovement>(), default))
            .ReturnsAsync((Asset e, AssetMovement _, CancellationToken __) => e);
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid id, CancellationToken _) => new Asset { Id = id, AssetCode = "EQ-2026-0001" });

        var result = await Sut.ImportAsync(new AssetImportRequestDto
        {
            Rows =
            [
                new AssetImportRowDto
                {
                    RowNumber = 2,
                    Kind = AssetKind.Equipment,
                    BrandName = "dell",
                    ModelName = "Latitude 5440",
                    CategoryName = "Laptops",
                    SerialNumber = "SN-IMP-1",
                    LocationName = "Oficina"
                }
            ]
        }, UserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.Created);
        _brands.Verify(b => b.AddAsync(It.Is<Brand>(x => x.Name == "DELL"), default), Times.Once);
    }
}
