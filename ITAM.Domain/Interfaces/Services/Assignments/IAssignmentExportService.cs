using ITAM.Shared.Dtos.Apis;

namespace ITAM.Domain.Interfaces.Services.Assignments;

/// <summary>Exporta listados de asignaciones / movimientos a Excel.</summary>
public interface IAssignmentExportService
{
    Task<Result<ExportFile>> ExportAssignmentsAsync(
        Guid? employeeId,
        Guid? assetId,
        bool onlyActive,
        CancellationToken ct = default);

    Task<Result<ExportFile>> ExportMovementsAsync(
        Guid? assetId,
        Guid? employeeId,
        CancellationToken ct = default);
}

public sealed class ExportFile
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    public string ContentType { get; init; } =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
