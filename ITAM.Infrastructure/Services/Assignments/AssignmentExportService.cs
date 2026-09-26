using ClosedXML.Excel;
using ITAM.Domain.Interfaces.Repositories.Assignments;
using ITAM.Domain.Interfaces.Services.Assignments;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assignments;
using ITAM.Shared.Enums;

namespace ITAM.Infrastructure.Services.Assignments;

/// <summary>Export Excel (ClosedXML) de asignaciones y movimientos.</summary>
public sealed class AssignmentExportService : IAssignmentExportService
{
    private readonly IAssetAssignmentRepository _repo;

    public AssignmentExportService(IAssetAssignmentRepository repo) => _repo = repo;

    public async Task<Result<ExportFile>> ExportAssignmentsAsync(
        Guid? employeeId,
        Guid? assetId,
        bool onlyActive,
        CancellationToken ct = default)
    {
        var items = await _repo.ListAssignmentsAsync(employeeId, assetId, onlyActive, ct);
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        var bytes = BuildAssignmentsWorkbook(items);

        return Ok(new ExportFile
        {
            Content = bytes,
            FileName = $"Asignaciones_{stamp}.xlsx"
        });
    }

    public async Task<Result<ExportFile>> ExportMovementsAsync(
        Guid? assetId,
        Guid? employeeId,
        CancellationToken ct = default)
    {
        var items = await _repo.ListMovementsAsync(assetId, employeeId, ct);
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        var bytes = BuildMovementsWorkbook(items);

        return Ok(new ExportFile
        {
            Content = bytes,
            FileName = $"Movimientos_{stamp}.xlsx"
        });
    }

    private static byte[] BuildAssignmentsWorkbook(IReadOnlyList<AssignmentListDto> items)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Asignaciones");

        var headers = new[]
        {
            "Código activo", "Serie", "Tipo", "No. empleado", "Colaborador",
            "Asignado el", "Devuelto el", "Condición devolución", "Activa",
            "Notas", "Asignó", "Devolvió"
        };
        WriteHeader(ws, headers);

        var row = 2;
        foreach (var a in items)
        {
            ws.Cell(row, 1).Value = a.AssetCode;
            ws.Cell(row, 2).Value = a.SerialNumber ?? string.Empty;
            ws.Cell(row, 3).Value = a.AssetKind.ToSpanish();
            ws.Cell(row, 4).Value = a.EmployeeNumber;
            ws.Cell(row, 5).Value = a.EmployeeName;
            ws.Cell(row, 6).Value = a.AssignedAt;
            ws.Cell(row, 6).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
            if (a.ReturnedAt is not null)
            {
                ws.Cell(row, 7).Value = a.ReturnedAt.Value;
                ws.Cell(row, 7).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
            }
            ws.Cell(row, 8).Value = a.ReturnCondition?.ToSpanish() ?? string.Empty;
            ws.Cell(row, 9).Value = a.IsActive ? "Sí" : "No";
            ws.Cell(row, 10).Value = a.Notes ?? string.Empty;
            ws.Cell(row, 11).Value = a.AssignedByUserName ?? string.Empty;
            ws.Cell(row, 12).Value = a.ReturnedByUserName ?? string.Empty;
            row++;
        }

        FinalizeSheet(ws, headers.Length);
        return ToBytes(wb);
    }

    private static byte[] BuildMovementsWorkbook(IReadOnlyList<AssetMovementListDto> items)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Movimientos");

        var headers = new[]
        {
            "Código activo", "Tipo movimiento", "Estado origen", "Estado destino",
            "Ubicación origen", "Ubicación destino", "Colaborador",
            "Ejecutó", "Folio responsiva", "Notas", "Fecha"
        };
        WriteHeader(ws, headers);

        var row = 2;
        foreach (var m in items)
        {
            ws.Cell(row, 1).Value = m.AssetCode;
            ws.Cell(row, 2).Value = m.MovementType.ToSpanish();
            ws.Cell(row, 3).Value = m.FromStatus?.ToSpanish() ?? string.Empty;
            ws.Cell(row, 4).Value = m.ToStatus?.ToSpanish() ?? string.Empty;
            ws.Cell(row, 5).Value = m.FromLocationName ?? string.Empty;
            ws.Cell(row, 6).Value = m.ToLocationName ?? string.Empty;
            ws.Cell(row, 7).Value = m.EmployeeName ?? string.Empty;
            ws.Cell(row, 8).Value = m.PerformedByUserName ?? string.Empty;
            ws.Cell(row, 9).Value = m.CustodyFolio ?? string.Empty;
            ws.Cell(row, 10).Value = m.Notes ?? string.Empty;
            ws.Cell(row, 11).Value = m.OccurredAt;
            ws.Cell(row, 11).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
            row++;
        }

        FinalizeSheet(ws, headers.Length);
        return ToBytes(wb);
    }

    private static void WriteHeader(IXLWorksheet ws, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var range = ws.Range(1, 1, 1, headers.Count);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
    }

    private static void FinalizeSheet(IXLWorksheet ws, int columnCount)
    {
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, columnCount).AdjustToContents();
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static Result<ExportFile> Ok(ExportFile file) => new()
    {
        IsSuccess = true,
        Message = "OK",
        Data = file
    };
}
