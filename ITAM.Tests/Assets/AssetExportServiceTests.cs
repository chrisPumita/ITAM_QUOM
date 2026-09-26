using ITAM.Domain.Entities.Assets;
using ITAM.Domain.Interfaces.Repositories.Assets;
using ITAM.Infrastructure.Services.Assets;
using ITAM.Shared.Enums;
using Moq;

namespace ITAM.Tests.Assets;

public class AssetExportServiceTests
{
    private readonly Mock<IAssetRepository> _repo = new();
    private AssetExportService Sut => new(_repo.Object);

    [Fact]
    public async Task ExportAsync_WhenEmpty_ReturnsNotFound()
    {
        _repo.Setup(r => r.ListAsync(null, null, null, null, null, default))
            .ReturnsAsync([]);

        var result = await Sut.ExportAsync(null, null, null, null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.Error);
    }

    [Fact]
    public async Task ExportAsync_WhenHasData_ReturnsXlsx()
    {
        _repo.Setup(r => r.ListAsync(null, null, null, null, It.IsAny<IReadOnlyList<int>?>(), default))
            .ReturnsAsync(
            [
                new Asset
                {
                    Id = Guid.NewGuid(),
                    AssetCode = "IT-001",
                    Kind = AssetKind.Equipment,
                    Status = AssetStatus.Assigned,
                    OwnershipType = OwnershipType.Owned,
                    Model = new Domain.Entities.Catalog.Model
                    {
                        Name = "Latitude 5440",
                        Specs = "i7 / 16GB",
                        Brand = new Domain.Entities.Catalog.Brand { Name = "LENOVO" },
                        Category = new Domain.Entities.Catalog.Category { Name = "Laptop" }
                    },
                    CurrentEmployee = new Domain.Entities.Company.Employee { FullName = "Ana" }
                }
            ]);

        var result = await Sut.ExportAsync(null, null, null, null, [1, 2]);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.Content.Length > 100);
        Assert.EndsWith(".xlsx", result.Data.FileName);
    }
}
