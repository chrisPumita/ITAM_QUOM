namespace ITAM.WebApp.Models;

public class HomeDashboardViewModel
{
    public bool IsAdmin { get; set; }
    public int Nuevos { get; set; }
    public int Disponibles { get; set; }
    public int Asignados { get; set; }
    public int Mantenimiento { get; set; }
    public int Baja { get; set; }
    public bool SummaryLoaded { get; set; }
    public string? SummaryError { get; set; }
}
