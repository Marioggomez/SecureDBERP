using Microsoft.Data.SqlClient;
using SecureERP.Domain.Modules.Security;
using SecureERP.Infrastructure.Persistence.Db;
using SecureERP.Infrastructure.Persistence.Mapping;
using SecureERP.Infrastructure.Persistence.SessionContext;
using System.Data;

namespace SecureERP.Infrastructure.Security;

public sealed class AuthRepository : IAuthRepository
{
    private const string LoginLookupProcedure = "[seguridad].[usp_auth_obtener_usuario_para_autenticacion]";
    private const string OperableCompaniesProcedure = "[seguridad].[usp_auth_obtener_empresas_usuario_operables]";
    private const string CreateAuthFlowProcedure = "[seguridad].[usp_auth_crear_flujo_autenticacion]";
    private const string GetAuthFlowProcedure = "[seguridad].[usp_auth_obtener_flujo_autenticacion]";
    private const string MarkAuthFlowProcedure = "[seguridad].[usp_auth_marcar_flujo_autenticacion_usado]";
    private const string MarkAuthFlowMfaValidatedProcedure = "[seguridad].[usp_auth_marcar_flujo_autenticacion_mfa_validado]";
    private const string CreateSessionProcedure = "[seguridad].[usp_auth_crear_sesion_usuario]";
    private const string MarkSessionMfaValidatedProcedure = "[seguridad].[usp_auth_sesion_marcar_mfa_validado]";
    private const string CreateMfaChallengeProcedure = "[seguridad].[usp_auth_crear_desafio_mfa]";
    private const string GetMfaChallengeProcedure = "[seguridad].[usp_auth_obtener_desafio_mfa]";
    private const string IncrementMfaChallengeProcedure = "[seguridad].[usp_auth_incrementar_intento_desafio_mfa]";
    private const string MarkMfaChallengeValidatedProcedure = "[seguridad].[usp_auth_marcar_desafio_mfa_validado]";
    private const string ValidateSessionProcedure = "[seguridad].[usp_auth_validate_session]";
    private const string WriteSecurityEventProcedure = "[seguridad].[usp_security_event_write]";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ISqlSessionContextApplier _sessionContextApplier;

    public AuthRepository(
        ISqlConnectionFactory connectionFactory,
        ISqlSessionContextApplier sessionContextApplier)
    {
        _connectionFactory = connectionFactory;
        _sessionContextApplier = sessionContextApplier;
    }

    public async Task<LoginUserCredential?> GetUserForLoginAsync(
        string tenantCode,
        string identifier,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await ApplyTenantLookupContextAsync(connection, tenantCode, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, LoginLookupProcedure);
        command.Parameters.Add(SqlParameterFactory.NVarChar("@tenant_codigo", tenantCode, 50));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@identificador", identifier, 250));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        long userId = reader.GetInt64(reader.GetOrdinal("id_usuario"));
        long tenantId = reader.GetInt64(reader.GetOrdinal("id_tenant"));
        long companyId = reader.IsDBNull(reader.GetOrdinal("id_empresa")) ? 0 : reader.GetInt64(reader.GetOrdinal("id_empresa"));
        string resultTenantCode = reader.GetString(reader.GetOrdinal("tenant_codigo"));
        string loginPrincipal = reader.GetString(reader.GetOrdinal("login_principal"));
        string displayName = reader.GetString(reader.GetOrdinal("nombre_mostrar"));
        string? email = reader.IsDBNull(reader.GetOrdinal("correo_electronico")) ? null : reader.GetString(reader.GetOrdinal("correo_electronico"));
        bool mfaEnabled = reader.GetBoolean(reader.GetOrdinal("mfa_habilitado"));
        bool requiresPasswordChange = reader.GetBoolean(reader.GetOrdinal("requiere_cambio_clave"));
        int userStatusId = reader.GetInt16(reader.GetOrdinal("id_estado_usuario"));
        bool isActiveUser = reader.GetBoolean(reader.GetOrdinal("activo_usuario"));
        byte[] passwordHash = (byte[])reader["hash_clave"];
        byte[] passwordSalt = (byte[])reader["salt_clave"];
        string passwordAlgorithm = reader.GetString(reader.GetOrdinal("algoritmo_clave"));
        int passwordIterations = reader.GetInt32(reader.GetOrdinal("iteraciones_clave"));
        bool isCredentialActive = reader.GetBoolean(reader.GetOrdinal("activo_credencial"));

        return new LoginUserCredential(
            userId,
            tenantId,
            companyId,
            resultTenantCode,
            loginPrincipal,
            displayName,
            email,
            mfaEnabled,
            requiresPasswordChange,
            userStatusId,
            isActiveUser,
            passwordHash,
            passwordSalt,
            passwordAlgorithm,
            passwordIterations,
            isCredentialActive);
    }

