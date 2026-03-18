using Microsoft.Data.SqlClient;
using SecureERP.Domain.Modules.Security;
using SecureERP.Infrastructure.Persistence.Db;
using SecureERP.Infrastructure.Persistence.Mapping;
using System.Data;

namespace SecureERP.Infrastructure.Security;

public sealed class OperationalSecurityRepository : IOperationalSecurityRepository
{
    private const string RateLimitProcedure = "[seguridad].[usp_security_rate_limit_evaluar]";
    private const string IpPolicyProcedure = "[seguridad].[usp_security_ip_policy_evaluar]";
    private const string LoginLockoutProcedure = "[seguridad].[usp_security_login_lockout_control]";

    private readonly ISqlConnectionFactory _connectionFactory;

    public OperationalSecurityRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RateLimitDecision> EvaluateRateLimitAsync(
        string actionCode,
        string scope,
        string key,
        long? tenantId,
        long? companyId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = RateLimitProcedure;
        command.Parameters.Add(SqlParameterFactory.NVarChar("@codigo_accion", actionCode, 100));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@ambito", scope, 30));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@llave", key, 300));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_tenant", tenantId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_empresa", companyId));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new RateLimitDecision(true, 0, 0, 0);
        }

        return new RateLimitDecision(
            reader.GetBoolean(reader.GetOrdinal("permitido")),
            reader.GetInt32(reader.GetOrdinal("conteo")),
            reader.GetInt32(reader.GetOrdinal("max_intentos")),
            reader.GetInt32(reader.GetOrdinal("retry_after_seconds")));
    }

    public async Task<IpPolicyDecision> EvaluateIpPolicyAsync(
        long? tenantId,
        long? companyId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = IpPolicyProcedure;
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_tenant", tenantId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_empresa", companyId));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@ip_origen", ipAddress, 45));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new IpPolicyDecision(true, "IP_POLICY_OK");
        }

        return new IpPolicyDecision(
            reader.GetBoolean(reader.GetOrdinal("permitido")),
            reader.GetString(reader.GetOrdinal("reason_code")));
    }

    public async Task<LoginLockoutDecision> ControlLoginLockoutAsync(
        string login,
        string ipAddress,
        string mode,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = LoginLockoutProcedure;
        command.Parameters.Add(SqlParameterFactory.VarChar("@login_usuario", login, 240));
        command.Parameters.Add(SqlParameterFactory.VarChar("@ip", ipAddress, 45));
        command.Parameters.Add(SqlParameterFactory.VarChar("@modo", mode, 20));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new LoginLockoutDecision(false, null, 0);
        }

        return new LoginLockoutDecision(
            reader.GetBoolean(reader.GetOrdinal("bloqueado")),
            reader.IsDBNull(reader.GetOrdinal("bloqueado_hasta")) ? null : reader.GetDateTime(reader.GetOrdinal("bloqueado_hasta")),
            reader.GetInt32(reader.GetOrdinal("intentos")));
    }
}
