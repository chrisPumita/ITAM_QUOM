using ITAM.Domain.Interfaces.Repositories.Assignments;
using ITAM.Domain.Interfaces.Services.Assignments;
using ITAM.Shared.Dtos.Apis;
using ITAM.Shared.Dtos.Assignments;
using ITAM.Shared.Services.Company;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ITAM.Infrastructure.Services.Assignments;

/// <summary>PDF de responsiva con QuestPDF y cabecero desde <see cref="CompanySettings"/>.</summary>
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

    private static void ComposeBody(IContainer container, CustodyFormDetailDto form, string issued)
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
                    t.Span(form.Status.ToString());
                });
                c.Item().Text(t =>
                {
                    t.Span("Emitida: ").Bold();
                    t.Span(issued);
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
                    cols.ConstantColumn(28);
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(1.5f);
                    cols.ConstantColumn(40);
                    cols.RelativeColumn(1.2f);
                    cols.RelativeColumn(2);
                });

                table.Header(h =>
                {
                    HeaderCell(h.Cell(), "#");
                    HeaderCell(h.Cell(), "Código");
                    HeaderCell(h.Cell(), "Serie");
                    HeaderCell(h.Cell(), "Cant.");
                    HeaderCell(h.Cell(), "Condición");
                    HeaderCell(h.Cell(), "Notas entrega");
                });

                var i = 1;
                foreach (var line in form.Lines)
                {
                    table.Cell().Padding(3).Text(i.ToString());
                    table.Cell().Padding(3).Text(line.AssetCode);
                    table.Cell().Padding(3).Text(line.SerialNumber ?? "—");
                    table.Cell().Padding(3).AlignCenter().Text(line.Quantity.ToString());
                    table.Cell().Padding(3).Text(line.ConditionOnDelivery.ToString());
                    table.Cell().Padding(3).Text(line.DeliveryNotes ?? string.Empty);
                    i++;
                }
            });

            col.Item().PaddingTop(24).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().LineHorizontal(1);
                    c.Item().PaddingTop(4).AlignCenter().Text("Firma del colaborador").FontSize(8);
                });
                row.ConstantItem(40);
                row.RelativeItem().Column(c =>
                {
                    c.Item().LineHorizontal(1);
                    c.Item().PaddingTop(4).AlignCenter().Text("Firma de quien entrega").FontSize(8);
                });
            });
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
