using ITAM.Domain.Interfaces.Repositories.Assignments;
using ITAM.Domain.Interfaces.Services.Assignments;
using ITAM.Infrastructure.Repositories.Assignments;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assignments;

namespace ITAM.Infrastructure.Services.Assignments;

public sealed class AssetAssignmentService : IAssetAssignmentService
{
    private readonly IAssetAssignmentRepository _repo;

    public AssetAssignmentService(IAssetAssignmentRepository repo) => _repo = repo;

    public async Task<Result<AssignAssetsResultDto>> AssignAsync(AssignAssetsDto dto, Guid performedByUserId)
    {
        if (performedByUserId == Guid.Empty)
            return Fail<AssignAssetsResultDto>("Usuario autenticado requerido.", "Validation");

        if (dto.Lines is null || dto.Lines.Count == 0)
            return Fail<AssignAssetsResultDto>("Debe indicar al menos un activo.", "Validation");

        if (dto.Lines.Select(l => l.AssetId).Distinct().Count() != dto.Lines.Count)
            return Fail<AssignAssetsResultDto>("Hay activos duplicados en la solicitud.", "Validation");

        try
        {
            var data = await _repo.AssignAsync(dto, performedByUserId);
            return Ok(data, $"Asignación exitosa. Folio {data.Folio}.");
        }
        catch (AssetAssignmentRepositoryException ex)
        {
            return Fail<AssignAssetsResultDto>(ex.Message, ex.ErrorCode == "Conflict" ? "Duplicate" : ex.ErrorCode);
        }
    }

    public async Task<Result<ReturnAssetResultDto>> ReturnAsync(ReturnAssetDto dto, Guid performedByUserId)
    {
        if (performedByUserId == Guid.Empty)
            return Fail<ReturnAssetResultDto>("Usuario autenticado requerido.", "Validation");

        try
        {
            var data = await _repo.ReturnAsync(dto, performedByUserId);
            return Ok(data, "Devolución registrada.");
        }
        catch (AssetAssignmentRepositoryException ex)
        {
            return Fail<ReturnAssetResultDto>(ex.Message, ex.ErrorCode);
        }
    }

    public async Task<Result<List<AssignmentListDto>>> ListAssignmentsAsync(
        Guid? employeeId, Guid? assetId, bool onlyActive)
    {
        var items = await _repo.ListAssignmentsAsync(employeeId, assetId, onlyActive);
        return Ok(items, "OK");
    }

    public async Task<Result<AssignmentListDto>> GetAssignmentAsync(Guid id)
    {
        var item = await _repo.GetAssignmentAsync(id);
        return item is null
            ? Fail<AssignmentListDto>("Asignación no encontrada.", "NotFound")
            : Ok(item, "OK");
    }

    public async Task<Result<List<CustodyFormListDto>>> ListCustodyFormsAsync(
        Guid? employeeId, DateTime? fromUtc, DateTime? toUtc)
    {
        var items = await _repo.ListCustodyFormsAsync(employeeId, fromUtc, toUtc);
        return Ok(items, "OK");
    }

    public async Task<Result<CustodyFormDetailDto>> GetCustodyFormAsync(Guid id)
    {
        var item = await _repo.GetCustodyFormAsync(id);
        return item is null
            ? Fail<CustodyFormDetailDto>("Responsiva no encontrada.", "NotFound")
            : Ok(item, "OK");
    }

    public async Task<Result<List<AssetMovementListDto>>> ListMovementsAsync(
        Guid? assetId, Guid? employeeId, DateTime? fromUtc, DateTime? toUtc)
    {
        var items = await _repo.ListMovementsAsync(assetId, employeeId, fromUtc, toUtc);
        return Ok(items, "OK");
    }

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
