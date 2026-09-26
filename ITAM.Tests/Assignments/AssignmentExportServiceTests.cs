using ITAM.Domain.Interfaces.Repositories.Assignments;
using ITAM.Infrastructure.Services.Assignments;
using ITAM.Shared.Dtos.Assignments;
using ITAM.Shared.Enums;
using Moq;

namespace ITAM.Tests.Assignments;

public class AssignmentExportServiceTests
{
    private readonly Mock<IAssetAssignmentRepository> _repo = new();
    private AssignmentExportService Sut => new(_repo.Object);

    [Fact]
    public async Task ExportMovementsAsync_ReturnsXlsxBytes()
    {
        _repo.Setup(r => r.ListMovementsAsync(null, null, null, null, default))
            .ReturnsAsync(
            [
                new AssetMovementListDto
                {
                    Id = Guid.NewGuid(),
                    AssetId = Guid.NewGuid(),
                    AssetCode = "IT-001",
                    AssetKind = AssetKind.Equipment,
                    CategoryName = "Laptop",
                    BrandName = "LENOVO",
                    ModelName = "Latitude 5440",
                    Specs = "i7 / 16GB",
                    MovementType = MovementType.Assigned,
                    PerformedByUserId = Guid.NewGuid(),
                    OccurredAt = DateTime.UtcNow
                }
            ]);

        var result = await Sut.ExportMovementsAsync(null, null, null, null);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.Content.Length > 100);
        Assert.EndsWith(".xlsx", result.Data.FileName);
    }

    [Fact]
    public async Task ExportMovementsAsync_WhenEmpty_ReturnsNotFound()
    {
        _repo.Setup(r => r.ListMovementsAsync(null, null, null, null, default)).ReturnsAsync([]);

        var result = await Sut.ExportMovementsAsync(null, null, null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.Error);
    }
}
