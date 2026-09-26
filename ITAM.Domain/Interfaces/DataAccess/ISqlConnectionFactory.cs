using System.Data.Common;

namespace ITAM.Domain.Interfaces.DataAccess;

/// <summary>Abre conexiones SQL Server para ADO.NET / Dapper (no EF).</summary>
public interface ISqlConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}
