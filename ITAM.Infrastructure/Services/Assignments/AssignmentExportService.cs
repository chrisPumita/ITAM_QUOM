using ClosedXML.Excel;
using ITAM.Domain.Interfaces.Repositories.Assignments;
using ITAM.Domain.Interfaces.Services;
using ITAM.Domain.Interfaces.Services.Assignments;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assignments;
using ITAM.Shared.Enums;

namespace ITAM.Infrastructure.Services.Assignments;

public sealed class AssignmentExportService : IAssignmentExportService
{
    private readonly IAssetAssignmentRepository _repo;

    public AssignmentExportService(IAssetAssignmentRepository repo) => _repo = repo;

    public async Task<Result<ExportFile>> ExportMovementsAsync(
        Guid? assetId,
        Guid? employeeId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default)
    {
        var items = await _repo.ListMovementsAsync(assetId, employeeId, fromUtc, toUtc, ct);
        if (items.Count == 0)
            return Empty("No hay movimientos para exportar.");

        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        return Ok(new ExportFile
        {
            Content = BuildMovementsWorkbook(items),
            FileName = $"Movimientos_{stamp}.xlsx"
        });
    }

    private static byte[] BuildMovementsWorkbook(IReadOnlyList<AssetMovementListDto> items)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Movimientos");

        var headers = new[]
        {
            "Código activo", "Serie", "Tipo", "Categoría", "Descripción",
            "Tipo movimiento", "Estado origen", "Estado destino",
            "Ubicación origen", "Ubicación destino", "Colaborador",
            "Ejecutó", "Folio responsiva", "Notas", "Fecha"
        };
        WriteHeader(ws, headers);

        var row = 2;
        foreach (var m in items)
        {
            ws.Cell(row, 1).Value = m.AssetCode;
            ws.Cell(row, 2).Value = m.SerialNumber ?? string.Empty;
            ws.Cell(row, 3).Value = m.AssetKind.ToSpanish();
            ws.Cell(row, 4).Value = m.CategoryName;
            ws.Cell(row, 5).Value = m.Description;
            ws.Cell(row, 6).Value = m.MovementType.ToSpanish();
            ws.Cell(row, 7).Value = m.FromStatus?.ToSpanish() ?? string.Empty;
            ws.Cell(row, 8).Value = m.ToStatus?.ToSpanish() ?? string.Empty;
            ws.Cell(row, 9).Value = m.FromLocationName ?? string.Empty;
            ws.Cell(row, 10).Value = m.ToLocationName ?? string.Empty;
            ws.Cell(row, 11).Value = m.EmployeeName ?? string.Empty;
            ws.Cell(row, 12).Value = m.PerformedByUserName ?? string.Empty;
            ws.Cell(row, 13).Value = m.CustodyFolio ?? string.Empty;
            ws.Cell(row, 14).Value = m.Notes ?? string.Empty;
            ws.Cell(row, 15).Value = m.OccurredAt;
            ws.Cell(row, 15).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
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

    private static Result<ExportFile> Empty(string message) => new()
    {
        IsSuccess = false,
        Message = message,
        Error = "NotFound"
    };
}
