using ITAM.Domain.Entities.Company;
using ITAM.Domain.Interfaces.Repositories.Company;
using ITAM.Application.Services.Company;
using ITAM.Shared.Dtos.Company;
using Moq;

namespace ITAM.Tests.Company;

public class SupplierServiceTests
{
    private readonly Mock<ISupplierRepository> _repo = new();
    private SupplierService Sut => new(_repo.Object);

    [Fact]
    public async Task CreateAsync_WhenValid_ReturnsId()
    {
        _repo.Setup(r => r.NameExistsAsync("Dell México", null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<Supplier>(), default))
            .ReturnsAsync((Supplier e, CancellationToken _) => e);

        var result = await Sut.CreateAsync(new SupplierUpsertDto
        {
            Name = " Dell México ",
            OffersPurchase = true,
            OffersRental = true
        });

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Data);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateName_ReturnsDuplicate()
    {
        _repo.Setup(r => r.NameExistsAsync("Dell", null, default)).ReturnsAsync(true);

        var result = await Sut.CreateAsync(new SupplierUpsertDto { Name = "Dell" });

        Assert.False(result.IsSuccess);
        Assert.Equal("Duplicate", result.Error);
        _repo.Verify(r => r.AddAsync(It.IsAny<Supplier>(), default), Times.Never);
    }

    [Fact]
    public async Task GetAsync_WhenMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Supplier?)null);

        var result = await Sut.GetAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.Error);
    }
}
