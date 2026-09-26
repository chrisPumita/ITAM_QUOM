namespace ITAM.Shared.Dtos.Assets;

public class AssetSummaryDto
{
    public int Nuevos { get; set; }
    public int Disponibles { get; set; }
    public int Asignados { get; set; }
    public int Mantenimiento { get; set; }
    public int Baja { get; set; }
    public int Total { get; set; }
}
