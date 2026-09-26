namespace ITAM.Domain.Interfaces.Services;

/// <summary>Archivo generado para descarga (Excel, etc.).</summary>
public sealed class ExportFile
{
    public required byte[] Content { get; init; }
    public required string FileName { get; init; }
    public string ContentType { get; init; } =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
