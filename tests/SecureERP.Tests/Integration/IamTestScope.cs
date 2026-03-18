using Microsoft.Data.SqlClient;
using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Security.Commands;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Application.Modules.Security.Queries;
using SecureERP.Application.Modules.Security.Services;
using SecureERP.Domain.Modules.Security;
using SecureERP.Infrastructure.Persistence.Db;
using SecureERP.Infrastructure.Persistence.SessionContext;
using SecureERP.Infrastructure.Security;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SecureERP.Tests.Integration;

internal sealed class IamTestScope : IAsyncDisposable
{
    public const string DeterministicOtp = "123456";

    private readonly Guid _suffix = Guid.NewGuid();
    private const string PasswordAlgorithm = "PBKDF2_HMAC_SHA512";
    private const int PasswordIterations = 210000;

    public string ConnectionString { get; }
    public long TenantId { get; private set; }
    public long CompanyId { get; private set; }
    public long UserId { get; private set; }
    public int PermissionId { get; private set; }
    public short AllowEffectId { get; private set; }
    public short DenyEffectId { get; private set; }
    public short ScopeCompanyId { get; private set; }
    public string TenantCode { get; private set; } = string.Empty;
    public string Login { get; private set; } = string.Empty;
    public string Password { get; } = $"P@ss!{Guid.NewGuid():N}";
    public string PermissionCode { get; private set; } = string.Empty;

    public RequestContextAccessor ContextAccessor { get; } = new();
    public ISqlConnectionFactory ConnectionFactory { get; }
    public ISqlSessionContextApplier SessionContextApplier { get; }
    public AuthRepository AuthRepository { get; }

    private IamTestScope(string connectionString)
    {
        ConnectionString = connectionString;
        ConnectionFactory = new SqlServerConnectionFactory(connectionString);
        SessionContextApplier = new SqlSessionContextApplier(ContextAccessor);
        AuthRepository = new AuthRepository(ConnectionFactory, SessionContextApplier);
    }

    public static async Task<IamTestScope> CreateAsync()
    {
        string connectionString = ResolveConnectionString()
            ?? throw new InvalidOperationException("Connection string not configured.");

        IamTestScope scope = new(connectionString);
        await scope.SeedCatalogsAsync();
        await scope.SeedPrincipalAsync();
        return scope;
    }

    public LoginHandler CreateLoginHandler() => new(AuthRepository, new PasswordHasher(), ContextAccessor);
    public RequestMfaChallengeHandler CreateRequestMfaChallengeHandler() => new(AuthRepository, new DeterministicMfaCodeService(), ContextAccessor);
    public VerifyMfaChallengeHandler CreateVerifyMfaChallengeHandler() => new(AuthRepository, new DeterministicMfaCodeService(), ContextAccessor);
    public SelectCompanyHandler CreateSelectCompanyHandler() => new(AuthRepository, new TokenGenerator(), ContextAccessor);
    public ValidateSessionHandler CreateValidateSessionHandler() => new(AuthRepository, new TokenGenerator());
    public AuthorizationEvaluator CreateAuthorizationEvaluator() => new(new AuthorizationRepository(ConnectionFactory, SessionContextApplier), ContextAccessor);

    public async Task<(SelectCompanyResponse Select, ValidateSessionResult Session)> LoginAndSelectCompanyAsync(bool withMfa)
    {
        LoginResponse login = await CreateLoginHandler().HandleAsync(new LoginRequest(TenantCode, Login, Password, "127.0.0.1", "SecureERP.Tests"));
        if (withMfa)
        {
            RequestMfaChallengeResponse challenge = await CreateRequestMfaChallengeHandler().HandleAsync(new RequestMfaChallengeRequest(
                login.AuthFlowId, MfaPurpose.Login, MfaChannel.Totp, "LOGIN"));
            VerifyMfaChallengeResponse verify = await CreateVerifyMfaChallengeHandler().HandleAsync(
                new VerifyMfaChallengeRequest(challenge.ChallengeId!.Value, DeterministicOtp));
            if (!verify.IsVerified) throw new InvalidOperationException("MFA verify failed in setup.");
        }

        SelectCompanyResponse select = await CreateSelectCompanyHandler().HandleAsync(
            new SelectCompanyRequest(login.AuthFlowId!.Value, CompanyId, "127.0.0.1", "SecureERP.Tests"));
        if (!select.IsSuccess || string.IsNullOrWhiteSpace(select.AccessToken))
        {
            throw new InvalidOperationException($"SelectCompany failed: {select.ErrorCode}:{select.ErrorMessage}");
        }

        ValidateSessionResult session = await CreateValidateSessionHandler().HandleAsync(
            new ValidateSessionRequest(select.AccessToken!, 30, true));
        if (!session.IsValid)
        {
            throw new InvalidOperationException($"ValidateSession failed: {session.ErrorCode}:{session.ErrorMessage}");
        }

        return (select, session);
    }

