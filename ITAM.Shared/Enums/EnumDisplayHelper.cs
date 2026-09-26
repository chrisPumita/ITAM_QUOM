namespace ITAM.Shared.Enums;

/// <summary>Etiquetas en español para enums de negocio (PDF, UI, reportes).</summary>
public static class EnumDisplayHelper
{
    public static string ToSpanish(this CustodyFormStatus status) => status switch
    {
        CustodyFormStatus.Draft => "Borrador",
        CustodyFormStatus.Issued => "Emitida",
        CustodyFormStatus.Signed => "Firmada",
        CustodyFormStatus.Closed => "Cerrada",
        _ => status.ToString()
    };

    public static string ToSpanish(this AssetCondition condition) => condition switch
    {
        AssetCondition.New => "Nuevo",
        AssetCondition.Used => "Usado",
        _ => condition.ToString()
    };

    public static string ToSpanish(this AssetStatus status) => status switch
    {
        AssetStatus.Available => "Disponible",
        AssetStatus.Assigned => "Asignado",
        AssetStatus.Maintenance => "Mantenimiento",
        AssetStatus.Retired => "Baja",
        _ => status.ToString()
    };

    public static string ToSpanish(this AssetKind kind) => kind switch
    {
        AssetKind.Equipment => "Equipo",
        AssetKind.Accessory => "Accesorio",
        _ => kind.ToString()
    };

    public static string ToSpanish(this OwnershipType ownership) => ownership switch
    {
        OwnershipType.Owned => "Propio",
        OwnershipType.Rented => "Rentado",
        _ => ownership.ToString()
    };

    public static string ToSpanish(this MovementType type) => type switch
    {
        MovementType.Created => "Creado",
        MovementType.Assigned => "Asignado",
        MovementType.Returned => "Devuelto",
        MovementType.StatusChanged => "Cambio de estado",
        MovementType.LocationChanged => "Cambio de ubicación",
        MovementType.Updated => "Actualizado",
        MovementType.CustodyIssued => "Responsiva emitida",
        _ => type.ToString()
    };
}
