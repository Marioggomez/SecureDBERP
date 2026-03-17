using Microsoft.Data.SqlClient;
using SecureERP.Application.Abstractions.Context;
using SecureERP.Infrastructure.Persistence.Mapping;
using System.Data;

namespace SecureERP.Infrastructure.Persistence.SessionContext;

public sealed class SqlSessionContextApplier : ISqlSessionContextApplier
{
    private readonly IRequestContextAccessor _requestContextAccessor;

    public SqlSessionContextApplier(IRequestContextAccessor requestContextAccessor)
    {
        _requestContextAccessor = requestContextAccessor;
    }

    public async Task ApplyAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        RequestContext context = _requestContextAccessor.Current;

        await SetValueIfPresentAsync(connection, "correlation_id", context.CorrelationId, cancellationToken);
        await SetValueIfPresentAsync(connection, "tenant_id", context.TenantId, cancellationToken);
        await SetValueIfPresentAsync(connection, "company_id", context.CompanyId, cancellationToken);
        await SetValueIfPresentAsync(connection, "user_id", context.UserId, cancellationToken);
        await SetValueIfPresentAsync(connection, "session_id", context.SessionId, cancellationToken);
    }

    private static async Task SetValueIfPresentAsync(
        SqlConnection connection,
        string key,
        object? value,
        CancellationToken cancellationToken)
    {
        if (value is null)
        {
            return;
        }

        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sys.sp_set_session_context";
        command.Parameters.Add(SqlParameterFactory.NVarChar("@key", key, 128));
        command.Parameters.Add(SqlParameterFactory.Variant("@value", value));
        command.Parameters.Add(SqlParameterFactory.Bit("@read_only", false));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