    public async Task ApplyTenantCompanyContextAsync(SqlConnection connection)
    {
        await SetSessionContextAsync(connection, "id_tenant", TenantId);
        await SetSessionContextAsync(connection, "id_empresa", CompanyId);
        await SetSessionContextAsync(connection, "id_usuario", UserId);
        await SetSessionContextAsync(connection, "es_admin_tenant", 1);
    }

    public static string? ResolveConnectionString()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable("SECUREERP_SQL_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(fromEnvironment)) return fromEnvironment;
        string root = Directory.GetCurrentDirectory();
        string config = Path.Combine(root, "database.config.example.json");
        if (!File.Exists(config))
        {
            DirectoryInfo? current = new(root);
            while (current is not null)
            {
                string candidate = Path.Combine(current.FullName, "database.config.example.json");
                if (File.Exists(candidate)) { config = candidate; break; }
                current = current.Parent;
            }
        }

        if (!File.Exists(config)) return null;
        using JsonDocument json = JsonDocument.Parse(File.ReadAllText(config));
        return json.RootElement.TryGetProperty("connectionString", out JsonElement value) ? value.GetString() : null;
    }

    public async ValueTask DisposeAsync()
    {
        await using SqlConnection connection = new(ConnectionString);
        await connection.OpenAsync();
        await ApplyTenantCompanyContextAsync(connection);

        await Exec(connection, "DELETE FROM observabilidad.auditoria_autorizacion WHERE id_tenant=@t AND id_usuario=@u;", ("@t", SqlDbType.BigInt, TenantId), ("@u", SqlDbType.BigInt, UserId));
        await Exec(connection, "DELETE FROM seguridad.security_event_audit WHERE id_tenant=@t AND id_usuario=@u;", ("@t", SqlDbType.BigInt, TenantId), ("@u", SqlDbType.BigInt, UserId));
        await Exec(connection, "DELETE FROM cumplimiento.accion_instancia_aprobacion WHERE id_tenant=@t;", ("@t", SqlDbType.BigInt, TenantId));
        await Exec(connection, "DELETE FROM cumplimiento.instancia_aprobacion WHERE id_tenant=@t;", ("@t", SqlDbType.BigInt, TenantId));
        await Exec(connection, "DELETE FROM cumplimiento.perfil_aprobacion WHERE id_tenant=@t;", ("@t", SqlDbType.BigInt, TenantId));
        await Exec(connection, "DELETE FROM seguridad.usuario_scope_unidad WHERE id_tenant=@t AND id_usuario=@u;", ("@t", SqlDbType.BigInt, TenantId), ("@u", SqlDbType.BigInt, UserId));
        await Exec(connection, "DELETE FROM seguridad.desafio_mfa WHERE id_tenant=@t AND id_usuario=@u;", ("@t", SqlDbType.BigInt, TenantId), ("@u", SqlDbType.BigInt, UserId));
        await Exec(connection, "DELETE FROM seguridad.sesion_usuario WHERE id_tenant=@t AND id_usuario=@u;", ("@t", SqlDbType.BigInt, TenantId), ("@u", SqlDbType.BigInt, UserId));
        await Exec(connection, "DELETE FROM seguridad.flujo_autenticacion WHERE id_tenant=@t AND id_usuario=@u;", ("@t", SqlDbType.BigInt, TenantId), ("@u", SqlDbType.BigInt, UserId));
        await Exec(connection, "DELETE FROM seguridad.excepcion_permiso_usuario WHERE id_tenant=@t AND id_usuario=@u;", ("@t", SqlDbType.BigInt, TenantId), ("@u", SqlDbType.BigInt, UserId));
        await Exec(connection, "DELETE FROM seguridad.permiso WHERE id_permiso=@p;", ("@p", SqlDbType.Int, PermissionId));
        await Exec(connection, "DELETE FROM organizacion.unidad_organizativa WHERE id_tenant=@t;", ("@t", SqlDbType.BigInt, TenantId));
        await Exec(connection, "DELETE FROM seguridad.credencial_local_usuario WHERE id_usuario=@u;", ("@u", SqlDbType.BigInt, UserId));
        await Exec(connection, "DELETE FROM seguridad.usuario_empresa WHERE id_tenant=@t AND id_usuario=@u;", ("@t", SqlDbType.BigInt, TenantId), ("@u", SqlDbType.BigInt, UserId));
        await Exec(connection, "DELETE FROM seguridad.usuario_tenant WHERE id_tenant=@t AND id_usuario=@u;", ("@t", SqlDbType.BigInt, TenantId), ("@u", SqlDbType.BigInt, UserId));
        await Exec(connection, "DELETE FROM seguridad.usuario WHERE id_usuario=@u;", ("@u", SqlDbType.BigInt, UserId));
        await Exec(connection, "DELETE FROM organizacion.empresa WHERE id_tenant=@t;", ("@t", SqlDbType.BigInt, TenantId));
        await Exec(connection, "DELETE FROM plataforma.tenant WHERE id_tenant=@t;", ("@t", SqlDbType.BigInt, TenantId));
    }

