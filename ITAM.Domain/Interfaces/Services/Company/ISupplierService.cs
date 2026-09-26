using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Company;

namespace ITAM.Domain.Interfaces.Services.Company;

public interface ISupplierService
{
    Task<Result<List<SupplierListDto>>> ListAsync(bool? onlyActive);
    Task<Result<SupplierListDto>> GetAsync(Guid id);
    Task<Result<Guid>> CreateAsync(SupplierUpsertDto dto);
    Task<Result<bool>> UpdateAsync(Guid id, SupplierUpsertDto dto);
}
