using System.ComponentModel.DataAnnotations;
using ITAM.Shared.Enums;

namespace ITAM.Shared.Dtos.Assignments;

public class AssignAssetsDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required, MinLength(1)]
    public List<AssignAssetLineDto> Lines { get; set; } = [];
}

public class AssignAssetLineDto
{
    [Required]
    public Guid AssetId { get; set; }

    [Range(1, 999)]
    public int Quantity { get; set; } = 1;

    public AssetCondition ConditionOnDelivery { get; set; } = AssetCondition.New;

    [MaxLength(1000)]
    public string? DeliveryNotes { get; set; }
}

public class AssignAssetsResultDto
{
    public Guid CustodyFormId { get; set; }
    public string Folio { get; set; } = string.Empty;
    public int AssignedCount { get; set; }
}

public class ReturnAssetDto
{
    [Required]
    public Guid AssetId { get; set; }

    public AssetCondition ReturnCondition { get; set; } = AssetCondition.Used;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>Opcional: ubicación a la que regresa el activo.</summary>
    public int? ToLocationId { get; set; }
}

public class ReturnAssetResultDto
{
    public Guid AssetId { get; set; }
    public Guid AssignmentId { get; set; }
}

/// <summary>Fila de asignación (activa o histórica) para listados Dapper.</summary>
public class AssignmentListDto
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public AssetKind AssetKind { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? Specs { get; set; }

    /// <summary>Marca + modelo · specs, listo para reportes.</summary>
    public string Description
    {
        get
        {
            var title = string.Join(" ", new[] { BrandName, ModelName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            return string.Join(" · ", new[] { title, Specs }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }
    }

    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;

    public DateTime AssignedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public AssetCondition? ReturnCondition { get; set; }
    public string? Notes { get; set; }

    public Guid AssignedByUserId { get; set; }
    public string? AssignedByUserName { get; set; }
    public Guid? ReturnedByUserId { get; set; }
    public string? ReturnedByUserName { get; set; }

    public bool IsActive => ReturnedAt is null;
}

public class CustodyFormListDto
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public CustodyFormStatus Status { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? Notes { get; set; }
    public Guid IssuedByUserId { get; set; }
    public string? IssuedByUserName { get; set; }
    public int LineCount { get; set; }
}

public class CustodyFormDetailDto : CustodyFormListDto
{
    public List<CustodyFormLineDto> Lines { get; set; } = [];
}

public class CustodyFormLineDto
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public AssetKind AssetKind { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? Specs { get; set; }
    public int Quantity { get; set; }
    public AssetCondition ConditionOnDelivery { get; set; }
    public string? DeliveryNotes { get; set; }
    public string? ReturnNotes { get; set; }

    /// <summary>Si el activo de esta línea ya fue devuelto: fecha UTC.</summary>
    public DateTime? ReturnedAt { get; set; }

    /// <summary>Usuario Identity que registró la devolución.</summary>
    public string? ReturnedByUserName { get; set; }
}

/// <summary>Historial / auditoría de movimientos de activo (Dapper).</summary>
public class AssetMovementListDto
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public AssetKind AssetKind { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? Specs { get; set; }

    public string Description
    {
        get
        {
            var title = string.Join(" ", new[] { BrandName, ModelName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            return string.Join(" · ", new[] { title, Specs }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }
    }

    public MovementType MovementType { get; set; }
    public AssetStatus? FromStatus { get; set; }
    public AssetStatus? ToStatus { get; set; }
    public int? FromLocationId { get; set; }
    public string? FromLocationName { get; set; }
    public int? ToLocationId { get; set; }
    public string? ToLocationName { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public Guid PerformedByUserId { get; set; }
    public string? PerformedByUserName { get; set; }
    public Guid? CustodyFormId { get; set; }
    public string? CustodyFolio { get; set; }
    public string? Notes { get; set; }
    public DateTime OccurredAt { get; set; }
}
