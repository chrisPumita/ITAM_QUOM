using ClosedXML.Excel;
using ITAM.Application.Services.Assets;
using ITAM.Domain.Entities.Assets;
using ITAM.Domain.Interfaces.Repositories.Assets;
using ITAM.Domain.Interfaces.Services;
using ITAM.Domain.Interfaces.Services.Assets;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assets;
using ITAM.Shared.Enums;

namespace ITAM.Infrastructure.Services.Assets;

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
            "Código", "Serie", "Tipo", "Estado", "Condición", "Categoría", "Descripción",
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
            ws.Cell(row, 5).Value = a.Condition.ToSpanish();
            ws.Cell(row, 6).Value = a.Model?.Category?.Name ?? string.Empty;
            ws.Cell(row, 7).Value = description;
            ws.Cell(row, 8).Value = a.OwnershipType.ToSpanish();
            ws.Cell(row, 9).Value = a.Supplier?.Name ?? string.Empty;
            ws.Cell(row, 10).Value = a.Location?.Name ?? string.Empty;
            ws.Cell(row, 11).Value = a.CurrentEmployee?.FullName ?? string.Empty;
            ws.Cell(row, 12).Value = a.Imei ?? string.Empty;
            ws.Cell(row, 13).Value = a.ContractNumber ?? string.Empty;
            if (a.PurchaseDate is not null)
                ws.Cell(row, 14).Value = a.PurchaseDate.Value.ToDateTime(TimeOnly.MinValue);
            if (a.RentalEndDate is not null)
                ws.Cell(row, 15).Value = a.RentalEndDate.Value.ToDateTime(TimeOnly.MinValue);
            if (a.WarrantyEndDate is not null)
                ws.Cell(row, 16).Value = a.WarrantyEndDate.Value.ToDateTime(TimeOnly.MinValue);
            row++;
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns(1, headers.Length).AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public static ExportFile BuildImportTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Alta");
        var headers = new[]
        {
            "Tipo", "Marca", "Modelo", "Categoria", "Specs", "Serie", "Propiedad",
            "Proveedor", "Ubicacion", "Compra", "FinGarantia", "IMEI", "Contrato"
        };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;

        var samples = BuildSampleRows();
        var row = 2;
        foreach (var s in samples)
        {
            ws.Cell(row, 1).Value = s.Tipo;
            ws.Cell(row, 2).Value = s.Marca;
            ws.Cell(row, 3).Value = s.Modelo;
            ws.Cell(row, 4).Value = s.Categoria;
            ws.Cell(row, 5).Value = s.Specs;
            ws.Cell(row, 6).Value = s.Serie;
            ws.Cell(row, 7).Value = s.Propiedad;
            ws.Cell(row, 8).Value = s.Proveedor;
            ws.Cell(row, 9).Value = s.Ubicacion;
            if (s.Compra is not null) ws.Cell(row, 10).Value = s.Compra.Value;
            if (s.FinGarantia is not null) ws.Cell(row, 11).Value = s.FinGarantia.Value;
            ws.Cell(row, 12).Value = s.Imei;
            ws.Cell(row, 13).Value = s.Contrato;
            row++;
        }

        var help = wb.Worksheets.Add("Instrucciones");
        help.Cell(1, 1).Value = "Tipo: Equipo | Accesorio (o Equipment | Accessory)";
        help.Cell(2, 1).Value = "Marca: texto (se guarda en MAYÚSCULAS; se crea si no existe)";
        help.Cell(3, 1).Value = "Modelo: texto (se crea bajo la marca si no existe)";
        help.Cell(4, 1).Value = "Categoria: texto (default General; se crea si no existe)";
        help.Cell(5, 1).Value = "Serie: obligatoria para Equipo; única";
        help.Cell(6, 1).Value = "Propiedad: Propio | Rentado. Si Rentado, Proveedor obligatorio y debe existir en catálogo";
        help.Cell(7, 1).Value = "Ubicacion / Proveedor: nombres exactos del catálogo (no se crean solos)";
        help.Cell(8, 1).Value = "El código de etiqueta (SKU) se genera automáticamente al importar";
        help.Cell(9, 1).Value = "La hoja Alta trae ~50 ejemplos; puede borrar o editar filas antes de subir";
        help.Columns().AdjustToContents();
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return new ExportFile
        {
            Content = ms.ToArray(),
            FileName = "Plantilla_Alta_Activos.xlsx"
        };
    }

    public static List<AssetImportRowDto> ParseImportWorkbook(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First(w => w.Name.Equals("Alta", StringComparison.OrdinalIgnoreCase)
                                          || w == wb.Worksheets.First());
        var rows = new List<AssetImportRowDto>();
        var last = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= last; r++)
        {
            var kindRaw = ws.Cell(r, 1).GetString().Trim();
            var brand = ws.Cell(r, 2).GetString().Trim();
            var model = ws.Cell(r, 3).GetString().Trim();
            if (string.IsNullOrWhiteSpace(kindRaw) && string.IsNullOrWhiteSpace(brand) && string.IsNullOrWhiteSpace(model))
                continue;

            rows.Add(new AssetImportRowDto
            {
                RowNumber = r,
                Kind = ParseKind(kindRaw),
                BrandName = brand,
                ModelName = model,
                CategoryName = NullIfEmpty(ws.Cell(r, 4).GetString()),
                Specs = NullIfEmpty(ws.Cell(r, 5).GetString()),
                SerialNumber = NullIfEmpty(ws.Cell(r, 6).GetString()),
                OwnershipType = ParseOwnership(ws.Cell(r, 7).GetString()),
                SupplierName = NullIfEmpty(ws.Cell(r, 8).GetString()),
                LocationName = NullIfEmpty(ws.Cell(r, 9).GetString()),
                PurchaseDate = TryDate(ws.Cell(r, 10)),
                WarrantyEndDate = TryDate(ws.Cell(r, 11)),
                Imei = NullIfEmpty(ws.Cell(r, 12).GetString()),
                ContractNumber = NullIfEmpty(ws.Cell(r, 13).GetString())
            });
        }

        return rows;
    }

    private static List<(string Tipo, string Marca, string Modelo, string Categoria, string Specs, string Serie, string Propiedad, string Proveedor, string Ubicacion, DateTime? Compra, DateTime? FinGarantia, string Imei, string Contrato)> BuildSampleRows()
    {
        var list = new List<(string, string, string, string, string, string, string, string, string, DateTime?, DateTime?, string, string)>();
        var catalog = new (string Brand, string Model, string Cat, string Specs, string Kind)[]
        {
            ("DELL", "Latitude 5440", "Laptops", "Core i5, 16GB, 512GB SSD", "Equipo"),
            ("DELL", "Latitude 5540", "Laptops", "Core i7, 32GB, 1TB SSD", "Equipo"),
            ("DELL", "OptiPlex 7010", "Desktops", "Core i5, 16GB, 512GB SSD", "Equipo"),
            ("DELL", "UltraSharp U2422H", "Monitores", "24\" IPS", "Equipo"),
            ("HP", "EliteBook 840 G10", "Laptops", "Core i5, 16GB, 512GB SSD", "Equipo"),
            ("HP", "ProBook 450 G10", "Laptops", "Core i5, 8GB, 256GB SSD", "Equipo"),
            ("HP", "E24 G5", "Monitores", "23.8\" FHD", "Equipo"),
            ("LENOVO", "ThinkPad T14 Gen 4", "Laptops", "Ryzen 5, 16GB, 512GB SSD", "Equipo"),
            ("LENOVO", "ThinkCentre M70q", "Desktops", "Core i5, 16GB, 512GB SSD", "Equipo"),
            ("APPLE", "MacBook Air M2", "Laptops", "8GB, 256GB", "Equipo"),
            ("APPLE", "MacBook Pro 14 M3", "Laptops", "16GB, 512GB", "Equipo"),
            ("APPLE", "iPhone 15", "Moviles", "128GB", "Equipo"),
            ("SAMSUNG", "Galaxy S24", "Moviles", "256GB", "Equipo"),
            ("SAMSUNG", "Galaxy Tab S9", "Tablets", "128GB", "Equipo"),
            ("MICROSOFT", "Surface Laptop 5", "Laptops", "i5, 16GB, 512GB", "Equipo"),
            ("LOGITECH", "MX Master 3S", "Perifericos", "Mouse inalambrico", "Accesorio"),
            ("LOGITECH", "MX Keys", "Perifericos", "Teclado inalambrico", "Accesorio"),
            ("LOGITECH", "C920e", "Perifericos", "Webcam HD", "Accesorio"),
            ("KINGSTON", "DataTraveler 64GB", "Almacenamiento", "USB 3.2", "Accesorio"),
            ("CISCO", "IP Phone 8841", "Telefonia", "VoIP", "Equipo"),
        };

        var loc = "Oficina CDMX - Piso 3";
        var baseDate = new DateTime(2025, 1, 15);
        for (var i = 0; i < 50; i++)
        {
            var c = catalog[i % catalog.Length];
            var sn = c.Kind == "Equipo" ? $"SN{20260000 + i + 1}" : "";
            var warrantyYears = c.Kind == "Equipo" ? 3 : 1;
            list.Add((
                c.Kind,
                c.Brand,
                c.Model,
                c.Cat,
                c.Specs,
                sn,
                "Propio",
                "",
                loc,
                baseDate.AddDays(i * 3),
                baseDate.AddDays(i * 3).AddYears(warrantyYears),
                "",
                ""
            ));
        }

        return list;
    }

    private static string? NullIfEmpty(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static DateOnly? TryDate(IXLCell cell)
    {
        if (cell.TryGetValue(out DateTime dt))
            return DateOnly.FromDateTime(dt);
        if (DateOnly.TryParse(cell.GetString().Trim(), out var d))
            return d;
        return null;
    }

    private static AssetKind ParseKind(string raw)
    {
        if (Enum.TryParse<AssetKind>(raw, true, out var k) && Enum.IsDefined(k))
            return k;
        if (string.Equals(raw, "Equipo", StringComparison.OrdinalIgnoreCase))
            return AssetKind.Equipment;
        if (string.Equals(raw, "Accesorio", StringComparison.OrdinalIgnoreCase))
            return AssetKind.Accessory;
        return AssetKind.Equipment;
    }

    private static OwnershipType ParseOwnership(string raw)
    {
        if (Enum.TryParse<OwnershipType>(raw, true, out var o) && Enum.IsDefined(o))
            return o;
        if (string.Equals(raw, "Propio", StringComparison.OrdinalIgnoreCase))
            return OwnershipType.Owned;
        if (string.Equals(raw, "Rentado", StringComparison.OrdinalIgnoreCase))
            return OwnershipType.Rented;
        return OwnershipType.Owned;
    }
}
