using ITAM.Shared.Dtos.Apis;

namespace ITAM.Domain.Interfaces.Services.Assignments;

/// <summary>Genera el PDF de responsiva (CustodyForm).</summary>
public interface ICustodyPdfService
{
    /// <summary>PDF con cabecero de empresa (appsettings) y renglones de la responsiva.</summary>
    Task<Result<CustodyPdfFile>> GenerateAsync(Guid custodyFormId, CancellationToken ct = default);
}

public sealed class CustodyPdfFile
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    public string ContentType { get; init; } = "application/pdf";
}
