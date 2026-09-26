namespace ITAM.Domain.Entities;

public class FolioCounter
{
    public int Id { get; set; }

    public string Prefix { get; set; } = string.Empty;

    public int? Year { get; set; }

    public int LastNumber { get; set; }

    public int PadLength { get; set; } = 4;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
