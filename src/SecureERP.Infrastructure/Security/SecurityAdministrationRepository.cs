using Microsoft.Data.SqlClient;
using SecureERP.Domain.Modules.Security;
using SecureERP.Infrastructure.Persistence.Db;
using SecureERP.Infrastructure.Persistence.Mapping;
using SecureERP.Infrastructure.Persistence.SessionContext;
using System.Data;

namespace SecureERP.Infrastructure.Security;

public sealed class SecurityAdministrationRepository : ISecurityAdministrationRepository
{
    private const string ListUsersProcedure = "[seguridad].[usp_security_usuario_listar]";
    private const string GetUserByIdProcedure = "[seguridad].[usp_security_usuario_obtener]";
    private const string ListSecurityEventsProcedure = "[seguridad].[usp_security_event_listar]";
    private const string RevokeSessionProcedure = "[seguridad].[usp_auth_revocar_sesion_usuario]";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ISqlSessionContextApplier _sessionContextApplier;

    public SecurityAdministrationRepository(
        ISqlConnectionFactory connectionFactory,
        ISqlSessionContextApplier sessionContextApplier)
    {
        _connectionFactory = connectionFactory;
        _sessionContextApplier = sessionContextApplier;
    }

    public async Task<IReadOnlyList<SecurityUserSnapshot>> ListUsersAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        List<SecurityUserSnapshot> rows = [];

        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, ListUsersProcedure);
        command.Parameters.Add(SqlParameterFactory.NVarChar("@buscar", search, 200));
        command.Parameters.Add(SqlParameterFactory.Bit("@solo_activos", activeOnly));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(MapUser(reader));
        }

        return rows;
    }

    public async Task<SecurityUserSnapshot?> GetUserByIdAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, GetUserByIdProcedure);
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_usuario", userId));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapUser(reader);
    }

    public async Task<IReadOnlyList<SecurityEventSnapshot>> ListSecurityEventsAsync(
        int top,
        string? eventType,
        string? severity,
        string? result,
        CancellationToken cancellationToken = default)
    {
        List<SecurityEventSnapshot> rows = [];

        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, ListSecurityEventsProcedure);
        command.Parameters.Add(SqlParameterFactory.Int("@top", top));
        command.Parameters.Add(SqlParameterFactory.VarChar("@event_type", eventType, 80));
        command.Parameters.Add(SqlParameterFactory.VarChar("@severity", severity, 20));
        command.Parameters.Add(SqlParameterFactory.VarChar("@resultado", result, 30));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SecurityEventSnapshot(
                reader.GetInt64(reader.GetOrdinal("id_security_event_audit")),
                reader.GetDateTime(reader.GetOrdinal("creado_utc")),
                reader.GetString(reader.GetOrdinal("event_type")),
                reader.GetString(reader.GetOrdinal("severity")),
                reader.GetString(reader.GetOrdinal("resultado")),
                reader.IsDBNull(reader.GetOrdinal("detalle")) ? null : reader.GetString(reader.GetOrdinal("detalle")),
                reader.IsDBNull(reader.GetOrdinal("id_tenant")) ? null : reader.GetInt64(reader.GetOrdinal("id_tenant")),
                reader.IsDBNull(reader.GetOrdinal("id_empresa")) ? null : reader.GetInt64(reader.GetOrdinal("id_empresa")),
                reader.IsDBNull(reader.GetOrdinal("id_usuario")) ? null : reader.GetInt64(reader.GetOrdinal("id_usuario")),
                reader.IsDBNull(reader.GetOrdinal("id_sesion_usuario")) ? null : reader.GetGuid(reader.GetOrdinal("id_sesion_usuario")),
                reader.IsDBNull(reader.GetOrdinal("auth_flow_id")) ? null : reader.GetGuid(reader.GetOrdinal("auth_flow_id")),
                reader.IsDBNull(reader.GetOrdinal("correlation_id")) ? null : reader.GetGuid(reader.GetOrdinal("correlation_id")),
                reader.IsDBNull(reader.GetOrdinal("ip_origen")) ? null : reader.GetString(reader.GetOrdinal("ip_origen")),
                reader.IsDBNull(reader.GetOrdinal("agente_usuario")) ? null : reader.GetString(reader.GetOrdinal("agente_usuario"))));
        }

        return rows;
    }

    public async Task<SessionRevocationResult> RevokeSessionAsync(
        Guid sessionId,
        long revokedByUserId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, RevokeSessionProcedure);
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_sesion_usuario", sessionId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@revocado_por", revokedByUserId));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@motivo", reason, 500));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new SessionRevocationResult(false, "AUTH_SESSION_REVOKE_FAILED", "Session revoke produced no response.", null);
        }

        return new SessionRevocationResult(
            reader.GetBoolean(reader.GetOrdinal("ok")),
            reader.IsDBNull(reader.GetOrdinal("error_code")) ? null : reader.GetString(reader.GetOrdinal("error_code")),
            reader.IsDBNull(reader.GetOrdinal("error_message")) ? null : reader.GetString(reader.GetOrdinal("error_message")),
            reader.IsDBNull(reader.GetOrdinal("target_user_id")) ? null : reader.GetInt64(reader.GetOrdinal("target_user_id")));
    }

    private static SqlCommand CreateStoredProcedureCommand(SqlConnection connection, string procedure)
    {
        SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = procedure;
        return command;
    }

    private static SecurityUserSnapshot MapUser(SqlDataReader reader)
    {
        return new SecurityUserSnapshot(
            reader.GetInt64(reader.GetOrdinal("id_usuario")),
            reader.GetString(reader.GetOrdinal("codigo")),
            reader.GetString(reader.GetOrdinal("login_principal")),
            reader.GetString(reader.GetOrdinal("nombre_mostrar")),
            reader.IsDBNull(reader.GetOrdinal("correo_electronico")) ? null : reader.GetString(reader.GetOrdinal("correo_electronico")),
            reader.GetBoolean(reader.GetOrdinal("mfa_habilitado")),
            reader.GetBoolean(reader.GetOrdinal("requiere_cambio_clave")),
            reader.GetBoolean(reader.GetOrdinal("activo")),
            reader.GetBoolean(reader.GetOrdinal("es_administrador_tenant")),
            reader.IsDBNull(reader.GetOrdinal("id_empresa")) ? null : reader.GetInt64(reader.GetOrdinal("id_empresa")),
            reader.GetBoolean(reader.GetOrdinal("es_empresa_predeterminada")),
            reader.GetBoolean(reader.GetOrdinal("puede_operar")),
            reader.IsDBNull(reader.GetOrdinal("fecha_inicio_utc")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_inicio_utc")),
            reader.IsDBNull(reader.GetOrdinal("fecha_fin_utc")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_fin_utc")),
            reader.IsDBNull(reader.GetOrdinal("bloqueado_hasta_utc")) ? null : reader.GetDateTime(reader.GetOrdinal("bloqueado_hasta_utc")),
            reader.IsDBNull(reader.GetOrdinal("ultimo_acceso_utc")) ? null : reader.GetDateTime(reader.GetOrdinal("ultimo_acceso_utc")));
    }
}
