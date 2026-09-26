using ITAM.Shared.Dtos.Assignments;

namespace ITAM.Domain.Interfaces.Repositories.Assignments;

/// <summary>
/// Persistencia de asignación/devolución (ADO.NET + SP) y lecturas (Dapper).
/// </summary>
public interface IAssetAssignmentRepository
{
    Task<AssignAssetsResultDto> AssignAsync(
        AssignAssetsDto dto,
        Guid performedByUserId,
        CancellationToken ct = default);

    Task<ReturnAssetResultDto> ReturnAsync(
        ReturnAssetDto dto,
        Guid performedByUserId,
        CancellationToken ct = default);

    Task<List<AssignmentListDto>> ListAssignmentsAsync(
        Guid? employeeId,
        Guid? assetId,
        bool onlyActive,
        CancellationToken ct = default);

    Task<AssignmentListDto?> GetAssignmentAsync(Guid id, CancellationToken ct = default);

    Task<List<CustodyFormListDto>> ListCustodyFormsAsync(
        Guid? employeeId,
        CancellationToken ct = default);

    Task<CustodyFormDetailDto?> GetCustodyFormAsync(Guid id, CancellationToken ct = default);

    Task<List<AssetMovementListDto>> ListMovementsAsync(
        Guid? assetId,
        Guid? employeeId,
        CancellationToken ct = default);
}
