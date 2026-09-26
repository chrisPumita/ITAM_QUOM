using ITAM.Domain.Interfaces.Repositories.Assignments;
using ITAM.Infrastructure.Repositories.Assignments;
using ITAM.Infrastructure.Services.Assignments;
using ITAM.Shared.Dtos.Assignments;
using Moq;

namespace ITAM.Tests.Assignments;

public class AssetAssignmentServiceTests
{
    private readonly Mock<IAssetAssignmentRepository> _repo = new();
    private AssetAssignmentService Sut => new(_repo.Object);

    [Fact]
    public async Task AssignAsync_WhenEmptyLines_ReturnsValidation()
    {
        var result = await Sut.AssignAsync(new AssignAssetsDto
        {
            EmployeeId = Guid.NewGuid(),
            Lines = []
        }, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("Validation", result.Error);
        _repo.Verify(c => c.AssignAsync(It.IsAny<AssignAssetsDto>(), It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task AssignAsync_WhenDuplicateAssets_ReturnsValidation()
    {
        var assetId = Guid.NewGuid();
        var result = await Sut.AssignAsync(new AssignAssetsDto
        {
            EmployeeId = Guid.NewGuid(),
            Lines =
            [
                new AssignAssetLineDto { AssetId = assetId },
                new AssignAssetLineDto { AssetId = assetId }
            ]
        }, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Contains("duplicados", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AssignAsync_WhenRepositorySucceeds_ReturnsFolio()
    {
        var userId = Guid.NewGuid();
        var dto = new AssignAssetsDto
        {
            EmployeeId = Guid.NewGuid(),
            Lines = [new AssignAssetLineDto { AssetId = Guid.NewGuid() }]
        };

        _repo.Setup(c => c.AssignAsync(dto, userId, default))
            .ReturnsAsync(new AssignAssetsResultDto
            {
                CustodyFormId = Guid.NewGuid(),
                Folio = "RES-2026-0001",
                AssignedCount = 1
            });

        var result = await Sut.AssignAsync(dto, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal("RES-2026-0001", result.Data!.Folio);
    }

    [Fact]
    public async Task ReturnAsync_WhenRepositoryThrowsNotFound_MapsError()
    {
        var dto = new ReturnAssetDto { AssetId = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        _repo.Setup(c => c.ReturnAsync(dto, userId, default))
            .ThrowsAsync(new AssetAssignmentRepositoryException("El activo no tiene asignación activa.", "NotFound"));

        var result = await Sut.ReturnAsync(dto, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.Error);
    }

    [Fact]
    public async Task GetAssignmentAsync_WhenMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetAssignmentAsync(id, default)).ReturnsAsync((AssignmentListDto?)null);

        var result = await Sut.GetAssignmentAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.Error);
    }

    [Fact]
    public async Task ListAssignmentsAsync_ReturnsItemsFromRepository()
    {
        var items = new List<AssignmentListDto>
        {
            new() { Id = Guid.NewGuid(), AssetCode = "EQ-001", EmployeeName = "Ana" }
        };
        _repo.Setup(r => r.ListAssignmentsAsync(null, null, true, default)).ReturnsAsync(items);

        var result = await Sut.ListAssignmentsAsync(null, null, onlyActive: true);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
        Assert.Equal("EQ-001", result.Data![0].AssetCode);
    }

    [Fact]
    public async Task GetCustodyFormAsync_WhenMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetCustodyFormAsync(id, default)).ReturnsAsync((CustodyFormDetailDto?)null);

        var result = await Sut.GetCustodyFormAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.Error);
    }
}
