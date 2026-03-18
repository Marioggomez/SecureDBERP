using Microsoft.Data.SqlClient;
using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Application.Modules.Workflow.Commands;
using SecureERP.Application.Modules.Workflow.DTOs;
using SecureERP.Infrastructure.Business;
using System.Data;

namespace SecureERP.Tests.Integration;

public sealed class OperationalSecurityPhase5aIntegrationTests
{
    [Fact]
    public async Task Login_ShouldTriggerRateLimit_WhenAttemptsExceedThreshold()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await UpsertPolicyAsync(scope.ConnectionString, "AUTH.LOGIN", 120, 1, 5, true);
        await CleanupRateLimitAsync(scope.ConnectionString, "AUTH.LOGIN");

        LoginResponse first = await scope.CreateLoginHandler().HandleAsync(new LoginRequest(scope.TenantCode, scope.Login, "WrongPassword1", "127.0.0.10", "SecureERP.Tests"));
        LoginResponse second = await scope.CreateLoginHandler().HandleAsync(new LoginRequest(scope.TenantCode, scope.Login, "WrongPassword2", "127.0.0.10", "SecureERP.Tests"));

        Assert.False(first.IsAuthenticated);
        Assert.False(second.IsAuthenticated);
        Assert.Equal("AUTH_REQUEST_REJECTED", second.ErrorCode);
    }

    [Fact]
    public async Task Login_ShouldLockout_AfterFailedAttempts()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await UpsertPolicyAsync(scope.ConnectionString, "AUTH.LOGIN", 120, 3, 1, true);
        await ResetLoginControlAsync(scope.ConnectionString, scope.Login, "127.0.0.11");

        await scope.CreateLoginHandler().HandleAsync(new LoginRequest(scope.TenantCode, scope.Login, "Wrong1", "127.0.0.11", "SecureERP.Tests"));
        await scope.CreateLoginHandler().HandleAsync(new LoginRequest(scope.TenantCode, scope.Login, "Wrong2", "127.0.0.11", "SecureERP.Tests"));
        await scope.CreateLoginHandler().HandleAsync(new LoginRequest(scope.TenantCode, scope.Login, "Wrong3", "127.0.0.11", "SecureERP.Tests"));
        await scope.CreateLoginHandler().HandleAsync(new LoginRequest(scope.TenantCode, scope.Login, "Wrong4", "127.0.0.11", "SecureERP.Tests"));
        LoginResponse lockout = await scope.CreateLoginHandler().HandleAsync(new LoginRequest(scope.TenantCode, scope.Login, scope.Password, "127.0.0.11", "SecureERP.Tests"));

        Assert.False(lockout.IsAuthenticated);
        Assert.Equal("AUTH_REQUEST_REJECTED", lockout.ErrorCode);
    }

    [Fact]
    public async Task MfaVerify_ShouldBeBlocked_ByAbuseRateLimit()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await UpsertPolicyAsync(scope.ConnectionString, "AUTH.MFA.VERIFY", 120, 1, null, false);
        await CleanupRateLimitAsync(scope.ConnectionString, "AUTH.MFA.VERIFY");

        LoginResponse login = await scope.CreateLoginHandler().HandleAsync(new LoginRequest(scope.TenantCode, scope.Login, scope.Password, "127.0.0.12", "SecureERP.Tests"));
        RequestMfaChallengeResponse challenge = await scope.CreateRequestMfaChallengeHandler().HandleAsync(
            new RequestMfaChallengeRequest(login.AuthFlowId, SecureERP.Domain.Modules.Security.MfaPurpose.Login, SecureERP.Domain.Modules.Security.MfaChannel.Totp, "LOGIN", "127.0.0.12", "SecureERP.Tests"));

        VerifyMfaChallengeResponse first = await scope.CreateVerifyMfaChallengeHandler().HandleAsync(
            new VerifyMfaChallengeRequest(challenge.ChallengeId!.Value, "000000", "127.0.0.12", "SecureERP.Tests"));
        VerifyMfaChallengeResponse second = await scope.CreateVerifyMfaChallengeHandler().HandleAsync(
            new VerifyMfaChallengeRequest(challenge.ChallengeId!.Value, "111111", "127.0.0.12", "SecureERP.Tests"));

        Assert.False(first.IsVerified);
        Assert.False(second.IsVerified);
        Assert.Equal("AUTH_REQUEST_REJECTED", second.ErrorCode);
    }

    [Fact]
    public async Task Login_ShouldBeDenied_WhenIpPolicyBlocksAddress()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await InsertBlockedIpAsync(scope.ConnectionString, "127.0.0.13");

        LoginResponse result = await scope.CreateLoginHandler().HandleAsync(
            new LoginRequest(scope.TenantCode, scope.Login, scope.Password, "127.0.0.13", "SecureERP.Tests"));

        Assert.False(result.IsAuthenticated);
        Assert.Equal("AUTH_REQUEST_REJECTED", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateSession_ShouldRespectRateLimit()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await UpsertPolicyAsync(scope.ConnectionString, "AUTH.VALIDATE_SESSION", 120, 2, null, false);
        await CleanupRateLimitAsync(scope.ConnectionString, "AUTH.VALIDATE_SESSION");

        (SelectCompanyResponse select, _) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        ValidateSessionResult first = await scope.CreateValidateSessionHandler().HandleAsync(
            new ValidateSessionRequest(select.AccessToken!, 30, true, "127.0.0.14"));
        ValidateSessionResult second = await scope.CreateValidateSessionHandler().HandleAsync(
            new ValidateSessionRequest(select.AccessToken!, 30, true, "127.0.0.14"));

        Assert.True(first.IsValid);
        Assert.False(second.IsValid);
        Assert.Equal("AUTH_REQUEST_REJECTED", second.ErrorCode);
    }

    [Fact]
    public async Task ApprovalCreate_ShouldRespectRateLimit()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await UpsertPolicyAsync(scope.ConnectionString, "WORKFLOW.APPROVAL_INSTANCE.CREATE", 120, 1, null, false);
        await CleanupRateLimitAsync(scope.ConnectionString, "WORKFLOW.APPROVAL_INSTANCE.CREATE");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "WORKFLOW.APPROVAL_INSTANCE.CREATE");
        await EnsureWorkflowSeedAsync(scope.ConnectionString);

        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        BusinessPilotRepository repository = new(scope.ConnectionFactory, scope.SessionContextApplier);
        long unitId = await repository.CreateUnitAsync(new SecureERP.Domain.Modules.Organization.OrganizationUnitToCreate(1, null, $"U{Guid.NewGuid():N}"[..20], "Unidad", 1, "ROOT/U", true, true, DateTime.UtcNow, null));
        long profileId = await EnsureApprovalProfileAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId);
        short stateId = await EnsureApprovalStateAsync(scope.ConnectionString);

        CreateApprovalInstanceHandler handler = new(repository, scope.ContextAccessor, scope.CreateOperationalSecurityService(), scope.AuthRepository);
        CreateApprovalInstanceResponse first = await handler.HandleAsync(
            new CreateApprovalInstanceRequest(unitId, profileId, "WF.PILOT", 5001, 1, stateId, null, "First", "{\"a\":1}", "127.0.0.15"));
        CreateApprovalInstanceResponse second = await handler.HandleAsync(
            new CreateApprovalInstanceRequest(unitId, profileId, "WF.PILOT", 5002, 1, stateId, null, "Second", "{\"a\":2}", "127.0.0.15"));

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal("AUTH_REQUEST_REJECTED", second.ErrorCode);
    }

    [Fact]
    public async Task SecurityEvents_ShouldPersist_ForOperationalAbuse()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await UpsertPolicyAsync(scope.ConnectionString, "AUTH.LOGIN", 120, 1, 5, true);
        await CleanupRateLimitAsync(scope.ConnectionString, "AUTH.LOGIN");

        await scope.CreateLoginHandler().HandleAsync(new LoginRequest(scope.TenantCode, scope.Login, "Wrong1", "127.0.0.16", "SecureERP.Tests"));
        await scope.CreateLoginHandler().HandleAsync(new LoginRequest(scope.TenantCode, scope.Login, "Wrong2", "127.0.0.16", "SecureERP.Tests"));

        await using SqlConnection connection = new(scope.ConnectionString);
        await connection.OpenAsync();
        await scope.ApplyTenantCompanyContextAsync(connection);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM seguridad.security_event_audit WHERE event_type IN ('RATE_LIMIT_HIT','LOGIN_LOCKOUT','IP_POLICY_DENY','AUTH_ABUSE_DETECTED','VALIDATE_SESSION_RATE_LIMIT_HIT','MFA_RATE_LIMIT_HIT');";
        int count = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.True(count > 0);
    }

    [Fact]
    public async Task Login_Response_ShouldBeUniform_ForUnknownAndKnownUserFailures()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await UpsertPolicyAsync(scope.ConnectionString, "AUTH.LOGIN", 120, 50, 5, true);

        LoginResponse unknown = await scope.CreateLoginHandler().HandleAsync(
            new LoginRequest(scope.TenantCode, "unknown.user@secureerp.local", "wrong", "127.0.0.17", "SecureERP.Tests"));
        LoginResponse knownWrong = await scope.CreateLoginHandler().HandleAsync(
            new LoginRequest(scope.TenantCode, scope.Login, "wrong", "127.0.0.18", "SecureERP.Tests"));

        Assert.False(unknown.IsAuthenticated);
        Assert.False(knownWrong.IsAuthenticated);
        Assert.Equal("AUTH_REQUEST_REJECTED", unknown.ErrorCode);
        Assert.Equal("AUTH_REQUEST_REJECTED", knownWrong.ErrorCode);
    }

    private static async Task UpsertPolicyAsync(string connectionString, string actionCode, int windowSeconds, int maxAttempts, int? lockoutMinutes, bool appliesLockout)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = """
            IF EXISTS (SELECT 1 FROM seguridad.politica_seguridad_operacional WHERE codigo_accion=@codigo)
            BEGIN
                UPDATE seguridad.politica_seguridad_operacional
                SET ventana_segundos=@ventana,max_intentos=@max,lockout_minutos=@lockout,aplica_lockout=@aplica,activo=1,actualizado_utc=SYSUTCDATETIME()
                WHERE codigo_accion=@codigo;
            END
            ELSE
            BEGIN
                INSERT INTO seguridad.politica_seguridad_operacional(codigo_accion,ventana_segundos,max_intentos,lockout_minutos,aplica_lockout,activo,creado_utc,actualizado_utc)
                VALUES(@codigo,@ventana,@max,@lockout,@aplica,1,SYSUTCDATETIME(),NULL);
            END;
            """;
        command.Parameters.Add(new SqlParameter("@codigo", SqlDbType.NVarChar, 100) { Value = actionCode });
        command.Parameters.Add(new SqlParameter("@ventana", SqlDbType.Int) { Value = windowSeconds });
        command.Parameters.Add(new SqlParameter("@max", SqlDbType.Int) { Value = maxAttempts });
        command.Parameters.Add(new SqlParameter("@lockout", SqlDbType.Int) { Value = (object?)lockoutMinutes ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@aplica", SqlDbType.Bit) { Value = appliesLockout });
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CleanupRateLimitAsync(string connectionString, string actionCode)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM seguridad.contador_rate_limit WHERE endpoint=@endpoint;";
        command.Parameters.Add(new SqlParameter("@endpoint", SqlDbType.NVarChar, 100) { Value = actionCode });
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ResetLoginControlAsync(string connectionString, string login, string ip)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM seguridad.control_intentos_login WHERE login_usuario=@login AND ip=@ip;";
        command.Parameters.Add(new SqlParameter("@login", SqlDbType.VarChar, 240) { Value = login });
        command.Parameters.Add(new SqlParameter("@ip", SqlDbType.VarChar, 45) { Value = ip });
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertBlockedIpAsync(string connectionString, string ip)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = """
            IF NOT EXISTS (SELECT 1 FROM seguridad.ip_bloqueada WHERE ip=@ip)
            BEGIN
                INSERT INTO seguridad.ip_bloqueada(ip,motivo,fecha_bloqueo,fecha_expiracion)
                VALUES(@ip,'test-block',SYSUTCDATETIME(),DATEADD(MINUTE,30,SYSUTCDATETIME()));
            END;
            """;
        command.Parameters.Add(new SqlParameter("@ip", SqlDbType.VarChar, 45) { Value = ip });
        await command.ExecuteNonQueryAsync();
    }

    private static async Task EnsurePermissionAllowAsync(string connectionString, long tenantId, long companyId, long userId, string permissionCode)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await SetSessionContextAsync(connection, "id_tenant", tenantId);
        await SetSessionContextAsync(connection, "id_empresa", companyId);
        await SetSessionContextAsync(connection, "id_usuario", userId);
        await SetSessionContextAsync(connection, "es_admin_tenant", 1);

        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = """
            IF NOT EXISTS (SELECT 1 FROM seguridad.permiso WHERE codigo=@codigo)
            BEGIN
                INSERT INTO seguridad.permiso(codigo,modulo,accion,nombre,descripcion,es_sensible,activo,creado_utc,actualizado_utc)
                VALUES(@codigo,N'Workflow',N'Create',N'Workflow Create',N'Workflow Create',1,1,SYSUTCDATETIME(),NULL);
            END;

            IF NOT EXISTS
            (
                SELECT 1
                FROM seguridad.excepcion_permiso_usuario e
                INNER JOIN seguridad.permiso p ON p.id_permiso=e.id_permiso
                INNER JOIN catalogo.efecto_permiso ef ON ef.id_efecto_permiso=e.id_efecto_permiso
                WHERE e.id_usuario=@u AND e.id_tenant=@t AND e.id_empresa=@e AND p.codigo=@codigo AND ef.codigo='ALLOW' AND e.activo=1
            )
            BEGIN
                INSERT INTO seguridad.excepcion_permiso_usuario
                (
                    id_usuario,id_tenant,id_permiso,id_efecto_permiso,id_alcance_asignacion,id_grupo_empresarial,id_empresa,id_unidad_organizativa,
                    fecha_inicio_utc,fecha_fin_utc,concedido_por,motivo,activo,creado_utc,actualizado_utc
                )
                VALUES
                (
                    @u,@t,(SELECT TOP 1 id_permiso FROM seguridad.permiso WHERE codigo=@codigo),
                    (SELECT TOP 1 id_efecto_permiso FROM catalogo.efecto_permiso WHERE codigo='ALLOW'),
                    (SELECT TOP 1 id_alcance_asignacion FROM catalogo.alcance_asignacion WHERE codigo='EMPRESA'),
                    NULL,@e,NULL,DATEADD(MINUTE,-5,SYSUTCDATETIME()),NULL,NULL,N'Phase5 allow',1,SYSUTCDATETIME(),NULL
                );
            END;
            """;
        command.Parameters.Add(new SqlParameter("@codigo", SqlDbType.NVarChar, 300) { Value = permissionCode });
        command.Parameters.Add(new SqlParameter("@u", SqlDbType.BigInt) { Value = userId });
        command.Parameters.Add(new SqlParameter("@t", SqlDbType.BigInt) { Value = tenantId });
        command.Parameters.Add(new SqlParameter("@e", SqlDbType.BigInt) { Value = companyId });
        await command.ExecuteNonQueryAsync();
    }

    private static async Task EnsureWorkflowSeedAsync(string connectionString)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await ExecNonQueryAsync(connection, "IF NOT EXISTS (SELECT 1 FROM catalogo.tipo_unidad_organizativa WHERE id_tipo_unidad_organizativa=1) BEGIN SET IDENTITY_INSERT catalogo.tipo_unidad_organizativa ON; INSERT INTO catalogo.tipo_unidad_organizativa(id_tipo_unidad_organizativa,codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES (1,'AREA',N'Area',N'Area',1,1,SYSUTCDATETIME(),NULL); SET IDENTITY_INSERT catalogo.tipo_unidad_organizativa OFF; END;");
        await ExecNonQueryAsync(connection, "IF NOT EXISTS (SELECT 1 FROM catalogo.estado_aprobacion WHERE id_estado_aprobacion=1) BEGIN SET IDENTITY_INSERT catalogo.estado_aprobacion ON; INSERT INTO catalogo.estado_aprobacion(id_estado_aprobacion,codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES (1,'PENDIENTE',N'Pendiente',N'Pendiente',1,1,SYSUTCDATETIME(),NULL); SET IDENTITY_INSERT catalogo.estado_aprobacion OFF; END;");
        await ExecNonQueryAsync(connection, "IF NOT EXISTS (SELECT 1 FROM seguridad.entidad_alcance_dato WHERE codigo_entidad='WF.PILOT') INSERT INTO seguridad.entidad_alcance_dato(codigo_entidad,nombre_tabla,columna_llave_primaria,columna_tenant,columna_empresa,columna_unidad_organizativa,columna_propietario,columna_contexto,descripcion,activo,creado_utc,actualizado_utc,modo_scope,codigo_entidad_raiz) VALUES ('WF.PILOT','cumplimiento.instancia_aprobacion','id_instancia_aprobacion','id_tenant','id_empresa','id_unidad_organizativa',NULL,NULL,'WF Pilot',1,SYSUTCDATETIME(),NULL,'DIRECTO',NULL);");
    }

    private static async Task<long> EnsureApprovalProfileAsync(string connectionString, long tenantId, long companyId)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await SetSessionContextAsync(connection, "id_tenant", tenantId);
        await SetSessionContextAsync(connection, "id_empresa", companyId);
        await SetSessionContextAsync(connection, "es_admin_tenant", 1);
        string code = $"P5_{Guid.NewGuid():N}"[..20];

        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO cumplimiento.perfil_aprobacion
            (
                id_tenant,id_empresa,codigo,codigo_entidad,tipo_proceso,requiere_mfa,impide_autoaprobacion,impide_misma_unidad,activo,creado_utc,actualizado_utc
            )
            OUTPUT INSERTED.id_perfil_aprobacion
            VALUES
            (@t,@e,@c,N'WF.PILOT','MANUAL',1,0,0,1,SYSUTCDATETIME(),NULL);
            """;
        command.Parameters.Add(new SqlParameter("@t", SqlDbType.BigInt) { Value = tenantId });
        command.Parameters.Add(new SqlParameter("@e", SqlDbType.BigInt) { Value = companyId });
        command.Parameters.Add(new SqlParameter("@c", SqlDbType.NVarChar, 80) { Value = code });
        object? id = await command.ExecuteScalarAsync();
        return Convert.ToInt64(id);
    }

    private static async Task<short> EnsureApprovalStateAsync(string connectionString)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT TOP 1 id_estado_aprobacion FROM catalogo.estado_aprobacion ORDER BY id_estado_aprobacion;";
        object? value = await command.ExecuteScalarAsync();
        return Convert.ToInt16(value);
    }

    private static async Task ExecNonQueryAsync(SqlConnection connection, string sql)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = sql;
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
}
