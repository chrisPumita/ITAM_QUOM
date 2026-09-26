using ITAM.Domain.Interfaces.Services;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Enums;

namespace ITAM.Domain.Interfaces.Services.Assignments;

/// <summary>Export Excel del historial de movimientos.</summary>
public interface IAssignmentExportService
{
    Task<Result<ExportFile>> ExportMovementsAsync(
        Guid? assetId,
        Guid? employeeId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default);
}
