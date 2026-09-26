using ITAM.Domain.Entities.Assets;
using ITAM.Domain.Enums;

namespace ITAM.Domain.Entities.Custody;

/// <summary>
/// Renglón de responsiva: un activo (equipo o accesorio) con cantidad y condición de entrega.
/// </summary>
public class CustodyFormLine : BaseEntity
{
    public Guid CustodyFormId { get; set; }
    public CustodyForm CustodyForm { get; set; } = null!;

    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    /// <summary>Cantidad (1 para equipo; N permitido en accesorios según reglas de servicio).</summary>
    public int Quantity { get; set; } = 1;

    public AssetCondition ConditionOnDelivery { get; set; } = AssetCondition.New;
    public string? DeliveryNotes { get; set; }
    public string? ReturnNotes { get; set; }
}
