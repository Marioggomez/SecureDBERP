using Microsoft.Data.SqlClient;

namespace SecureERP.Infrastructure.Persistence.Queries;

public interface IStoredProcedureQueryExecutor
{
    Task<IReadOnlyList<T>> QueryAsync<T>(
        string schema,
        string procedureName,
        IReadOnlyCollection<SqlParameter> parameters,
        Func<SqlDataReader, T> map,
        CancellationToken cancellationToken = default);
}