    private async Task SeedCatalogsAsync()
    {
        await using SqlConnection connection = new(ConnectionString);
        await connection.OpenAsync();

        await Exec(connection, "IF NOT EXISTS (SELECT 1 FROM catalogo.estado_usuario WHERE id_estado_usuario=1) BEGIN SET IDENTITY_INSERT catalogo.estado_usuario ON; INSERT INTO catalogo.estado_usuario(id_estado_usuario,codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES (1,'ACTIVO',N'Activo',N'Activo',1,1,SYSUTCDATETIME(),NULL); SET IDENTITY_INSERT catalogo.estado_usuario OFF; END;");
        await Exec(connection, "IF NOT EXISTS (SELECT 1 FROM catalogo.estado_empresa WHERE id_estado_empresa=1) BEGIN SET IDENTITY_INSERT catalogo.estado_empresa ON; INSERT INTO catalogo.estado_empresa(id_estado_empresa,codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES (1,'ACTIVA',N'Activa',N'Activa',1,1,SYSUTCDATETIME(),NULL); SET IDENTITY_INSERT catalogo.estado_empresa OFF; END;");
        await Exec(connection, "IF NOT EXISTS (SELECT 1 FROM catalogo.tipo_empresa WHERE id_tipo_empresa=1) BEGIN SET IDENTITY_INSERT catalogo.tipo_empresa ON; INSERT INTO catalogo.tipo_empresa(id_tipo_empresa,codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES (1,'GENERAL',N'General',N'General',1,1,SYSUTCDATETIME(),NULL); SET IDENTITY_INSERT catalogo.tipo_empresa OFF; END;");
        await Exec(connection, "IF NOT EXISTS (SELECT 1 FROM catalogo.efecto_permiso WHERE codigo='ALLOW') BEGIN SET IDENTITY_INSERT catalogo.efecto_permiso ON; INSERT INTO catalogo.efecto_permiso(id_efecto_permiso,codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES (1,'ALLOW',N'Allow',N'Allow',1,1,SYSUTCDATETIME(),NULL); SET IDENTITY_INSERT catalogo.efecto_permiso OFF; END;");
        await Exec(connection, "IF NOT EXISTS (SELECT 1 FROM catalogo.efecto_permiso WHERE codigo='DENY') INSERT INTO catalogo.efecto_permiso(codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES ('DENY',N'Deny',N'Deny',2,1,SYSUTCDATETIME(),NULL);");
        await Exec(connection, "IF NOT EXISTS (SELECT 1 FROM catalogo.alcance_asignacion WHERE codigo='EMPRESA') BEGIN SET IDENTITY_INSERT catalogo.alcance_asignacion ON; INSERT INTO catalogo.alcance_asignacion(id_alcance_asignacion,codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES (1,'EMPRESA',N'Empresa',N'Empresa',1,1,SYSUTCDATETIME(),NULL); SET IDENTITY_INSERT catalogo.alcance_asignacion OFF; END;");
        await Exec(connection, "IF NOT EXISTS (SELECT 1 FROM catalogo.proposito_desafio_mfa WHERE codigo='LOGIN') BEGIN SET IDENTITY_INSERT catalogo.proposito_desafio_mfa ON; INSERT INTO catalogo.proposito_desafio_mfa(id_proposito_desafio_mfa,codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES (1,'LOGIN',N'Login',N'Login',1,1,SYSUTCDATETIME(),NULL); SET IDENTITY_INSERT catalogo.proposito_desafio_mfa OFF; END;");
        await Exec(connection, "IF NOT EXISTS (SELECT 1 FROM catalogo.proposito_desafio_mfa WHERE codigo='STEP_UP') INSERT INTO catalogo.proposito_desafio_mfa(codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES ('STEP_UP',N'StepUp',N'StepUp',2,1,SYSUTCDATETIME(),NULL);");
        await Exec(connection, "IF NOT EXISTS (SELECT 1 FROM catalogo.canal_notificacion WHERE codigo='TOTP') BEGIN SET IDENTITY_INSERT catalogo.canal_notificacion ON; INSERT INTO catalogo.canal_notificacion(id_canal_notificacion,codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES (1,'TOTP',N'TOTP',N'TOTP',1,1,SYSUTCDATETIME(),NULL); SET IDENTITY_INSERT catalogo.canal_notificacion OFF; END;");

        await using (SqlCommand ids = connection.CreateCommand())
        {
            ids.CommandText = "SELECT CAST((SELECT TOP 1 id_efecto_permiso FROM catalogo.efecto_permiso WHERE codigo='ALLOW') AS SMALLINT), CAST((SELECT TOP 1 id_efecto_permiso FROM catalogo.efecto_permiso WHERE codigo='DENY') AS SMALLINT), CAST((SELECT TOP 1 id_alcance_asignacion FROM catalogo.alcance_asignacion WHERE codigo='EMPRESA') AS SMALLINT);";
            await using SqlDataReader reader = await ids.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                AllowEffectId = reader.GetInt16(0);
                DenyEffectId = reader.GetInt16(1);
                ScopeCompanyId = reader.GetInt16(2);
            }
        }
    }

    private async Task SeedPrincipalAsync()
    {
        string suffix = _suffix.ToString("N");
        TenantCode = $"TENANT_{suffix[..10]}";
        Login = $"user_{suffix[..12]}@secureerp.local";
        PermissionCode = $"SEC.TEST.{suffix[..12]}";

        byte[] salt = RandomNumberGenerator.GetBytes(32);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(Password, salt, PasswordIterations, HashAlgorithmName.SHA512, 64);

        await using SqlConnection connection = new(ConnectionString);
        await connection.OpenAsync();

        await using (SqlCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO plataforma.tenant(codigo,nombre,descripcion,dominio_principal,activo,creado_utc,actualizado_utc,es_entrenamiento) OUTPUT INSERTED.id_tenant VALUES (@c,@n,@d,@dom,1,SYSUTCDATETIME(),NULL,0);";
            cmd.Parameters.Add(new SqlParameter("@c", SqlDbType.NVarChar, 100) { Value = TenantCode });
            cmd.Parameters.Add(new SqlParameter("@n", SqlDbType.NVarChar, 400) { Value = $"Tenant {suffix[..8]}" });
            cmd.Parameters.Add(new SqlParameter("@d", SqlDbType.NVarChar, 1000) { Value = "Integration tenant" });
            cmd.Parameters.Add(new SqlParameter("@dom", SqlDbType.NVarChar, 400) { Value = $"tenant-{suffix[..8]}.local" });
            TenantId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
        }

        await SetSessionContextAsync(connection, "id_tenant", TenantId);
        await SetSessionContextAsync(connection, "es_admin_tenant", 1);
        await using (SqlCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO organizacion.empresa(id_tenant,codigo,nombre,nombre_legal,id_tipo_empresa,id_estado_empresa,identificacion_fiscal,moneda_base,zona_horaria,activo,creado_utc,actualizado_utc) OUTPUT INSERTED.id_empresa VALUES (@t,@c,@n,@nl,1,1,@f,'USD','America/Guatemala',1,SYSUTCDATETIME(),NULL);";
            cmd.Parameters.Add(new SqlParameter("@t", SqlDbType.BigInt) { Value = TenantId });
            cmd.Parameters.Add(new SqlParameter("@c", SqlDbType.NVarChar, 100) { Value = $"EMP_{suffix[..8]}" });
            cmd.Parameters.Add(new SqlParameter("@n", SqlDbType.NVarChar, 500) { Value = $"Empresa {suffix[..8]}" });
            cmd.Parameters.Add(new SqlParameter("@nl", SqlDbType.NVarChar, 600) { Value = $"Empresa Legal {suffix[..8]}" });
            cmd.Parameters.Add(new SqlParameter("@f", SqlDbType.NVarChar, 100) { Value = $"NIT-{suffix[..8]}" });
            CompanyId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
        }

        await SetSessionContextAsync(connection, "id_empresa", CompanyId);
        await using (SqlCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO seguridad.usuario(codigo,login_principal,login_normalizado,nombre,apellido,nombre_mostrar,correo_electronico,correo_normalizado,telefono_movil,idioma,zona_horaria,id_estado_usuario,bloqueado_hasta_utc,mfa_habilitado,requiere_cambio_clave,ultimo_acceso_utc,activo,creado_por,creado_utc,actualizado_por,actualizado_utc) OUTPUT INSERTED.id_usuario VALUES (@c,@l,@ln,N'Integration',N'User',N'Integration User',@l,@ln,NULL,N'es-GT',N'America/Guatemala',1,NULL,1,0,NULL,1,NULL,SYSUTCDATETIME(),NULL,NULL);";
            cmd.Parameters.Add(new SqlParameter("@c", SqlDbType.NVarChar, 100) { Value = $"USR_{suffix[..10]}" });
            cmd.Parameters.Add(new SqlParameter("@l", SqlDbType.NVarChar, 240) { Value = Login });
            cmd.Parameters.Add(new SqlParameter("@ln", SqlDbType.NVarChar, 500) { Value = Login.ToUpperInvariant() });
            UserId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
        }

        await SetSessionContextAsync(connection, "id_usuario", UserId);
        await Exec(connection, "INSERT INTO seguridad.usuario_tenant(id_usuario,id_tenant,es_administrador_tenant,es_cuenta_servicio,activo,creado_utc,actualizado_utc) VALUES (@u,@t,0,0,1,SYSUTCDATETIME(),NULL);", ("@u", SqlDbType.BigInt, UserId), ("@t", SqlDbType.BigInt, TenantId));
        await Exec(connection, "INSERT INTO seguridad.usuario_empresa(id_usuario,id_tenant,id_empresa,es_empresa_predeterminada,puede_operar,fecha_inicio_utc,fecha_fin_utc,activo,creado_utc,actualizado_utc) VALUES (@u,@t,@e,1,1,DATEADD(MINUTE,-5,SYSUTCDATETIME()),NULL,1,SYSUTCDATETIME(),NULL);", ("@u", SqlDbType.BigInt, UserId), ("@t", SqlDbType.BigInt, TenantId), ("@e", SqlDbType.BigInt, CompanyId));
        await Exec(connection, "INSERT INTO seguridad.credencial_local_usuario(id_usuario,hash_clave,salt_clave,algoritmo_clave,iteraciones_clave,cambio_clave_utc,debe_cambiar_clave,activo) VALUES (@u,@h,@s,@a,@i,SYSUTCDATETIME(),0,1);", ("@u", SqlDbType.BigInt, UserId), ("@h", SqlDbType.VarBinary, hash), ("@s", SqlDbType.VarBinary, salt), ("@a", SqlDbType.VarChar, PasswordAlgorithm), ("@i", SqlDbType.Int, PasswordIterations));

        await using (SqlCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO seguridad.permiso(codigo,modulo,accion,nombre,descripcion,es_sensible,activo,creado_utc,actualizado_utc) OUTPUT INSERTED.id_permiso VALUES (@c,N'Security',N'Read',N'Integration Permission',N'Integration',0,1,SYSUTCDATETIME(),NULL);";
            cmd.Parameters.Add(new SqlParameter("@c", SqlDbType.NVarChar, 300) { Value = PermissionCode });
            PermissionId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        await Exec(connection, "INSERT INTO seguridad.excepcion_permiso_usuario(id_usuario,id_tenant,id_permiso,id_efecto_permiso,id_alcance_asignacion,id_grupo_empresarial,id_empresa,id_unidad_organizativa,fecha_inicio_utc,fecha_fin_utc,concedido_por,motivo,activo,creado_utc,actualizado_utc) VALUES (@u,@t,@p,@ef,@alc,NULL,@e,NULL,DATEADD(MINUTE,-5,SYSUTCDATETIME()),NULL,NULL,N'Integration allow',1,SYSUTCDATETIME(),NULL);", ("@u", SqlDbType.BigInt, UserId), ("@t", SqlDbType.BigInt, TenantId), ("@p", SqlDbType.Int, PermissionId), ("@ef", SqlDbType.SmallInt, AllowEffectId), ("@alc", SqlDbType.SmallInt, ScopeCompanyId), ("@e", SqlDbType.BigInt, CompanyId));
    }

    public async Task AddExplicitDenyForPermissionAsync()
    {
        await using SqlConnection connection = new(ConnectionString);
        await connection.OpenAsync();
        await ApplyTenantCompanyContextAsync(connection);
        await Exec(connection, "INSERT INTO seguridad.excepcion_permiso_usuario(id_usuario,id_tenant,id_permiso,id_efecto_permiso,id_alcance_asignacion,id_grupo_empresarial,id_empresa,id_unidad_organizativa,fecha_inicio_utc,fecha_fin_utc,concedido_por,motivo,activo,creado_utc,actualizado_utc) VALUES (@u,@t,@p,@ef,@alc,NULL,@e,NULL,DATEADD(MINUTE,-5,SYSUTCDATETIME()),NULL,NULL,N'Integration deny',1,SYSUTCDATETIME(),NULL);", ("@u", SqlDbType.BigInt, UserId), ("@t", SqlDbType.BigInt, TenantId), ("@p", SqlDbType.Int, PermissionId), ("@ef", SqlDbType.SmallInt, DenyEffectId), ("@alc", SqlDbType.SmallInt, ScopeCompanyId), ("@e", SqlDbType.BigInt, CompanyId));
    }

    public async Task RemovePermissionExceptionsAsync()
    {
        await using SqlConnection connection = new(ConnectionString);
        await connection.OpenAsync();
        await ApplyTenantCompanyContextAsync(connection);
        await Exec(connection, "DELETE FROM seguridad.excepcion_permiso_usuario WHERE id_tenant=@t AND id_usuario=@u AND id_permiso=@p;", ("@t", SqlDbType.BigInt, TenantId), ("@u", SqlDbType.BigInt, UserId), ("@p", SqlDbType.Int, PermissionId));
    }

    private static async Task Exec(SqlConnection connection, string sql, params (string Name, SqlDbType Type, object Value)[] parameters)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = sql;
        foreach ((string name, SqlDbType type, object value) in parameters)
        {
            SqlParameter p = command.Parameters.Add(name, type);
            p.Value = value;
        }

        await command.ExecuteNonQueryAsync();
    }

    private static async Task SetSessionContextAsync(SqlConnection connection, string key, object value)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sys.sp_set_session_context";
        command.Parameters.Add(new SqlParameter("@key", SqlDbType.NVarChar, 128) { Value = key });
        command.Parameters.Add(new SqlParameter("@value", SqlDbType.Variant) { Value = value });
        command.Parameters.Add(new SqlParameter("@read_only", SqlDbType.Bit) { Value = false });
        await command.ExecuteNonQueryAsync();
    }

    private sealed class DeterministicMfaCodeService : IMfaCodeService
    {
        public string GenerateCode(int digits = 6) => DeterministicOtp;
        public byte[] GenerateSalt(int sizeBytes = 16)
        {
            byte[] salt = new byte[sizeBytes];
            for (int i = 0; i < sizeBytes; i++) salt[i] = (byte)(i + 1);
            return salt;
        }

        public byte[] ComputeHash(string otpCode, byte[] salt)
        {
            byte[] code = Encoding.UTF8.GetBytes(otpCode);
            byte[] payload = new byte[salt.Length + code.Length];
            Buffer.BlockCopy(salt, 0, payload, 0, salt.Length);
            Buffer.BlockCopy(code, 0, payload, salt.Length, code.Length);
            return SHA256.HashData(payload);
        }
    }
}
