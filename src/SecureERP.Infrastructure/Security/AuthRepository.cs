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
    private const string CreateSessionProcedure = "[seguridad].[usp_auth_crear_sesion_usuario]";

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

        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = LoginLookupProcedure;
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
        int userStatusId = reader.GetInt32(reader.GetOrdinal("id_estado_usuario"));
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

    public async Task CreateSessionAsync(UserSessionToCreate session, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = CreateSessionProcedure;
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
}