    public async Task<IReadOnlyList<OperableCompany>> GetOperableCompaniesAsync(
        long userId,
        long tenantId,
        CancellationToken cancellationToken = default)
    {
        List<OperableCompany> companies = new();

        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);
        await SetSessionContextAsync(connection, "es_admin_tenant", 1, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, OperableCompaniesProcedure);
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_usuario", userId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_tenant", tenantId));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            companies.Add(new OperableCompany(
                reader.GetInt64(reader.GetOrdinal("id_empresa")),
                reader.GetString(reader.GetOrdinal("codigo_empresa")),
                reader.GetString(reader.GetOrdinal("nombre_empresa")),
                reader.GetBoolean(reader.GetOrdinal("es_predeterminada"))));
        }

        return companies;
    }

    public async Task CreateAuthFlowAsync(AuthFlowToCreate authFlow, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, CreateAuthFlowProcedure);
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_flujo_autenticacion", authFlow.AuthFlowId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_usuario", authFlow.UserId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_tenant", authFlow.TenantId));
        command.Parameters.Add(SqlParameterFactory.Bit("@mfa_requerido", authFlow.MfaRequired));
        command.Parameters.Add(SqlParameterFactory.Bit("@mfa_validado", authFlow.MfaValidated));
        command.Parameters.Add(SqlParameterFactory.DateTime2("@expira_en_utc", authFlow.UtcExpiresAt));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@ip_origen", authFlow.IpAddress, 45));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@agente_usuario", authFlow.UserAgent, 300));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@huella_dispositivo", null, 200));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@solicitud_id", authFlow.RequestId, 64));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<AuthFlowSnapshot?> GetAuthFlowAsync(Guid authFlowId, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, GetAuthFlowProcedure);
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_flujo_autenticacion", authFlowId));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AuthFlowSnapshot(
            reader.GetGuid(reader.GetOrdinal("id_flujo_autenticacion")),
            reader.GetInt64(reader.GetOrdinal("id_usuario")),
            reader.GetInt64(reader.GetOrdinal("id_tenant")),
            reader.GetBoolean(reader.GetOrdinal("mfa_requerido")),
            reader.GetBoolean(reader.GetOrdinal("mfa_validado")),
            reader.GetBoolean(reader.GetOrdinal("usado")),
            reader.GetDateTime(reader.GetOrdinal("expira_en_utc")));
    }

    public async Task<bool> MarkAuthFlowAsUsedAsync(
        Guid authFlowId,
        bool mfaValidated,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, MarkAuthFlowProcedure);
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_flujo_autenticacion", authFlowId));
        command.Parameters.Add(SqlParameterFactory.Bit("@mfa_validado", mfaValidated));

        object? scalar = await command.ExecuteScalarAsync(cancellationToken);
        int affected = scalar is null ? 0 : Convert.ToInt32(scalar);
        return affected > 0;
    }

    public async Task CreateSessionAsync(UserSessionToCreate session, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, CreateSessionProcedure);
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_sesion_usuario", session.SessionId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_usuario", session.UserId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_tenant", session.TenantId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_empresa", session.CompanyId));
        command.Parameters.Add(SqlParameterFactory.VarBinary("@token_hash", session.TokenHash, 32));
        command.Parameters.Add(SqlParameterFactory.VarBinary("@refresh_hash", null, 32));
        command.Parameters.Add(SqlParameterFactory.VarChar("@origen_autenticacion", session.AuthenticationSource, 20));
        command.Parameters.Add(SqlParameterFactory.Bit("@mfa_validado", session.MfaValidated));
        command.Parameters.Add(SqlParameterFactory.DateTime2("@creado_utc", session.UtcCreatedAt));
        command.Parameters.Add(SqlParameterFactory.DateTime2("@expira_absoluta_utc", session.UtcExpiresAt));
        command.Parameters.Add(SqlParameterFactory.DateTime2("@ultima_actividad_utc", session.UtcLastActivityAt));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@ip_origen", session.IpAddress, 45));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@agente_usuario", session.UserAgent, 300));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@huella_dispositivo", null, 200));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<SessionValidationSnapshot?> ValidateSessionByTokenHashAsync(
        byte[] tokenHash,
        int idleTimeoutMinutes,
        bool updateLastActivity,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, ValidateSessionProcedure);
        command.Parameters.Add(SqlParameterFactory.VarBinary("@token_hash", tokenHash, 32));
        command.Parameters.Add(SqlParameterFactory.Int("@idle_timeout_minutes", idleTimeoutMinutes));
        command.Parameters.Add(SqlParameterFactory.Bit("@actualizar_actividad", updateLastActivity));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SessionValidationSnapshot(
            reader.GetGuid(reader.GetOrdinal("id_sesion_usuario")),
            reader.GetInt64(reader.GetOrdinal("id_usuario")),
            reader.GetInt64(reader.GetOrdinal("id_tenant")),
            reader.GetInt64(reader.GetOrdinal("id_empresa")),
            reader.GetBoolean(reader.GetOrdinal("mfa_validado")),
            reader.GetDateTime(reader.GetOrdinal("expira_absoluta_utc")),
            reader.GetDateTime(reader.GetOrdinal("ultima_actividad_utc")),
            reader.GetBoolean(reader.GetOrdinal("sesion_valida")));
    }

    public async Task WriteSecurityEventAsync(SecurityEventToCreate securityEvent, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, WriteSecurityEventProcedure);
        command.Parameters.Add(SqlParameterFactory.VarChar("@event_type", securityEvent.EventType, 80));
        command.Parameters.Add(SqlParameterFactory.VarChar("@severity", securityEvent.Severity, 20));
        command.Parameters.Add(SqlParameterFactory.VarChar("@resultado", securityEvent.Result, 30));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@detalle", securityEvent.Detail, 1000));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_tenant", securityEvent.TenantId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_empresa", securityEvent.CompanyId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_usuario", securityEvent.UserId));
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_sesion_usuario", securityEvent.SessionId));
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@auth_flow_id", securityEvent.AuthFlowId));
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@correlation_id", securityEvent.CorrelationId));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@ip_origen", securityEvent.IpAddress, 45));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@agente_usuario", securityEvent.UserAgent, 300));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task CreateMfaChallengeAsync(MfaChallengeToCreate challenge, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, CreateMfaChallengeProcedure);
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_desafio_mfa", challenge.ChallengeId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_usuario", challenge.UserId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_tenant", challenge.TenantId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_empresa", challenge.CompanyId));
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_sesion_usuario", challenge.SessionId));
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_flujo_autenticacion", challenge.AuthFlowId));
        command.Parameters.Add(SqlParameterFactory.SmallInt("@id_proposito_desafio_mfa", (short)challenge.Purpose));
        command.Parameters.Add(SqlParameterFactory.SmallInt("@id_canal_notificacion", (short)challenge.Channel));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@codigo_accion", challenge.ActionCode, 100));
        command.Parameters.Add(SqlParameterFactory.VarBinary("@otp_hash", challenge.OtpHash, 32));
        command.Parameters.Add(SqlParameterFactory.VarBinary("@otp_salt", challenge.OtpSalt, 16));
        command.Parameters.Add(SqlParameterFactory.DateTime2("@expira_en_utc", challenge.UtcExpiresAt));
        command.Parameters.Add(SqlParameterFactory.SmallInt("@max_intentos", challenge.MaxAttempts));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<MfaChallengeSnapshot?> GetMfaChallengeAsync(Guid challengeId, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, GetMfaChallengeProcedure);
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_desafio_mfa", challengeId));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new MfaChallengeSnapshot(
            reader.GetGuid(reader.GetOrdinal("id_desafio_mfa")),
            reader.IsDBNull(reader.GetOrdinal("id_flujo_autenticacion"))
                ? null
                : reader.GetGuid(reader.GetOrdinal("id_flujo_autenticacion")),
            reader.IsDBNull(reader.GetOrdinal("id_sesion_usuario"))
                ? null
                : reader.GetGuid(reader.GetOrdinal("id_sesion_usuario")),
            reader.GetInt64(reader.GetOrdinal("id_usuario")),
            reader.GetInt64(reader.GetOrdinal("id_tenant")),
            reader.IsDBNull(reader.GetOrdinal("id_empresa")) ? null : reader.GetInt64(reader.GetOrdinal("id_empresa")),
            (MfaPurpose)reader.GetInt16(reader.GetOrdinal("id_proposito_desafio_mfa")),
            (byte[])reader["otp_hash"],
            (byte[])reader["otp_salt"],
            reader.GetDateTime(reader.GetOrdinal("expira_en_utc")),
            reader.GetBoolean(reader.GetOrdinal("usado")),
            reader.GetInt16(reader.GetOrdinal("intentos")),
            reader.GetInt16(reader.GetOrdinal("max_intentos")));
    }

    public async Task<bool> IncrementMfaChallengeAttemptAsync(Guid challengeId, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, IncrementMfaChallengeProcedure);
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_desafio_mfa", challengeId));
        object? scalar = await command.ExecuteScalarAsync(cancellationToken);
        int affected = scalar is null ? 0 : Convert.ToInt32(scalar);
        return affected > 0;
    }

    public async Task<bool> MarkMfaChallengeValidatedAsync(Guid challengeId, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, MarkMfaChallengeValidatedProcedure);
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_desafio_mfa", challengeId));
        object? scalar = await command.ExecuteScalarAsync(cancellationToken);
        int affected = scalar is null ? 0 : Convert.ToInt32(scalar);
        return affected > 0;
    }

    public async Task<bool> MarkAuthFlowMfaValidatedAsync(Guid authFlowId, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, MarkAuthFlowMfaValidatedProcedure);
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_flujo_autenticacion", authFlowId));
        object? scalar = await command.ExecuteScalarAsync(cancellationToken);
        int affected = scalar is null ? 0 : Convert.ToInt32(scalar);
        return affected > 0;
    }

    public async Task<bool> MarkSessionMfaValidatedAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, MarkSessionMfaValidatedProcedure);
        command.Parameters.Add(SqlParameterFactory.UniqueIdentifier("@id_sesion_usuario", sessionId));
        object? scalar = await command.ExecuteScalarAsync(cancellationToken);
        int affected = scalar is null ? 0 : Convert.ToInt32(scalar);
        return affected > 0;
    }

    private static SqlCommand CreateStoredProcedureCommand(SqlConnection connection, string procedure)
    {
        SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = procedure;
        return command;
    }

    private static async Task ApplyTenantLookupContextAsync(
        SqlConnection connection,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        await using SqlCommand tenantCommand = connection.CreateCommand();
        tenantCommand.CommandType = CommandType.Text;
        tenantCommand.CommandText = "SELECT TOP (1) id_tenant FROM plataforma.tenant WHERE codigo = @codigo;";
        tenantCommand.Parameters.Add(SqlParameterFactory.NVarChar("@codigo", tenantCode, 100));
        object? tenantValue = await tenantCommand.ExecuteScalarAsync(cancellationToken);
        if (tenantValue is null || tenantValue == DBNull.Value)
        {
            return;
        }

        long tenantId = Convert.ToInt64(tenantValue);
        await SetSessionContextAsync(connection, "id_tenant", tenantId, cancellationToken);
        await SetSessionContextAsync(connection, "es_admin_tenant", 1, cancellationToken);
    }

    private static async Task SetSessionContextAsync(
        SqlConnection connection,
        string key,
        object value,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sys.sp_set_session_context";
        command.Parameters.Add(SqlParameterFactory.NVarChar("@key", key, 128));
        command.Parameters.Add(SqlParameterFactory.Variant("@value", value));
        command.Parameters.Add(SqlParameterFactory.Bit("@read_only", false));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
