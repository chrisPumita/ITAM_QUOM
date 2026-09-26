namespace ITAM.Domain.Enums;

/// <summary>Propiedad patrimonial del activo (sin comodato — solo lo del requerimiento).</summary>
public enum OwnershipType
{
    Owned = 1,
    Rented = 2
}

/// <summary>Estado del ciclo de vida del activo.</summary>
public enum AssetStatus
{
    Available = 1,
    Assigned = 2,
    Maintenance = 3,
    Retired = 4
}

/// <summary>Equipo serializado vs accesorio de inventario.</summary>
public enum AssetKind
{
    Equipment = 1,
    Accessory = 2
}

/// <summary>Tipos de movimiento para historial / trazabilidad.</summary>
public enum MovementType
{
    Created = 1,
    Assigned = 2,
    Returned = 3,
    StatusChanged = 4,
    LocationChanged = 5,
    Updated = 6,
    CustodyIssued = 7
}

/// <summary>Estado de la responsiva (CustodyForm).</summary>
public enum CustodyFormStatus
{
    Draft = 1,
    Issued = 2,
    Signed = 3,
    Closed = 4
}

/// <summary>Condición física al entregar / devolver.</summary>
public enum AssetCondition
{
    New = 1,
    Used = 2
}
