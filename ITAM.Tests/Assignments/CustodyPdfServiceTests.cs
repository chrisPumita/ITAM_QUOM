using ITAM.Domain.Interfaces.Repositories.Assignments;
using ITAM.Infrastructure.Services.Assignments;
using ITAM.Shared.Dtos.Assignments;
using ITAM.Shared.Enums;
using ITAM.Shared.Services.Company;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;

namespace ITAM.Tests.Assignments;

public class CustodyPdfServiceTests
{
    private readonly Mock<IAssetAssignmentRepository> _repo = new();
    private readonly Mock<IHostEnvironment> _env = new();

    private CustodyPdfService Sut => new(
        _repo.Object,
        Options.Create(new CompanySettings
        {
            Name = "Demo Corp",
            LegalName = "Demo Corp SA de CV",
            Rfc = "DEM010101AAA",
            Address = "Calle 1",
            City = "CDMX",
            Phone = "555",
            Email = "demo@local"
        }),
        _env.Object);

    public CustodyPdfServiceTests()
    {
        _env.Setup(e => e.ContentRootPath).Returns(Path.GetTempPath());
    }

    [Fact]
    public async Task GenerateAsync_WhenMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetCustodyFormAsync(id, default)).ReturnsAsync((CustodyFormDetailDto?)null);

        var result = await Sut.GenerateAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.Error);
    }

    [Fact]
    public async Task GenerateAsync_WhenFound_ReturnsPdfBytes()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetCustodyFormAsync(id, default)).ReturnsAsync(new CustodyFormDetailDto
        {
            Id = id,
            Folio = "RES-2026-0001",
            EmployeeId = Guid.NewGuid(),
            EmployeeNumber = "E001",
            EmployeeName = "Ana López",
            Status = CustodyFormStatus.Issued,
            IssuedAt = DateTime.UtcNow,
            IssuedByUserId = Guid.NewGuid(),
            LineCount = 1,
            Lines =
            [
                new CustodyFormLineDto
                {
                    Id = Guid.NewGuid(),
                    AssetId = Guid.NewGuid(),
                    AssetCode = "EQ-001",
                    SerialNumber = "SN-1",
                    AssetKind = AssetKind.Equipment,
                    CategoryName = "Laptop",
                    BrandName = "Dell",
                    ModelName = "Latitude",
                    Specs = "Core i7 16GB",
                    Quantity = 1,
                    ConditionOnDelivery = AssetCondition.New
                }
            ]
        });

        var result = await Sut.GenerateAsync(id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.Content.Length > 100);
        Assert.Equal("application/pdf", result.Data.ContentType);
        Assert.Contains("RES-2026-0001", result.Data.FileName);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(result.Data.Content, 0, 4));
    }
}
