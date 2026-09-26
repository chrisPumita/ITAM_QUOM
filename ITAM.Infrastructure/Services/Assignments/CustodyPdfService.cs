using ITAM.Domain.Interfaces.Repositories.Assignments;
using ITAM.Domain.Interfaces.Services.Assignments;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assignments;
using ITAM.Shared.Enums;
using ITAM.Shared.Services.Company;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ITAM.Infrastructure.Services.Assignments;

public sealed class CustodyPdfService : ICustodyPdfService
{
    private readonly IAssetAssignmentRepository _repo;
    private readonly CompanySettings _company;
    private readonly IHostEnvironment _env;

    static CustodyPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public CustodyPdfService(
        IAssetAssignmentRepository repo,
        IOptions<CompanySettings> company,
        IHostEnvironment env)
    {
        _repo = repo;
        _company = company.Value;
        _env = env;
    }

    public async Task<Result<CustodyPdfFile>> GenerateAsync(Guid custodyFormId, CancellationToken ct = default)
    {
        var form = await _repo.GetCustodyFormAsync(custodyFormId, ct);
        if (form is null)
        {
            return new Result<CustodyPdfFile>
            {
                IsSuccess = false,
                Message = "Responsiva no encontrada.",
                Error = "NotFound"
            };
        }

        var bytes = BuildPdf(form);
        var safeFolio = form.Folio.Replace('/', '-');

        return new Result<CustodyPdfFile>
        {
            IsSuccess = true,
            Message = "OK",
            Data = new CustodyPdfFile
            {
                Content = bytes,
                FileName = $"Responsiva_{safeFolio}.pdf"
            }
        };
    }

