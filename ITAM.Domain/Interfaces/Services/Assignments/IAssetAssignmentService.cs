using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assignments;

namespace ITAM.Domain.Interfaces.Services.Assignments;

public interface IAssetAssignmentService
{
    Task<Result<AssignAssetsResultDto>> AssignAsync(AssignAssetsDto dto, Guid performedByUserId);
    Task<Result<ReturnAssetResultDto>> ReturnAsync(ReturnAssetDto dto, Guid performedByUserId);

    Task<Result<List<AssignmentListDto>>> ListAssignmentsAsync(Guid? employeeId, Guid? assetId, bool onlyActive);
    Task<Result<AssignmentListDto>> GetAssignmentAsync(Guid id);
    Task<Result<List<CustodyFormListDto>>> ListCustodyFormsAsync(Guid? employeeId, DateTime? fromUtc, DateTime? toUtc);
    Task<Result<CustodyFormDetailDto>> GetCustodyFormAsync(Guid id);
    Task<Result<List<AssetMovementListDto>>> ListMovementsAsync(
        Guid? assetId, Guid? employeeId, DateTime? fromUtc, DateTime? toUtc);
}
