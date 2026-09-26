using ClosedXML.Excel;
using ITAM.Domain.Entities.Assets;
using ITAM.Domain.Interfaces.Repositories.Assets;
using ITAM.Domain.Interfaces.Services;
using ITAM.Domain.Interfaces.Services.Assets;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assets;
using ITAM.Shared.Enums;

namespace ITAM.Infrastructure.Services.Assets;

/// <summary>Export Excel del inventario de activos (universo + filtros).</summary>
public sealed class AssetExportService : IAssetExportService
{
    private readonly IAssetRepository _repo;

    public AssetExportService(IAssetRepository repo) => _repo = repo;

    public async Task<Result<ExportFile>> ExportAsync(AssetListQuery query, CancellationToken ct = default)
    {
        query.Normalize();

        var filterResult = AssetService.TryBuildFilter(query);
        if (!filterResult.IsSuccess || filterResult.Data is null)
        {
            return new Result<ExportFile>
            {
                IsSuccess = false,
                Message = filterResult.Message,
                Error = filterResult.Error ?? "Validation"
            };
        }

        var items = await _repo.ListAsync(filterResult.Data, ct);

        if (items.Count == 0)
        {
            return new Result<ExportFile>
            {
                IsSuccess = false,
                Message = "No hay activos para exportar.",
                Error = "NotFound"
            };
        }

        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        return new Result<ExportFile>
        {
            IsSuccess = true,
            Message = "OK",
            Data = new ExportFile
            {
                Content = BuildWorkbook(items),
                FileName = $"Activos_{stamp}.xlsx"
            }
        };
    }

    private static byte[] BuildWorkbook(IReadOnlyList<Asset> items)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Activos");

        var headers = new[]
        {
            "Código", "Serie", "Tipo", "Estado", "Categoría", "Descripción",
            "Propiedad", "Proveedor", "Ubicación", "Colaborador actual",
            "IMEI", "Contrato", "Compra", "Fin renta", "Fin garantía"
        };

        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        ws.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 2;
        foreach (var a in items)
        {
            var brand = a.Model?.Brand?.Name ?? string.Empty;
            var model = a.Model?.Name ?? string.Empty;
            var specs = a.Model?.Specs;
            var title = string.Join(" ", new[] { brand, model }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var description = string.Join(" · ", new[] { title, specs }.Where(s => !string.IsNullOrWhiteSpace(s)));

            ws.Cell(row, 1).Value = a.AssetCode;
            ws.Cell(row, 2).Value = a.SerialNumber ?? string.Empty;
            ws.Cell(row, 3).Value = a.Kind.ToSpanish();
            ws.Cell(row, 4).Value = a.Status.ToSpanish();
            ws.Cell(row, 5).Value = a.Model?.Category?.Name ?? string.Empty;
            ws.Cell(row, 6).Value = description;
            ws.Cell(row, 7).Value = a.OwnershipType.ToSpanish();
            ws.Cell(row, 8).Value = a.Supplier?.Name ?? string.Empty;
            ws.Cell(row, 9).Value = a.Location?.Name ?? string.Empty;
            ws.Cell(row, 10).Value = a.CurrentEmployee?.FullName ?? string.Empty;
            ws.Cell(row, 11).Value = a.Imei ?? string.Empty;
            ws.Cell(row, 12).Value = a.ContractNumber ?? string.Empty;
            if (a.PurchaseDate is not null)
                ws.Cell(row, 13).Value = a.PurchaseDate.Value.ToDateTime(TimeOnly.MinValue);
            if (a.RentalEndDate is not null)
                ws.Cell(row, 14).Value = a.RentalEndDate.Value.ToDateTime(TimeOnly.MinValue);
            if (a.WarrantyEndDate is not null)
                ws.Cell(row, 15).Value = a.WarrantyEndDate.Value.ToDateTime(TimeOnly.MinValue);
            row++;
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns(1, headers.Length).AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
