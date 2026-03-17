using Microsoft.Data.SqlClient;
using SecureERP.Domain.Modules.Security;
using SecureERP.Infrastructure.Persistence.Db;
using SecureERP.Infrastructure.Persistence.Mapping;
using SecureERP.Infrastructure.Persistence.SessionContext;
using System.Data;

namespace SecureERP.Infrastructure.Security;

public sealed class AuthorizationRepository : IAuthorizationRepository
{
    private const string EvaluateAuthorizationProcedure = "[seguridad].[usp_autorizacion_evaluar]";
    private const string WriteAuthorizationAuditProcedure = "[observabilidad].[usp_auditoria_autorizacion_crear]";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ISqlSessionContextApplier _sessionContextApplier;

    public AuthorizationRepository(
        ISqlConnectionFactory connectionFactory,
        ISqlSessionContextApplier sessionContextApplier)
    {
        _connectionFactory = connectionFactory;
        _sessionContextApplier = sessionContextApplier;
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

    public async Task WriteAuthorizationAuditAsync(
        AuthorizationAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = WriteAuthorizationAuditProcedure;
        command.Parameters.Add(SqlParameterFactory.DateTime2("@fecha_utc", entry.UtcTimestamp));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_tenant", entry.TenantId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_usuario", entry.UserId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_empresa", entry.CompanyId));
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_sesion_usuario", entry.SessionId));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@codigo_permiso", entry.PermissionCode, 150));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@codigo_operacion", entry.OperationCode, 150));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@metodo_http", entry.HttpMethod, 10));
        command.Parameters.Add(SqlParameterFactory.Bit("@permitido", entry.Allowed));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@motivo", entry.Reason, 200));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@codigo_entidad", entry.EntityCode, 128));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_objeto", entry.ObjectId));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@ip_origen", entry.IpAddress, 45));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@agente_usuario", entry.UserAgent, 300));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@solicitud_id", entry.RequestId, 64));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
