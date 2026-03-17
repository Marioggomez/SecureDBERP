using Microsoft.Data.SqlClient;
using SecureERP.Infrastructure.Persistence.Db;
using SecureERP.Infrastructure.Persistence.SessionContext;
using System.Data;

namespace SecureERP.Infrastructure.Persistence.Commands;

public sealed class StoredProcedureCommandExecutor : IStoredProcedureCommandExecutor
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ISqlSessionContextApplier _sessionContextApplier;

    public StoredProcedureCommandExecutor(
        ISqlConnectionFactory connectionFactory,
        ISqlSessionContextApplier sessionContextApplier)
    {
        _connectionFactory = connectionFactory;
        _sessionContextApplier = sessionContextApplier;
    }

    public async Task<int> ExecuteAsync(
        string schema,
        string procedureName,
        IReadOnlyCollection<SqlParameter> parameters,
        CancellationToken cancellationToken = default)
    {
        string commandName = StoredProcedureName.Build(schema, procedureName);

        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = commandName;
        command.Parameters.AddRange(parameters.ToArray());

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
