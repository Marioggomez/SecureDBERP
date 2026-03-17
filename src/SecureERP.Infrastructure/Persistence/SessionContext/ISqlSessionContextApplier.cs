using Microsoft.Data.SqlClient;

namespace SecureERP.Infrastructure.Persistence.SessionContext;

public interface ISqlSessionContextApplier
{
    Task ApplyAsync(SqlConnection connection, CancellationToken cancellationToken = default);
}
