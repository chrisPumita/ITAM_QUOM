using System.Data;
using System.Text.Json;
using Dapper;
using ITAM.Domain.Interfaces.DataAccess;
using ITAM.Domain.Interfaces.Repositories.Assignments;
using ITAM.Shared.Dtos.Assignments;
using ITAM.Shared.Enums;
using Microsoft.Data.SqlClient;

namespace ITAM.Infrastructure.Repositories.Assignments;

/// <summary>
/// Repository de asignaciones: escrituras con ADO.NET + SP; lecturas con Dapper.
/// </summary>
public sealed class AssetAssignmentRepository : IAssetAssignmentRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ISqlConnectionFactory _connections;

    public AssetAssignmentRepository(ISqlConnectionFactory connections) => _connections = connections;

    public async Task<AssignAssetsResultDto> AssignAsync(
        AssignAssetsDto dto,
        Guid performedByUserId,
        CancellationToken ct = default)
    {
        var linesJson = JsonSerializer.Serialize(dto.Lines.Select(l => new
        {
            assetId = l.AssetId,
            quantity = l.Quantity,
            conditionOnDelivery = l.ConditionOnDelivery.ToString(),
            deliveryNotes = l.DeliveryNotes
        }), JsonOptions);

        await using var connection = (SqlConnection)await _connections.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.sp_AssignAssets";
        cmd.Parameters.Add(new SqlParameter("@EmployeeId", dto.EmployeeId));
        cmd.Parameters.Add(new SqlParameter("@AssignedByUserId", performedByUserId));
        cmd.Parameters.Add(new SqlParameter("@Notes", (object?)dto.Notes ?? DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@LinesJson", linesJson));

        var pCustody = new SqlParameter("@CustodyFormId", SqlDbType.UniqueIdentifier)
        {
            Direction = ParameterDirection.Output
        };
        var pFolio = new SqlParameter("@Folio", SqlDbType.NVarChar, 40)
        {
            Direction = ParameterDirection.Output
        };
        var pCount = new SqlParameter("@AssignedCount", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(pCustody);
        cmd.Parameters.Add(pFolio);
        cmd.Parameters.Add(pCount);

        try
        {
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (SqlException ex) when (ex.Number is >= 50001 and <= 50099)
        {
            throw new AssetAssignmentRepositoryException(ex.Message, MapAssignError(ex.Number), ex);
        }

        return new AssignAssetsResultDto
        {
            CustodyFormId = (Guid)pCustody.Value!,
            Folio = (string)pFolio.Value!,
            AssignedCount = (int)pCount.Value!
        };
    }

    public async Task<ReturnAssetResultDto> ReturnAsync(
        ReturnAssetDto dto,
        Guid performedByUserId,
        CancellationToken ct = default)
    {
        await using var connection = (SqlConnection)await _connections.CreateOpenConnectionAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.sp_ReturnAsset";
        cmd.Parameters.Add(new SqlParameter("@AssetId", dto.AssetId));
        cmd.Parameters.Add(new SqlParameter("@ReturnedByUserId", performedByUserId));
        cmd.Parameters.Add(new SqlParameter("@ReturnCondition", dto.ReturnCondition.ToString()));
        cmd.Parameters.Add(new SqlParameter("@Notes", (object?)dto.Notes ?? DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@ToLocationId", (object?)dto.ToLocationId ?? DBNull.Value));

        var pAssignment = new SqlParameter("@AssignmentId", SqlDbType.UniqueIdentifier)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(pAssignment);

        try
        {
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (SqlException ex) when (ex.Number is >= 50001 and <= 50099)
        {
            throw new AssetAssignmentRepositoryException(ex.Message, MapReturnError(ex.Number), ex);
        }

        return new ReturnAssetResultDto
        {
            AssetId = dto.AssetId,
            AssignmentId = (Guid)pAssignment.Value!
        };
    }

    public async Task<List<AssignmentListDto>> ListAssignmentsAsync(
        Guid? employeeId,
        Guid? assetId,
        bool onlyActive,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                aa.Id,
                aa.AssetId,
                a.AssetCode,
                a.SerialNumber,
                a.Kind            AS AssetKind,
                aa.EmployeeId,
                e.EmployeeNumber,
                e.FullName        AS EmployeeName,
                aa.AssignedAt,
                aa.ReturnedAt,
                aa.ReturnCondition,
                aa.Notes,
                aa.AssignedByUserId,
                ab.DisplayName    AS AssignedByUserName,
                aa.ReturnedByUserId,
                rb.DisplayName    AS ReturnedByUserName
            FROM dbo.AssetAssignments aa
            INNER JOIN dbo.Assets a ON a.Id = aa.AssetId AND a.IsDeleted = 0
            INNER JOIN dbo.Employees e ON e.Id = aa.EmployeeId AND e.IsDeleted = 0
            LEFT JOIN dbo.AspNetUsers ab ON ab.Id = aa.AssignedByUserId
            LEFT JOIN dbo.AspNetUsers rb ON rb.Id = aa.ReturnedByUserId
            WHERE aa.IsDeleted = 0
              AND (@EmployeeId IS NULL OR aa.EmployeeId = @EmployeeId)
              AND (@AssetId IS NULL OR aa.AssetId = @AssetId)
              AND (@OnlyActive = 0 OR aa.ReturnedAt IS NULL)
            ORDER BY aa.AssignedAt DESC
            """;

        await using var connection = await _connections.CreateOpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<AssignmentRow>(
            new CommandDefinition(sql, new
            {
                EmployeeId = employeeId,
                AssetId = assetId,
                OnlyActive = onlyActive ? 1 : 0
            }, cancellationToken: ct));

        return rows.Select(MapAssignment).ToList();
    }

    public async Task<AssignmentListDto?> GetAssignmentAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                aa.Id,
                aa.AssetId,
                a.AssetCode,
                a.SerialNumber,
                a.Kind            AS AssetKind,
                aa.EmployeeId,
                e.EmployeeNumber,
                e.FullName        AS EmployeeName,
                aa.AssignedAt,
                aa.ReturnedAt,
                aa.ReturnCondition,
                aa.Notes,
                aa.AssignedByUserId,
                ab.DisplayName    AS AssignedByUserName,
                aa.ReturnedByUserId,
                rb.DisplayName    AS ReturnedByUserName
            FROM dbo.AssetAssignments aa
            INNER JOIN dbo.Assets a ON a.Id = aa.AssetId AND a.IsDeleted = 0
            INNER JOIN dbo.Employees e ON e.Id = aa.EmployeeId AND e.IsDeleted = 0
            LEFT JOIN dbo.AspNetUsers ab ON ab.Id = aa.AssignedByUserId
            LEFT JOIN dbo.AspNetUsers rb ON rb.Id = aa.ReturnedByUserId
            WHERE aa.IsDeleted = 0 AND aa.Id = @Id
            """;

        await using var connection = await _connections.CreateOpenConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<AssignmentRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return row is null ? null : MapAssignment(row);
    }

    public async Task<List<CustodyFormListDto>> ListCustodyFormsAsync(
        Guid? employeeId,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                cf.Id,
                cf.Folio,
                cf.EmployeeId,
                e.EmployeeNumber,
                e.FullName        AS EmployeeName,
                cf.Status,
                cf.IssuedAt,
                cf.SignedAt,
                cf.Notes,
                cf.IssuedByUserId,
                u.DisplayName     AS IssuedByUserName,
                (SELECT COUNT(1)
                 FROM dbo.CustodyFormLines l
                 WHERE l.CustodyFormId = cf.Id AND l.IsDeleted = 0) AS LineCount
            FROM dbo.CustodyForms cf
            INNER JOIN dbo.Employees e ON e.Id = cf.EmployeeId AND e.IsDeleted = 0
            LEFT JOIN dbo.AspNetUsers u ON u.Id = cf.IssuedByUserId
            WHERE cf.IsDeleted = 0
              AND (@EmployeeId IS NULL OR cf.EmployeeId = @EmployeeId)
            ORDER BY cf.IssuedAt DESC, cf.CreatedAt DESC
            """;

        await using var connection = await _connections.CreateOpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<CustodyHeaderRow>(
            new CommandDefinition(sql, new { EmployeeId = employeeId }, cancellationToken: ct));
        return rows.Select(MapCustodyList).ToList();
    }

    public async Task<CustodyFormDetailDto?> GetCustodyFormAsync(Guid id, CancellationToken ct = default)
    {
        const string headerSql = """
            SELECT
                cf.Id,
                cf.Folio,
                cf.EmployeeId,
                e.EmployeeNumber,
                e.FullName        AS EmployeeName,
                cf.Status,
                cf.IssuedAt,
                cf.SignedAt,
                cf.Notes,
                cf.IssuedByUserId,
                u.DisplayName     AS IssuedByUserName,
                (SELECT COUNT(1)
                 FROM dbo.CustodyFormLines l
                 WHERE l.CustodyFormId = cf.Id AND l.IsDeleted = 0) AS LineCount
            FROM dbo.CustodyForms cf
            INNER JOIN dbo.Employees e ON e.Id = cf.EmployeeId AND e.IsDeleted = 0
            LEFT JOIN dbo.AspNetUsers u ON u.Id = cf.IssuedByUserId
            WHERE cf.IsDeleted = 0 AND cf.Id = @Id
            """;

        const string linesSql = """
            SELECT
                l.Id,
                l.AssetId,
                a.AssetCode,
                a.SerialNumber,
                a.Kind            AS AssetKind,
                l.Quantity,
                l.ConditionOnDelivery,
                l.DeliveryNotes,
                l.ReturnNotes
            FROM dbo.CustodyFormLines l
            INNER JOIN dbo.Assets a ON a.Id = l.AssetId AND a.IsDeleted = 0
            WHERE l.IsDeleted = 0 AND l.CustodyFormId = @Id
            ORDER BY a.AssetCode
            """;

        await using var connection = await _connections.CreateOpenConnectionAsync(ct);
        var header = await connection.QuerySingleOrDefaultAsync<CustodyHeaderRow>(
            new CommandDefinition(headerSql, new { Id = id }, cancellationToken: ct));
        if (header is null)
            return null;

        var lines = await connection.QueryAsync<CustodyLineRow>(
            new CommandDefinition(linesSql, new { Id = id }, cancellationToken: ct));

        var detail = MapCustodyDetail(header);
        detail.Lines = lines.Select(MapCustodyLine).ToList();
        return detail;
    }

    public async Task<List<AssetMovementListDto>> ListMovementsAsync(
        Guid? assetId,
        Guid? employeeId,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                m.Id,
                m.AssetId,
                a.AssetCode,
                m.MovementType,
                m.FromStatus,
                m.ToStatus,
                m.FromLocationId,
                fl.Name           AS FromLocationName,
                m.ToLocationId,
                tl.Name           AS ToLocationName,
                m.EmployeeId,
                e.FullName        AS EmployeeName,
                m.PerformedByUserId,
                u.DisplayName     AS PerformedByUserName,
                m.CustodyFormId,
                cf.Folio          AS CustodyFolio,
                m.Notes,
                m.OccurredAt
            FROM dbo.AssetMovements m
            INNER JOIN dbo.Assets a ON a.Id = m.AssetId AND a.IsDeleted = 0
            LEFT JOIN dbo.Locations fl ON fl.Id = m.FromLocationId
            LEFT JOIN dbo.Locations tl ON tl.Id = m.ToLocationId
            LEFT JOIN dbo.Employees e ON e.Id = m.EmployeeId AND e.IsDeleted = 0
            LEFT JOIN dbo.AspNetUsers u ON u.Id = m.PerformedByUserId
            LEFT JOIN dbo.CustodyForms cf ON cf.Id = m.CustodyFormId AND cf.IsDeleted = 0
            WHERE m.IsDeleted = 0
              AND (@AssetId IS NULL OR m.AssetId = @AssetId)
              AND (@EmployeeId IS NULL OR m.EmployeeId = @EmployeeId)
            ORDER BY m.OccurredAt DESC
            """;

        await using var connection = await _connections.CreateOpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<MovementRow>(
            new CommandDefinition(sql, new { AssetId = assetId, EmployeeId = employeeId }, cancellationToken: ct));
        return rows.Select(MapMovement).ToList();
    }

    private static AssignmentListDto MapAssignment(AssignmentRow r) => new()
    {
        Id = r.Id,
        AssetId = r.AssetId,
        AssetCode = r.AssetCode,
        SerialNumber = r.SerialNumber,
        AssetKind = ParseEnum<AssetKind>(r.AssetKind),
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

    private static CustodyFormListDto MapCustodyList(CustodyHeaderRow r) => new()
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

    private static CustodyFormDetailDto MapCustodyDetail(CustodyHeaderRow r) => new()
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

    private static CustodyFormLineDto MapCustodyLine(CustodyLineRow r) => new()
    {
        Id = r.Id,
        AssetId = r.AssetId,
        AssetCode = r.AssetCode,
        SerialNumber = r.SerialNumber,
        AssetKind = ParseEnum<AssetKind>(r.AssetKind),
        Quantity = r.Quantity,
        ConditionOnDelivery = ParseEnum<AssetCondition>(r.ConditionOnDelivery),
        DeliveryNotes = r.DeliveryNotes,
        ReturnNotes = r.ReturnNotes
    };

    private static AssetMovementListDto MapMovement(MovementRow r) => new()
    {
        Id = r.Id,
        AssetId = r.AssetId,
        AssetCode = r.AssetCode,
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

    private static string MapAssignError(int number) => number switch
    {
        50001 => "Validation",
        50002 => "Validation",
        50003 => "Validation",
        50004 => "Conflict",
        _ => "Validation"
    };

    private static string MapReturnError(int number) => number switch
    {
        50011 => "NotFound",
        50012 => "Validation",
        _ => "Validation"
    };

    private sealed class AssignmentRow
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string AssetKind { get; set; } = string.Empty;
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

    private sealed class CustodyHeaderRow
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

    private sealed class CustodyLineRow
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string AssetKind { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string ConditionOnDelivery { get; set; } = string.Empty;
        public string? DeliveryNotes { get; set; }
        public string? ReturnNotes { get; set; }
    }

    private sealed class MovementRow
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public string AssetCode { get; set; } = string.Empty;
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
}

/// <summary>Error de negocio levantado por el SP (números 500xx).</summary>
public sealed class AssetAssignmentRepositoryException : Exception
{
    public string ErrorCode { get; }

    public AssetAssignmentRepositoryException(string message, string errorCode, Exception? inner = null)
        : base(message, inner)
    {
        ErrorCode = errorCode;
    }
}
