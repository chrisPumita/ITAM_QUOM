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
    public async Task ExportAssignmentsAsync_ReturnsXlsxBytes()
    {
        _repo.Setup(r => r.ListAssignmentsAsync(null, null, true, default))
            .ReturnsAsync(
            [
                new AssignmentListDto
                {
                    Id = Guid.NewGuid(),
                    AssetId = Guid.NewGuid(),
                    AssetCode = "IT-001",
                    AssetKind = AssetKind.Equipment,
                    EmployeeId = Guid.NewGuid(),
                    EmployeeNumber = "E1",
                    EmployeeName = "Ana",
                    AssignedAt = DateTime.UtcNow,
                    AssignedByUserId = Guid.NewGuid()
                }
            ]);

        var result = await Sut.ExportAssignmentsAsync(null, null, onlyActive: true);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.Content.Length > 100);
        Assert.EndsWith(".xlsx", result.Data.FileName);
        Assert.Contains("spreadsheetml", result.Data.ContentType);
    }

    [Fact]
    public async Task ExportMovementsAsync_ReturnsXlsxBytes()
    {
        _repo.Setup(r => r.ListMovementsAsync(null, null, default))
            .ReturnsAsync(
            [
                new AssetMovementListDto
                {
                    Id = Guid.NewGuid(),
                    AssetId = Guid.NewGuid(),
                    AssetCode = "IT-001",
                    MovementType = MovementType.Assigned,
                    PerformedByUserId = Guid.NewGuid(),
                    OccurredAt = DateTime.UtcNow
                }
            ]);

        var result = await Sut.ExportMovementsAsync(null, null);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.Content.Length > 100);
        Assert.EndsWith(".xlsx", result.Data.FileName);
    }

    [Fact]
    public async Task ExportAssignmentsAsync_WhenEmpty_ReturnsNotFound()
    {
        _repo.Setup(r => r.ListAssignmentsAsync(null, null, true, default))
            .ReturnsAsync([]);

        var result = await Sut.ExportAssignmentsAsync(null, null, onlyActive: true);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.Error);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task ExportMovementsAsync_WhenEmpty_ReturnsNotFound()
    {
        _repo.Setup(r => r.ListMovementsAsync(null, null, default)).ReturnsAsync([]);

        var result = await Sut.ExportMovementsAsync(null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.Error);
    }
}
