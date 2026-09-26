using ITAM.Domain.Entities.Company;

namespace ITAM.Domain.Interfaces.Repositories.Company;

public interface IEmployeeRepository
{
    Task<List<Employee>> ListAsync(bool? onlyActive, CancellationToken ct = default);
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> NumberExistsAsync(string employeeNumber, Guid? excludeId, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken ct = default);
    Task<bool> IdentityUserExistsAsync(Guid identityUserId, CancellationToken ct = default);
    Task<Employee> AddAsync(Employee entity, CancellationToken ct = default);
    Task UpdateAsync(Employee entity, CancellationToken ct = default);
}
