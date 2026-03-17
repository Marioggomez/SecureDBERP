using Microsoft.Data.SqlClient;
using SecureERP.Infrastructure.Persistence.Db;
using SecureERP.Infrastructure.Persistence.SessionContext;
using System.Data;

namespace SecureERP.Infrastructure.Persistence.Queries;

public sealed class StoredProcedureQueryExecutor : IStoredProcedureQueryExecutor
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ISqlSessionContextApplier _sessionContextApplier;

    public StoredProcedureQueryExecutor(
        ISqlConnectionFactory connectionFactory,
        ISqlSessionContextApplier sessionContextApplier)
    {
        _connectionFactory = connectionFactory;
        _sessionContextApplier = sessionContextApplier;
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string schema,
        string procedureName,
        IReadOnlyCollection<SqlParameter> parameters,
        Func<SqlDataReader, T> map,
        CancellationToken cancellationToken = default)
    {
        if (map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        string commandName = StoredProcedureName.Build(schema, procedureName);
        List<T> results = new();

        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = commandName;
        command.Parameters.AddRange(parameters.ToArray());

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(map(reader));
        }

        return results;
    }
}
