using Microsoft.Data.SqlClient;
using SecureERP.Domain.Modules.Security;
using SecureERP.Infrastructure.Persistence.Db;
using SecureERP.Infrastructure.Persistence.Mapping;
using System.Data;

namespace SecureERP.Infrastructure.Security;

public sealed class AuthorizationRepository : IAuthorizationRepository
{
    private const string EvaluateAuthorizationProcedure = "[seguridad].[usp_autorizacion_evaluar]";

    private readonly ISqlConnectionFactory _connectionFactory;

    public AuthorizationRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AuthorizationDecision> EvaluateAsync(
        AuthorizationCheck request,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = EvaluateAuthorizationProcedure;
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_usuario", request.UserId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_tenant", request.TenantId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_empresa", request.CompanyId));
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_sesion_usuario", request.SessionId));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@codigo_permiso", request.PermissionCode, 150));
        command.Parameters.Add(SqlParameterFactory.Bit("@requiere_mfa", request.RequiresMfa));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new AuthorizationDecision(false, "AUTHZ_NO_RESPONSE", "DEFAULT_DENY");
        }

        return new AuthorizationDecision(
            reader.GetBoolean(reader.GetOrdinal("autorizado")),
            reader.GetString(reader.GetOrdinal("reason_code")),
            reader.GetString(reader.GetOrdinal("resolution_source")));
    }
}
