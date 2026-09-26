using ITAM.Domain.Entities.Company;
using ITAM.Domain.Interfaces.Repositories.Company;
using ITAM.Application.Services.Company;
using ITAM.Shared.Dtos.Company;
using Moq;

namespace ITAM.Tests.Company;

public class EmployeeServiceTests
{
    private readonly Mock<IEmployeeRepository> _repo = new();
    private EmployeeService Sut => new(_repo.Object);

    [Fact]
    public async Task CreateAsync_WhenValid_ReturnsId()
    {
        _repo.Setup(r => r.NumberExistsAsync("E-001", null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.EmailExistsAsync("ana@corp.com", null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<Employee>(), default))
            .ReturnsAsync((Employee e, CancellationToken _) => e);

        var result = await Sut.CreateAsync(new EmployeeUpsertDto
        {
            EmployeeNumber = " E-001 ",
            FullName = "Ana López",
            Email = "ana@corp.com",
            Department = "TI"
        });

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Data);
        _repo.Verify(r => r.AddAsync(It.Is<Employee>(e => e.EmployeeNumber == "E-001"), default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateNumber_ReturnsDuplicate()
    {
        _repo.Setup(r => r.NumberExistsAsync("E-001", null, default)).ReturnsAsync(true);

        var result = await Sut.CreateAsync(new EmployeeUpsertDto
        {
            EmployeeNumber = "E-001",
            FullName = "Ana",
            Email = "ana@corp.com"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Duplicate", result.Error);
        _repo.Verify(r => r.AddAsync(It.IsAny<Employee>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenIdentityUserMissing_ReturnsValidation()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.NumberExistsAsync("E-002", null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.EmailExistsAsync("bob@corp.com", null, default)).ReturnsAsync(false);
        _repo.Setup(r => r.IdentityUserExistsAsync(userId, default)).ReturnsAsync(false);

        var result = await Sut.CreateAsync(new EmployeeUpsertDto
        {
            EmployeeNumber = "E-002",
            FullName = "Bob",
            Email = "bob@corp.com",
            IdentityUserId = userId
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Validation", result.Error);
    }

    [Fact]
    public async Task GetAsync_WhenMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Employee?)null);

        var result = await Sut.GetAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenDuplicateEmail_ReturnsDuplicate()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(new Employee
        {
            Id = id,
            EmployeeNumber = "E-001",
            FullName = "Ana",
            Email = "ana@corp.com"
        });
        _repo.Setup(r => r.NumberExistsAsync("E-001", id, default)).ReturnsAsync(false);
        _repo.Setup(r => r.EmailExistsAsync("other@corp.com", id, default)).ReturnsAsync(true);

        var result = await Sut.UpdateAsync(id, new EmployeeUpsertDto
        {
            EmployeeNumber = "E-001",
            FullName = "Ana",
            Email = "other@corp.com"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Duplicate", result.Error);
    }
}
