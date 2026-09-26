using System.Data.Common;

namespace ITAM.Domain.Interfaces.DataAccess;

public interface ISqlConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken ct = default);
}
