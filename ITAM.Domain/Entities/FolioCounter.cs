namespace ITAM.Domain.Entities;

/// <summary>
/// Contador de folios (RESPONSIVAS, etc.). Incremento en transacción para evitar duplicados.
/// Ejemplo: Prefijo RES + Year 2026 + LastNumber → RES-2026-0001.
/// </summary>
public class FolioCounter
{
    public int Id { get; set; }

    /// <summary>Prefijo del documento (ej. RES).</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Año del folio; null si el formato no usa año.</summary>
    public int? Year { get; set; }

    /// <summary>Último número emitido.</summary>
    public int LastNumber { get; set; }

    /// <summary>Cantidad de dígitos con padding (default 4 → 0001).</summary>
    public int PadLength { get; set; } = 4;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
