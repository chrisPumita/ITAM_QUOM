using ITAM.Domain.Entities.Company;
using ITAM.Domain.Interfaces.Repositories.Company;
using ITAM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Infrastructure.Repositories.Company;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly ApplicationDbContext _db;

    public EmployeeRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Employee>> ListAsync(bool? onlyActive, CancellationToken ct = default)
    {
        var q = _db.Employees.AsNoTracking();
        if (onlyActive == true)
            q = q.Where(x => x.IsActive);
        return q.OrderBy(x => x.FullName).ToListAsync(ct);
    }

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Employees.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> NumberExistsAsync(string employeeNumber, Guid? excludeId, CancellationToken ct = default)
    {
        var normalized = employeeNumber.Trim();
        return _db.Employees.AnyAsync(
            x => x.EmployeeNumber == normalized && (!excludeId.HasValue || x.Id != excludeId.Value), ct);
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _db.Employees.AnyAsync(
            x => x.Email.ToLower() == normalized && (!excludeId.HasValue || x.Id != excludeId.Value), ct);
    }

    public Task<bool> IdentityUserExistsAsync(Guid identityUserId, CancellationToken ct = default)
        => _db.Users.AnyAsync(x => x.Id == identityUserId, ct);

    public async Task<Employee> AddAsync(Employee entity, CancellationToken ct = default)
    {
        _db.Employees.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(Employee entity, CancellationToken ct = default)
    {
        _db.Employees.Update(entity);
        await _db.SaveChangesAsync(ct);
    }
}