    private byte[] BuildPdf(CustodyFormDetailDto form)
    {
        var logoPath = ResolveLogoPath();
        var issued = form.IssuedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "—";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, logoPath, form.Folio));
                page.Content().Element(c => ComposeBody(c, form, issued));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                    t.Span($"  ·  {_company.Name}");
                });
            });
        }).GeneratePdf();
    }

    private void ComposeHeader(IContainer container, string? logoPath, string folio)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                if (logoPath is not null)
                {
                    row.ConstantItem(70).Height(50).Image(logoPath).FitArea();
                    row.ConstantItem(12);
                }

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(_company.Name).Bold().FontSize(14);
                    if (!string.IsNullOrWhiteSpace(_company.LegalName))
                        c.Item().Text(_company.LegalName).FontSize(9).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrWhiteSpace(_company.Rfc))
                        c.Item().Text($"RFC: {_company.Rfc}").FontSize(9);
                    if (!string.IsNullOrWhiteSpace(_company.Address))
                        c.Item().Text(_company.Address).FontSize(8).FontColor(Colors.Grey.Darken1);
                    var cityLine = string.Join(" · ", new[] { _company.City, _company.Phone, _company.Email }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrWhiteSpace(cityLine))
                        c.Item().Text(cityLine).FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(120).AlignRight().Column(c =>
                {
                    c.Item().Text("RESPONSIVA").Bold().FontSize(12);
                    c.Item().Text(folio).Bold().FontSize(11).FontColor(Colors.Blue.Medium);
                });
            });

            col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Medium);
        });
    }

    private void ComposeBody(IContainer container, CustodyFormDetailDto form, string issued)
    {
        container.PaddingTop(12).Column(col =>
        {
            col.Spacing(10);

            col.Item().Text("Resguardo de activos de TI").Bold().FontSize(12);

            col.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(c =>
            {
                c.Spacing(2);
                c.Item().Text(t =>
                {
                    t.Span("Colaborador: ").Bold();
                    t.Span($"{form.EmployeeName} ({form.EmployeeNumber})");
                });
                c.Item().Text(t =>
                {
                    t.Span("Estado: ").Bold();
                    t.Span(form.Status.ToSpanish());
                });
                c.Item().Text(t =>
                {
                    t.Span("Emitida: ").Bold();
                    t.Span(issued);
                });
                c.Item().Text(t =>
                {
                    t.Span("Asignó (admin): ").Bold();
                    t.Span(string.IsNullOrWhiteSpace(form.IssuedByUserName) ? "—" : form.IssuedByUserName);
                });
                if (!string.IsNullOrWhiteSpace(form.Notes))
                {
                    c.Item().Text(t =>
                    {
                        t.Span("Notas: ").Bold();
                        t.Span(form.Notes);
                    });
                }
            });

            col.Item().Text("Activos entregados").Bold().FontSize(11);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(1.2f); // Código
                    cols.RelativeColumn(1.2f); // Serie
                    cols.RelativeColumn(1.2f); // Categoría
                    cols.RelativeColumn(2.2f); // Descripción
                    cols.RelativeColumn(1.6f); // Devolución
                });

                table.Header(h =>
                {
                    HeaderCell(h.Cell(), "Código");
                    HeaderCell(h.Cell(), "Serie");
                    HeaderCell(h.Cell(), "Categoría");
                    HeaderCell(h.Cell(), "Descripción");
                    HeaderCell(h.Cell(), "Devolución");
                });

                foreach (var line in form.Lines)
                {
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                        .PaddingVertical(5).PaddingHorizontal(3).AlignMiddle()
                        .Text(line.AssetCode).FontSize(9);

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                        .PaddingVertical(5).PaddingHorizontal(3)
                        .Element(c => ComposeSerialCell(c, line));

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                        .PaddingVertical(5).PaddingHorizontal(3)
                        .Element(c => ComposeCategoryCell(c, line));

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                        .PaddingVertical(5).PaddingHorizontal(3)
                        .Element(c => ComposeDescriptionCell(c, line));

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1)
                        .PaddingVertical(5).PaddingHorizontal(3)
                        .Element(c => ComposeReturnCell(c, line));
                }
            });

            if (!string.IsNullOrWhiteSpace(_company.CustodyLegend))
            {
                col.Item().PaddingTop(14).Border(0.75f).BorderColor(Colors.Grey.Medium)
                    .Background(Colors.Grey.Lighten4)
                    .Padding(8)
                    .Column(c =>
                    {
                        c.Item().Text("Declaración de resguardo").Bold().FontSize(9);
                        c.Item().PaddingTop(4)
                            .Text(_company.CustodyLegend)
                            .FontSize(7.5f)
                            .LineHeight(1.25f)
                            .Justify();
                    });
            }

            col.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Element(c => ComposeSignatureBox(c, "Firma del colaborador"));
                row.ConstantItem(30);
                row.RelativeItem().Element(c => ComposeSignatureBox(c, "Firma de quien entrega"));
            });
        });
    }

    private static void ComposeSerialCell(IContainer container, CustodyFormLineDto line)
    {
        container.Column(c =>
        {
            c.Spacing(1);
            c.Item().Text(line.SerialNumber ?? "—").FontSize(9);
            c.Item().Text(line.ConditionOnDelivery.ToSpanish()).FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    }

    private static void ComposeCategoryCell(IContainer container, CustodyFormLineDto line)
    {
        container.Column(c =>
        {
            c.Spacing(1);
            c.Item().Text(line.AssetKind.ToSpanish()).Bold().FontSize(8);
            if (!string.IsNullOrWhiteSpace(line.CategoryName))
                c.Item().Text(line.CategoryName).FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    }

    private static void ComposeDescriptionCell(IContainer container, CustodyFormLineDto line)
    {
        container.Column(c =>
        {
            c.Spacing(1);
            var title = string.Join(" ", new[] { line.BrandName, line.ModelName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            c.Item().Text(string.IsNullOrWhiteSpace(title) ? "—" : title).Bold().FontSize(9);
            if (!string.IsNullOrWhiteSpace(line.Specs))
                c.Item().Text(line.Specs).FontSize(8).FontColor(Colors.Grey.Darken2);
        });
    }

    private static void ComposeReturnCell(IContainer container, CustodyFormLineDto line)
    {
        container.Column(c =>
        {
            c.Spacing(1);
            if (line.ReturnedAt is null)
            {
                c.Item().Text("Pendiente").FontSize(8).FontColor(Colors.Orange.Darken2);
                return;
            }

            c.Item().Text(line.ReturnedAt.Value.ToLocalTime().ToString("dd/MM/yyyy")).FontSize(8);
            c.Item().Text(string.IsNullOrWhiteSpace(line.ReturnedByUserName)
                    ? "—"
                    : $"Recibió: {line.ReturnedByUserName}")
                .FontSize(7.5f)
                .FontColor(Colors.Grey.Darken1);
        });
    }

    private static void ComposeSignatureBox(IContainer container, string label)
    {
        container.Column(c =>
        {
            c.Item()
                .Border(1)
                .BorderColor(Colors.Grey.Darken1)
                .Height(70)
                .Padding(6)
                .AlignBottom()
                .AlignCenter()
                .Text(string.Empty);

            c.Item().PaddingTop(6).AlignCenter().Text(label).FontSize(8);
        });
    }

    private static void HeaderCell(IContainer cell, string text) =>
        cell.Background(Colors.Grey.Lighten2).Padding(4).Text(text).Bold().FontSize(8);

    private string? ResolveLogoPath()
    {
        if (string.IsNullOrWhiteSpace(_company.LogoPath))
            return null;

        var full = Path.IsPathRooted(_company.LogoPath)
            ? _company.LogoPath
            : Path.Combine(_env.ContentRootPath, _company.LogoPath);

        return File.Exists(full) ? full : null;
    }
}
