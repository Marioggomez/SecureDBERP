using Microsoft.Data.SqlClient;

namespace SecureERP.Infrastructure.Persistence.Commands;

public interface IStoredProcedureCommandExecutor
{
    Task<int> ExecuteAsync(
        string schema,
        string procedureName,
        IReadOnlyCollection<SqlParameter> parameters,
        CancellationToken cancellationToken = default);
}
