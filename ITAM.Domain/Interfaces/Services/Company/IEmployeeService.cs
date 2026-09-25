using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Company;

namespace ITAM.Domain.Interfaces.Services.Company;

public interface IEmployeeService
{
    Task<Result<List<EmployeeListDto>>> ListAsync(bool? onlyActive);
    Task<Result<EmployeeListDto>> GetAsync(Guid id);
    Task<Result<Guid>> CreateAsync(EmployeeUpsertDto dto);
    Task<Result<bool>> UpdateAsync(Guid id, EmployeeUpsertDto dto);
}
