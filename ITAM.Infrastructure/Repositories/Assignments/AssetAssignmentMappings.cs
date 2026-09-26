using ITAM.Shared.Dtos.Assignments;
using ITAM.Shared.Enums;

namespace ITAM.Infrastructure.Repositories.Assignments;

internal static class AssetAssignmentMappings
{
    public static AssignmentListDto ToDto(AssignmentRow r) => new()
    {
        Id = r.Id,
        AssetId = r.AssetId,
        AssetCode = r.AssetCode,
        SerialNumber = r.SerialNumber,
        AssetKind = ParseEnum<AssetKind>(r.AssetKind),
        CategoryName = r.CategoryName,
        BrandName = r.BrandName,
        ModelName = r.ModelName,
        Specs = r.Specs,
        EmployeeId = r.EmployeeId,
        EmployeeNumber = r.EmployeeNumber,
        EmployeeName = r.EmployeeName,
        AssignedAt = r.AssignedAt,
        ReturnedAt = r.ReturnedAt,
        ReturnCondition = ParseEnumOrNull<AssetCondition>(r.ReturnCondition),
        Notes = r.Notes,
        AssignedByUserId = r.AssignedByUserId,
        AssignedByUserName = r.AssignedByUserName,
        ReturnedByUserId = r.ReturnedByUserId,
        ReturnedByUserName = r.ReturnedByUserName
    };

    public static CustodyFormListDto ToListDto(CustodyHeaderRow r) => new()
    {
        Id = r.Id,
        Folio = r.Folio,
        EmployeeId = r.EmployeeId,
        EmployeeNumber = r.EmployeeNumber,
        EmployeeName = r.EmployeeName,
        Status = ParseEnum<CustodyFormStatus>(r.Status),
        IssuedAt = r.IssuedAt,
        SignedAt = r.SignedAt,
        Notes = r.Notes,
        IssuedByUserId = r.IssuedByUserId,
        IssuedByUserName = r.IssuedByUserName,
        LineCount = r.LineCount
    };

    public static CustodyFormDetailDto ToDetailDto(CustodyHeaderRow r) => new()
    {
        Id = r.Id,
        Folio = r.Folio,
        EmployeeId = r.EmployeeId,
        EmployeeNumber = r.EmployeeNumber,
        EmployeeName = r.EmployeeName,
        Status = ParseEnum<CustodyFormStatus>(r.Status),
        IssuedAt = r.IssuedAt,
        SignedAt = r.SignedAt,
        Notes = r.Notes,
        IssuedByUserId = r.IssuedByUserId,
        IssuedByUserName = r.IssuedByUserName,
        LineCount = r.LineCount
    };

    public static CustodyFormLineDto ToDto(CustodyLineRow r) => new()
    {
        Id = r.Id,
        AssetId = r.AssetId,
        AssetCode = r.AssetCode,
        SerialNumber = r.SerialNumber,
        AssetKind = ParseEnum<AssetKind>(r.AssetKind),
        CategoryName = r.CategoryName,
        BrandName = r.BrandName,
        ModelName = r.ModelName,
        Specs = r.Specs,
        Quantity = r.Quantity,
        ConditionOnDelivery = ParseEnum<AssetCondition>(r.ConditionOnDelivery),
        DeliveryNotes = r.DeliveryNotes,
        ReturnNotes = r.ReturnNotes,
        ReturnedAt = r.ReturnedAt,
        ReturnedByUserName = r.ReturnedByUserName
    };

    public static AssetMovementListDto ToDto(MovementRow r) => new()
    {
        Id = r.Id,
        AssetId = r.AssetId,
        AssetCode = r.AssetCode,
        SerialNumber = r.SerialNumber,
        AssetKind = ParseEnum<AssetKind>(r.AssetKind),
        CategoryName = r.CategoryName,
        BrandName = r.BrandName,
        ModelName = r.ModelName,
        Specs = r.Specs,
        MovementType = ParseEnum<MovementType>(r.MovementType),
        FromStatus = ParseEnumOrNull<AssetStatus>(r.FromStatus),
        ToStatus = ParseEnumOrNull<AssetStatus>(r.ToStatus),
        FromLocationId = r.FromLocationId,
        FromLocationName = r.FromLocationName,
        ToLocationId = r.ToLocationId,
        ToLocationName = r.ToLocationName,
        EmployeeId = r.EmployeeId,
        EmployeeName = r.EmployeeName,
        PerformedByUserId = r.PerformedByUserId,
        PerformedByUserName = r.PerformedByUserName,
        CustodyFormId = r.CustodyFormId,
        CustodyFolio = r.CustodyFolio,
        Notes = r.Notes,
        OccurredAt = r.OccurredAt
    };

    private static T ParseEnum<T>(string value) where T : struct, Enum =>
        Enum.Parse<T>(value, ignoreCase: true);

    private static T? ParseEnumOrNull<T>(string? value) where T : struct, Enum =>
        string.IsNullOrWhiteSpace(value) ? null : Enum.Parse<T>(value, ignoreCase: true);
}

internal sealed class AssignmentRow
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string AssetKind { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? Specs { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string? ReturnCondition { get; set; }
    public string? Notes { get; set; }
    public Guid AssignedByUserId { get; set; }
    public string? AssignedByUserName { get; set; }
    public Guid? ReturnedByUserId { get; set; }
    public string? ReturnedByUserName { get; set; }
}

internal sealed class CustodyHeaderRow
{
    public Guid Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? IssuedAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? Notes { get; set; }
    public Guid IssuedByUserId { get; set; }
    public string? IssuedByUserName { get; set; }
    public int LineCount { get; set; }
}

internal sealed class CustodyLineRow
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string AssetKind { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? Specs { get; set; }
    public int Quantity { get; set; }
    public string ConditionOnDelivery { get; set; } = string.Empty;
    public string? DeliveryNotes { get; set; }
    public string? ReturnNotes { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string? ReturnedByUserName { get; set; }
}

internal sealed class MovementRow
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string AssetKind { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? Specs { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
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
