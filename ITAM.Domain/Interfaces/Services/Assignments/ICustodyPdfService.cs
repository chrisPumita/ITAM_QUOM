using ITAM.Shared.Dtos.Apis;

namespace ITAM.Domain.Interfaces.Services.Assignments;

public interface ICustodyPdfService
{
    Task<Result<CustodyPdfFile>> GenerateAsync(Guid custodyFormId, CancellationToken ct = default);
}

public sealed class CustodyPdfFile
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    public string ContentType { get; init; } = "application/pdf";
}
