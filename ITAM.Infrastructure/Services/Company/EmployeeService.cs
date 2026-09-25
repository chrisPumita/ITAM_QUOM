using ITAM.Domain.Entities.Company;
using ITAM.Domain.Interfaces.Repositories.Company;
using ITAM.Domain.Interfaces.Services.Company;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Company;

namespace ITAM.Infrastructure.Services.Company;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repo;

    public EmployeeService(IEmployeeRepository repo) => _repo = repo;

    public async Task<Result<List<EmployeeListDto>>> ListAsync(bool? onlyActive)
    {
        var items = await _repo.ListAsync(onlyActive);
        return Ok(items.Select(Map).ToList(), "Empleados obtenidos.");
    }

    public async Task<Result<EmployeeListDto>> GetAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<EmployeeListDto>("Empleado no encontrado.", "NotFound");
        return Ok(Map(entity), "OK");
    }

    public async Task<Result<Guid>> CreateAsync(EmployeeUpsertDto dto)
    {
        var validation = await ValidateAsync(dto, excludeId: null);
        if (validation is not null)
            return validation;

        var entity = new Employee
        {
            EmployeeNumber = dto.EmployeeNumber.Trim(),
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim(),
            Department = string.IsNullOrWhiteSpace(dto.Department) ? null : dto.Department.Trim(),
            IsActive = dto.IsActive,
            IdentityUserId = dto.IdentityUserId
        };
        await _repo.AddAsync(entity);
        return Ok(entity.Id, "Empleado creado.");
    }

    public async Task<Result<bool>> UpdateAsync(Guid id, EmployeeUpsertDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return Fail<bool>("Empleado no encontrado.", "NotFound");

        var validation = await ValidateAsync(dto, excludeId: id);
        if (validation is not null)
            return Fail<bool>(validation.Message, validation.Error);

        entity.EmployeeNumber = dto.EmployeeNumber.Trim();
        entity.FullName = dto.FullName.Trim();
        entity.Email = dto.Email.Trim();
        entity.Department = string.IsNullOrWhiteSpace(dto.Department) ? null : dto.Department.Trim();
        entity.IsActive = dto.IsActive;
        entity.IdentityUserId = dto.IdentityUserId;
        entity.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(entity);
        return Ok(true, "Empleado actualizado.");
    }

    private async Task<Result<Guid>?> ValidateAsync(EmployeeUpsertDto dto, Guid? excludeId)
    {
        if (string.IsNullOrWhiteSpace(dto.EmployeeNumber))
            return Fail<Guid>("El número de empleado es obligatorio.", "Validation");
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return Fail<Guid>("El nombre es obligatorio.", "Validation");
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Fail<Guid>("El correo es obligatorio.", "Validation");

        if (await _repo.NumberExistsAsync(dto.EmployeeNumber, excludeId))
            return Fail<Guid>("Ya existe un empleado con ese número.", "Duplicate");

        if (await _repo.EmailExistsAsync(dto.Email, excludeId))
            return Fail<Guid>("Ya existe un empleado con ese correo.", "Duplicate");

        if (dto.IdentityUserId.HasValue)
        {
            if (!await _repo.IdentityUserExistsAsync(dto.IdentityUserId.Value))
                return Fail<Guid>("El usuario Identity vinculado no existe.", "Validation");
        }

        return null;
    }

    private static EmployeeListDto Map(Employee x) => new()
    {
        Id = x.Id,
        EmployeeNumber = x.EmployeeNumber,
        FullName = x.FullName,
        Email = x.Email,
        Department = x.Department,
        IsActive = x.IsActive,
        IdentityUserId = x.IdentityUserId
    };

    private static Result<T> Ok<T>(T data, string message) => new()
    {
        IsSuccess = true,
        Message = message,
        Data = data
    };

    private static Result<T> Fail<T>(string message, string error) => new()
    {
        IsSuccess = false,
        Message = message,
        Error = error
    };
}
