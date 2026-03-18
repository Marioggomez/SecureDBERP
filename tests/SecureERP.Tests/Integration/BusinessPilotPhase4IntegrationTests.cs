using Microsoft.Data.SqlClient;
using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Application.Modules.Security.Services;
using SecureERP.Application.Modules.Workflow.Commands;
using SecureERP.Application.Modules.Workflow.DTOs;
using SecureERP.Application.Modules.Workflow.Queries;
using SecureERP.Domain.Modules.Organization;
using SecureERP.Infrastructure.Business;
using System.Data;

namespace SecureERP.Tests.Integration;

public sealed class BusinessPilotPhase4IntegrationTests
{
    private const string WorkflowReadPermission = "WORKFLOW.APPROVAL_INSTANCE.READ";
    private const string WorkflowCreatePermission = "WORKFLOW.APPROVAL_INSTANCE.CREATE";

    [Fact]
    public async Task Pilot_UserWithPermissionAndUnitScope_ShouldSeeOnlyScopedApprovalInstances()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        await EnsurePilotCatalogsAsync(scope.ConnectionString);
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, WorkflowReadPermission);

        BusinessPilotRepository repository = new(scope.ConnectionFactory, scope.SessionContextApplier);
        long unitA = await CreateOrganizationUnitAsync(repository, "U-A");
        long unitB = await CreateOrganizationUnitAsync(repository, "U-B");
        long profileId = await EnsureApprovalProfileAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId);
        short stateId = await EnsureApprovalStateAsync(scope.ConnectionString);

        CreateApprovalInstanceHandler createHandler = new(repository, scope.ContextAccessor, scope.CreateOperationalSecurityService(), scope.AuthRepository);
        await createHandler.HandleAsync(new CreateApprovalInstanceRequest(unitA, profileId, "WF.PILOT", 1001, 1, stateId, null, "A", "{\"pilot\":1}"));
        await createHandler.HandleAsync(new CreateApprovalInstanceRequest(unitB, profileId, "WF.PILOT", 1002, 1, stateId, null, "B", "{\"pilot\":2}"));
        await AddUserUnitScopeAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, unitA);

        ListApprovalInstancesHandler listHandler = new(repository);
        IReadOnlyList<ApprovalInstanceDto> rows = await listHandler.HandleAsync();
        Assert.NotEmpty(rows);
        Assert.All(rows, row => Assert.Equal(unitA, row.OrganizationUnitId));
    }

    [Fact]
    public async Task Pilot_UserShouldNotSeeOtherCompanyData()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        await EnsurePilotCatalogsAsync(scope.ConnectionString);
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, WorkflowReadPermission);
        BusinessPilotRepository repository = new(scope.ConnectionFactory, scope.SessionContextApplier);

        long profileId = await EnsureApprovalProfileAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId);
        short stateId = await EnsureApprovalStateAsync(scope.ConnectionString);
        long ownUnit = await CreateOrganizationUnitAsync(repository, "U-OWN");
        await AddUserUnitScopeAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, ownUnit);
        CreateApprovalInstanceHandler createHandler = new(repository, scope.ContextAccessor, scope.CreateOperationalSecurityService(), scope.AuthRepository);
        await createHandler.HandleAsync(new CreateApprovalInstanceRequest(ownUnit, profileId, "WF.PILOT", 2001, 1, stateId, null, "OWN", "{\"pilot\":true}"));

        long otherCompanyId = await CreateCompanyInTenantAsync(scope.ConnectionString, scope.TenantId, "EMP_OTRA");
        await InsertApprovalInstanceInCompanyAsync(scope.ConnectionString, scope.TenantId, otherCompanyId);

        ListApprovalInstancesHandler listHandler = new(repository);
        IReadOnlyList<ApprovalInstanceDto> rows = await listHandler.HandleAsync();
        Assert.NotEmpty(rows);
        Assert.All(rows, row => Assert.Equal(scope.CompanyId, row.CompanyId));
        Assert.DoesNotContain(rows, row => row.CompanyId == otherCompanyId);
    }

    [Fact]
    public async Task Pilot_UserWithoutPermission_ShouldBeDenied_WithValidSession()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        AuthorizationEvaluator evaluator = scope.CreateAuthorizationEvaluator();
        AuthorizationCheckResult decision = await evaluator.EvaluateAsync(
            new AuthorizationCheckRequest(
                "WORKFLOW.APPROVAL_INSTANCE.READ.DENYCASE",
                false,
                "/api/v1/workflow/approval-instances",
                "GET",
                "127.0.0.1",
                "SecureERP.Tests",
                Guid.NewGuid().ToString()));

        Assert.False(decision.IsAllowed);
        Assert.Equal("DENY_DEFAULT", decision.ReasonCode);
    }

    [Fact]
    public async Task Pilot_AuthorizationAudit_ShouldPersist_ForPilotPermission()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, WorkflowReadPermission);

        AuthorizationEvaluator evaluator = scope.CreateAuthorizationEvaluator();
        AuthorizationCheckResult decision = await evaluator.EvaluateAsync(
            new AuthorizationCheckRequest(
                WorkflowReadPermission,
                false,
                "/api/v1/workflow/approval-instances",
                "GET",
                "127.0.0.1",
                "SecureERP.Tests",
                Guid.NewGuid().ToString()));
        Assert.True(decision.IsAllowed);

        await using SqlConnection connection = new(scope.ConnectionString);
        await connection.OpenAsync();
        await scope.ApplyTenantCompanyContextAsync(connection);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM observabilidad.auditoria_autorizacion WHERE id_tenant=@t AND id_usuario=@u AND id_empresa=@e AND codigo_permiso=@p;";
        command.Parameters.Add(new SqlParameter("@t", SqlDbType.BigInt) { Value = scope.TenantId });
        command.Parameters.Add(new SqlParameter("@u", SqlDbType.BigInt) { Value = scope.UserId });
        command.Parameters.Add(new SqlParameter("@e", SqlDbType.BigInt) { Value = scope.CompanyId });
        command.Parameters.Add(new SqlParameter("@p", SqlDbType.NVarChar, 150) { Value = WorkflowReadPermission });
        int count = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.True(count > 0);
    }

    [Fact]
    public async Task Pilot_SensitiveCreatePermission_ShouldDeny_WhenMfaIsNotValidated()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: false);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, WorkflowCreatePermission);

        AuthorizationEvaluator evaluator = scope.CreateAuthorizationEvaluator();
        AuthorizationCheckResult decision = await evaluator.EvaluateAsync(
            new AuthorizationCheckRequest(
                WorkflowCreatePermission,
                true,
                "/api/v1/workflow/approval-instances",
                "POST",
                "127.0.0.1",
                "SecureERP.Tests",
                Guid.NewGuid().ToString()));

        Assert.False(decision.IsAllowed);
        Assert.Equal("MFA_REQUIRED", decision.ReasonCode);
    }

    private static async Task EnsurePilotCatalogsAsync(string connectionString)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await ExecAsync(connection, "IF NOT EXISTS (SELECT 1 FROM catalogo.tipo_unidad_organizativa WHERE id_tipo_unidad_organizativa=1) BEGIN SET IDENTITY_INSERT catalogo.tipo_unidad_organizativa ON; INSERT INTO catalogo.tipo_unidad_organizativa(id_tipo_unidad_organizativa,codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES (1,'AREA',N'Area',N'Area',1,1,SYSUTCDATETIME(),NULL); SET IDENTITY_INSERT catalogo.tipo_unidad_organizativa OFF; END;");
        await ExecAsync(connection, "IF NOT EXISTS (SELECT 1 FROM catalogo.estado_aprobacion WHERE id_estado_aprobacion=1) BEGIN SET IDENTITY_INSERT catalogo.estado_aprobacion ON; INSERT INTO catalogo.estado_aprobacion(id_estado_aprobacion,codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES (1,'PENDIENTE',N'Pendiente',N'Pendiente',1,1,SYSUTCDATETIME(),NULL); SET IDENTITY_INSERT catalogo.estado_aprobacion OFF; END;");
    }

    private static async Task EnsurePermissionAllowAsync(
        string connectionString,
        long tenantId,
        long companyId,
        long userId,
        string permissionCode)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await SetSessionContextAsync(connection, "id_tenant", tenantId);
        await SetSessionContextAsync(connection, "id_empresa", companyId);
        await SetSessionContextAsync(connection, "id_usuario", userId);
        await SetSessionContextAsync(connection, "es_admin_tenant", 1);

        await ExecAsync(connection, """
            IF NOT EXISTS (SELECT 1 FROM seguridad.permiso WHERE codigo = @codigo)
            BEGIN
                INSERT INTO seguridad.permiso(codigo,modulo,accion,nombre,descripcion,es_sensible,activo,creado_utc,actualizado_utc)
                VALUES (@codigo,N'Workflow',N'Read',N'Pilot permission',N'Pilot permission',0,1,SYSUTCDATETIME(),NULL);
            END;
            """, ("@codigo", SqlDbType.NVarChar, permissionCode, 300));

        await ExecAsync(connection, """
            IF NOT EXISTS
            (
                SELECT 1
                FROM seguridad.excepcion_permiso_usuario e
                INNER JOIN seguridad.permiso p ON p.id_permiso = e.id_permiso
                INNER JOIN catalogo.efecto_permiso ef ON ef.id_efecto_permiso = e.id_efecto_permiso
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
                    NULL,@e,NULL,DATEADD(MINUTE,-5,SYSUTCDATETIME()),NULL,NULL,N'Pilot allow',1,SYSUTCDATETIME(),NULL
                );
            END;
            """,
            ("@u", SqlDbType.BigInt, userId, null),
            ("@t", SqlDbType.BigInt, tenantId, null),
            ("@e", SqlDbType.BigInt, companyId, null),
            ("@codigo", SqlDbType.NVarChar, permissionCode, 300));
    }

    private static async Task<long> CreateOrganizationUnitAsync(BusinessPilotRepository repository, string suffix)
    {
        return await repository.CreateUnitAsync(
            new OrganizationUnitToCreate(
                1,
                null,
                $"UNIT_{suffix}_{Guid.NewGuid():N}"[..20],
                $"Unidad {suffix}",
                1,
                $"ROOT/{suffix}",
                true,
                true,
                DateTime.UtcNow,
                null));
    }

    private static async Task<long> EnsureApprovalProfileAsync(string connectionString, long tenantId, long companyId)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await SetSessionContextAsync(connection, "id_tenant", tenantId);
        await SetSessionContextAsync(connection, "id_empresa", companyId);
        await SetSessionContextAsync(connection, "es_admin_tenant", 1);
        await EnsureApprovalEntityScopeAsync(connection);

        string code = $"PRF_{Guid.NewGuid():N}"[..20];
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

    private static async Task EnsureApprovalEntityScopeAsync(SqlConnection connection)
    {
        await ExecAsync(connection, """
            IF NOT EXISTS (SELECT 1 FROM seguridad.entidad_alcance_dato WHERE codigo_entidad = N'WF.PILOT')
            BEGIN
                INSERT INTO seguridad.entidad_alcance_dato
                (
                    codigo_entidad,nombre_tabla,columna_llave_primaria,columna_tenant,columna_empresa,columna_unidad_organizativa,columna_propietario,columna_contexto,descripcion,activo,creado_utc,actualizado_utc,modo_scope,codigo_entidad_raiz
                )
                VALUES
                (
                    N'WF.PILOT',N'cumplimiento.instancia_aprobacion',N'id_instancia_aprobacion',N'id_tenant',N'id_empresa',N'id_unidad_organizativa',NULL,NULL,N'Pilot workflow entity',1,SYSUTCDATETIME(),NULL,N'DIRECTO',NULL
                );
            END;
            """);
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

    private static async Task AddUserUnitScopeAsync(string connectionString, long tenantId, long companyId, long userId, long unitId)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await SetSessionContextAsync(connection, "id_tenant", tenantId);
        await SetSessionContextAsync(connection, "id_empresa", companyId);
        await SetSessionContextAsync(connection, "id_usuario", userId);
        await SetSessionContextAsync(connection, "es_admin_tenant", 1);

        await ExecAsync(connection, """
            IF NOT EXISTS
            (
                SELECT 1 FROM seguridad.usuario_scope_unidad
                WHERE id_usuario=@u AND id_tenant=@t AND id_empresa=@e AND id_unidad_organizativa=@unit AND activo=1
            )
            BEGIN
                INSERT INTO seguridad.usuario_scope_unidad
                (
                    id_usuario,id_unidad_organizativa,id_tenant,id_empresa,fecha_inicio_utc,fecha_fin_utc,activo,creado_utc,actualizado_utc
                )
                VALUES
                (
                    @u,@unit,@t,@e,DATEADD(MINUTE,-5,SYSUTCDATETIME()),NULL,1,SYSUTCDATETIME(),NULL
                );
            END;
            """,
            ("@u", SqlDbType.BigInt, userId, null),
            ("@t", SqlDbType.BigInt, tenantId, null),
            ("@e", SqlDbType.BigInt, companyId, null),
            ("@unit", SqlDbType.BigInt, unitId, null));
    }

    private static async Task<long> CreateCompanyInTenantAsync(string connectionString, long tenantId, string suffix)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await SetSessionContextAsync(connection, "id_tenant", tenantId);
        await SetSessionContextAsync(connection, "es_admin_tenant", 1);

        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO organizacion.empresa
            (
                id_tenant,codigo,nombre,nombre_legal,id_tipo_empresa,id_estado_empresa,identificacion_fiscal,moneda_base,zona_horaria,activo,creado_utc,actualizado_utc
            )
            OUTPUT INSERTED.id_empresa
            VALUES
            (@t,@c,@n,@nl,1,1,@f,'USD','America/Guatemala',1,SYSUTCDATETIME(),NULL);
            """;
        cmd.Parameters.Add(new SqlParameter("@t", SqlDbType.BigInt) { Value = tenantId });
        cmd.Parameters.Add(new SqlParameter("@c", SqlDbType.NVarChar, 100) { Value = $"{suffix}_{Guid.NewGuid():N}"[..20] });
        cmd.Parameters.Add(new SqlParameter("@n", SqlDbType.NVarChar, 500) { Value = $"Empresa {suffix}" });
        cmd.Parameters.Add(new SqlParameter("@nl", SqlDbType.NVarChar, 600) { Value = $"Empresa Legal {suffix}" });
        cmd.Parameters.Add(new SqlParameter("@f", SqlDbType.NVarChar, 100) { Value = $"NIT-{Guid.NewGuid():N}"[..16] });
        object? id = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(id);
    }

    private static async Task InsertApprovalInstanceInCompanyAsync(string connectionString, long tenantId, long companyId)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await SetSessionContextAsync(connection, "id_tenant", tenantId);
        await SetSessionContextAsync(connection, "id_empresa", companyId);
        await SetSessionContextAsync(connection, "es_admin_tenant", 1);
        await EnsureApprovalEntityScopeAsync(connection);

        long unitId;
        await using (SqlCommand unitCmd = connection.CreateCommand())
        {
            unitCmd.CommandText = """
                INSERT INTO organizacion.unidad_organizativa
                (
                    id_tenant,id_empresa,id_tipo_unidad_organizativa,id_unidad_padre,codigo,nombre,nivel_jerarquia,ruta_jerarquia,es_hoja,activo,creado_utc,actualizado_utc
                )
                OUTPUT INSERTED.id_unidad_organizativa
                VALUES
                (@t,@e,1,NULL,@c,N'Unidad Externa',1,@r,1,1,SYSUTCDATETIME(),NULL);
                """;
            unitCmd.Parameters.Add(new SqlParameter("@t", SqlDbType.BigInt) { Value = tenantId });
            unitCmd.Parameters.Add(new SqlParameter("@e", SqlDbType.BigInt) { Value = companyId });
            unitCmd.Parameters.Add(new SqlParameter("@c", SqlDbType.NVarChar, 60) { Value = $"UX_{Guid.NewGuid():N}"[..20] });
            unitCmd.Parameters.Add(new SqlParameter("@r", SqlDbType.NVarChar, 500) { Value = "ROOT/EXTERNAL" });
            unitId = Convert.ToInt64(await unitCmd.ExecuteScalarAsync());
        }

        long profileId = await EnsureApprovalProfileAsync(connectionString, tenantId, companyId);
        short stateId = await EnsureApprovalStateAsync(connectionString);
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO cumplimiento.instancia_aprobacion
            (
                id_tenant,id_empresa,id_unidad_organizativa,id_perfil_aprobacion,codigo_entidad,id_objeto,nivel_actual,id_estado_aprobacion,solicitado_por,solicitado_utc,expira_utc,motivo,hash_payload,activo
            )
            VALUES
            (@t,@e,@u,@p,N'WF.PILOT',9001,1,@s,1,SYSUTCDATETIME(),NULL,N'other-company',CONVERT(binary(32), HASHBYTES('SHA2_256', CONVERT(varbinary(max), NEWID()))),1);
            """;
        cmd.Parameters.Add(new SqlParameter("@t", SqlDbType.BigInt) { Value = tenantId });
        cmd.Parameters.Add(new SqlParameter("@e", SqlDbType.BigInt) { Value = companyId });
        cmd.Parameters.Add(new SqlParameter("@u", SqlDbType.BigInt) { Value = unitId });
        cmd.Parameters.Add(new SqlParameter("@p", SqlDbType.BigInt) { Value = profileId });
        cmd.Parameters.Add(new SqlParameter("@s", SqlDbType.SmallInt) { Value = stateId });
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ExecAsync(SqlConnection connection, string sql, params (string Name, SqlDbType Type, object Value, int? Size)[] parameters)
    {
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = CommandType.Text;
        foreach ((string name, SqlDbType type, object value, int? size) in parameters)
        {
            SqlParameter p = size is null ? new SqlParameter(name, type) : new SqlParameter(name, type, size.Value);
            p.Value = value;
            cmd.Parameters.Add(p);
        }

        await cmd.ExecuteNonQueryAsync();
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
