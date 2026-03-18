using Microsoft.Data.SqlClient;
using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Purchase.Commands;
using SecureERP.Application.Modules.Purchase.DTOs;
using SecureERP.Application.Modules.Purchase.Queries;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Application.Modules.Security.Services;
using SecureERP.Domain.Modules.Security;
using SecureERP.Infrastructure.Business;
using SecureERP.Infrastructure.Persistence.Db;
using SecureERP.Infrastructure.Persistence.SessionContext;
using SecureERP.Infrastructure.Security;
using System.Data;

namespace SecureERP.Tests.Integration;

public sealed class PurchaseOrderModuleIntegrationTests
{
    [Fact]
    public async Task PurchaseOrder_CreateDraft_ShouldSucceed()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.CREATE");
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        PurchaseOrderRepository repository = new(scope.ConnectionFactory, scope.SessionContextApplier);
        CreatePurchaseOrderHandler create = new(repository, scope.ContextAccessor);
        GetPurchaseOrderByIdHandler get = new(repository);

        CreatePurchaseOrderResponse created = await create.HandleAsync(
            new CreatePurchaseOrderRequest(null, DateTime.UtcNow.Date, "Draft create test"));
        PurchaseOrderDto? fetched = await get.HandleAsync(created.PurchaseOrderId!.Value);

        Assert.True(created.IsSuccess);
        Assert.NotNull(fetched);
        Assert.Equal("DRAFT", fetched!.StateCode);
    }

    [Fact]
    public async Task PurchaseOrder_List_ShouldOnlyReturnCurrentCompanyRows()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.READ");
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        PurchaseOrderRepository repository = new(scope.ConnectionFactory, scope.SessionContextApplier);
        CreatePurchaseOrderHandler create = new(repository, scope.ContextAccessor);
        ListPurchaseOrdersHandler list = new(repository);

        await create.HandleAsync(new CreatePurchaseOrderRequest(null, DateTime.UtcNow.Date, "Own company row"));
        long otherCompanyId = await CreateCompanyInTenantAsync(scope.ConnectionString, scope.TenantId, "PR_OTH");
        await InsertPurchaseOrderInCompanyAsync(scope.ConnectionString, scope.TenantId, otherCompanyId, scope.UserId);

        IReadOnlyList<PurchaseOrderListItemDto> rows = await list.HandleAsync();
        Assert.NotEmpty(rows);

        foreach (PurchaseOrderListItemDto row in rows)
        {
            PurchaseOrderDto? full = await new GetPurchaseOrderByIdHandler(repository).HandleAsync(row.PurchaseOrderId);
            Assert.NotNull(full);
            Assert.Equal(scope.CompanyId, full!.CompanyId);
        }
    }

    [Fact]
    public async Task PurchaseOrder_GetById_ShouldNotExposeOtherCompanyRow()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.READ");
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        long otherCompanyId = await CreateCompanyInTenantAsync(scope.ConnectionString, scope.TenantId, "PR_NOVIS");
        long otherRequestId = await InsertPurchaseOrderInCompanyAsync(scope.ConnectionString, scope.TenantId, otherCompanyId, scope.UserId);

        PurchaseOrderRepository repository = new(scope.ConnectionFactory, scope.SessionContextApplier);
        PurchaseOrderDto? result = await new GetPurchaseOrderByIdHandler(repository).HandleAsync(otherRequestId);
        Assert.Null(result);
    }

    [Fact]
    public async Task PurchaseOrder_Update_ShouldOnlyAllowDraft()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.UPDATE");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.SUBMIT");
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        PurchaseOrderRepository repository = new(scope.ConnectionFactory, scope.SessionContextApplier);
        CreatePurchaseOrderHandler create = new(repository, scope.ContextAccessor);
        UpsertPurchaseOrderDetailHandler upsert = new(repository, scope.ContextAccessor);
        SubmitPurchaseOrderHandler submit = new(repository, scope.ContextAccessor, scope.CreateOperationalSecurityService(), scope.AuthRepository);
        UpdatePurchaseOrderDraftHandler update = new(repository, scope.ContextAccessor);

        long requestId = (await create.HandleAsync(new CreatePurchaseOrderRequest(null, DateTime.UtcNow.Date, "Update test"))).PurchaseOrderId!.Value;
        await upsert.HandleAsync(new UpsertPurchaseOrderDetailRequest(requestId, null, null, "Line", 1, 100, null));
        await submit.HandleAsync(new SubmitPurchaseOrderRequest(requestId));

        UpdatePurchaseOrderDraftResponse response = await update.HandleAsync(
            new UpdatePurchaseOrderDraftRequest(requestId, null, DateTime.UtcNow.Date, "Should fail"));

        Assert.False(response.IsSuccess);
        Assert.Equal("PURCHASE_ORDER_UPDATE_NOT_ALLOWED", response.ErrorCode);
    }

    [Fact]
    public async Task PurchaseOrder_Submit_ShouldTransitionToSubmitted()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.CREATE");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.UPDATE");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.SUBMIT");
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        PurchaseOrderRepository repository = new(scope.ConnectionFactory, scope.SessionContextApplier);
        CreatePurchaseOrderHandler create = new(repository, scope.ContextAccessor);
        UpsertPurchaseOrderDetailHandler upsert = new(repository, scope.ContextAccessor);
        SubmitPurchaseOrderHandler submit = new(repository, scope.ContextAccessor, scope.CreateOperationalSecurityService(), scope.AuthRepository);
        GetPurchaseOrderByIdHandler get = new(repository);

        long requestId = (await create.HandleAsync(new CreatePurchaseOrderRequest(null, DateTime.UtcNow.Date, "Submit test"))).PurchaseOrderId!.Value;
        await upsert.HandleAsync(new UpsertPurchaseOrderDetailRequest(requestId, null, null, "Line", 2, 25, "CC-01"));
        SubmitPurchaseOrderResponse result = await submit.HandleAsync(new SubmitPurchaseOrderRequest(requestId));
        PurchaseOrderDto? row = await get.HandleAsync(requestId);

        Assert.True(result.IsSuccess);
        Assert.Equal("SUBMITTED", result.NewStateCode);
        Assert.NotNull(row);
        Assert.Equal("SUBMITTED", row!.StateCode);
    }

    [Fact]
    public async Task PurchaseOrder_Approve_ShouldRequirePermission()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        AuthorizationEvaluator evaluator = scope.CreateAuthorizationEvaluator();
        AuthorizationCheckResult decision = await evaluator.EvaluateAsync(
            new AuthorizationCheckRequest(
                "PURCHASE.ORDER.APPROVE",
                true,
                "/api/v1/purchase/orders/1/approve",
                "POST",
                "127.0.0.1",
                "SecureERP.Tests",
                Guid.NewGuid().ToString()));

        Assert.False(decision.IsAllowed);
    }

    [Fact]
    public async Task PurchaseOrder_Approve_ShouldRequireMfa()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.APPROVE");
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: false);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        AuthorizationEvaluator evaluator = scope.CreateAuthorizationEvaluator();
        AuthorizationCheckResult decision = await evaluator.EvaluateAsync(
            new AuthorizationCheckRequest(
                "PURCHASE.ORDER.APPROVE",
                true,
                "/api/v1/purchase/orders/1/approve",
                "POST",
                "127.0.0.1",
                "SecureERP.Tests",
                Guid.NewGuid().ToString()));

        Assert.False(decision.IsAllowed);
        Assert.Equal("MFA_REQUIRED", decision.ReasonCode);
    }

    [Fact]
    public async Task PurchaseOrder_Approve_ShouldDenyCreatorBySoD()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.CREATE");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.UPDATE");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.SUBMIT");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.APPROVE");
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        PurchaseOrderRepository repository = new(scope.ConnectionFactory, scope.SessionContextApplier);
        CreatePurchaseOrderHandler create = new(repository, scope.ContextAccessor);
        UpsertPurchaseOrderDetailHandler upsert = new(repository, scope.ContextAccessor);
        SubmitPurchaseOrderHandler submit = new(repository, scope.ContextAccessor, scope.CreateOperationalSecurityService(), scope.AuthRepository);
        ApprovePurchaseOrderHandler approve = new(repository, scope.ContextAccessor, scope.CreateOperationalSecurityService(), scope.AuthRepository);

        long requestId = (await create.HandleAsync(new CreatePurchaseOrderRequest(null, DateTime.UtcNow.Date, "SoD"))).PurchaseOrderId!.Value;
        await upsert.HandleAsync(new UpsertPurchaseOrderDetailRequest(requestId, null, null, "Line", 1, 10, null));
        await submit.HandleAsync(new SubmitPurchaseOrderRequest(requestId));

        ApprovePurchaseOrderResponse result = await approve.HandleAsync(new ApprovePurchaseOrderRequest(requestId, "creator attempt"));
        Assert.False(result.IsSuccess);
        Assert.Equal("PURCHASE_ORDER_SOD_DENY", result.ErrorCode);
    }

    [Fact]
    public async Task PurchaseOrder_Approve_ShouldPersistAudit_WhenApproved()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        long approverUserId = await CreateApproverUserAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId);
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.CREATE");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.UPDATE");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.SUBMIT");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, approverUserId, "PURCHASE.ORDER.APPROVE");

        (_, ValidateSessionResult creatorSession) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(creatorSession.TenantId, creatorSession.CompanyId, creatorSession.UserId, creatorSession.SessionId, Guid.NewGuid().ToString()));

        PurchaseOrderRepository repository = new(scope.ConnectionFactory, scope.SessionContextApplier);
        long requestId = (await new CreatePurchaseOrderHandler(repository, scope.ContextAccessor)
            .HandleAsync(new CreatePurchaseOrderRequest(null, DateTime.UtcNow.Date, "Approve audit"))).PurchaseOrderId!.Value;
        await new UpsertPurchaseOrderDetailHandler(repository, scope.ContextAccessor)
            .HandleAsync(new UpsertPurchaseOrderDetailRequest(requestId, null, null, "Line", 1, 100, null));
        await new SubmitPurchaseOrderHandler(repository, scope.ContextAccessor, scope.CreateOperationalSecurityService(), scope.AuthRepository)
            .HandleAsync(new SubmitPurchaseOrderRequest(requestId));

        Guid approverSession = await CreateApproverSessionAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, approverUserId, true);
        scope.ContextAccessor.SetCurrent(new RequestContext(scope.TenantId, scope.CompanyId, approverUserId, approverSession, Guid.NewGuid().ToString()));

        ApprovePurchaseOrderResponse approved = await new ApprovePurchaseOrderHandler(repository, scope.ContextAccessor, scope.CreateOperationalSecurityService(), scope.AuthRepository)
            .HandleAsync(new ApprovePurchaseOrderRequest(requestId, "approved by second user"));
        Assert.True(approved.IsSuccess);

        await using SqlConnection connection = new(scope.ConnectionString);
        await connection.OpenAsync();
        await scope.ApplyTenantCompanyContextAsync(connection);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM cumplimiento.auditoria_operacion WHERE tabla='compras.orden_compra' AND operacion='APPROVE' AND id_registro=@id;";
        command.Parameters.Add(new SqlParameter("@id", SqlDbType.BigInt) { Value = requestId });
        int count = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.True(count > 0);
    }

    [Fact]
    public async Task PurchaseOrder_Approve_ShouldPersistSecurityEvent_WhenApproved()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        long approverUserId = await CreateApproverUserAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId);
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.CREATE");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.UPDATE");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.SUBMIT");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, approverUserId, "PURCHASE.ORDER.APPROVE");

        (_, ValidateSessionResult creatorSession) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(creatorSession.TenantId, creatorSession.CompanyId, creatorSession.UserId, creatorSession.SessionId, Guid.NewGuid().ToString()));

        PurchaseOrderRepository repository = new(scope.ConnectionFactory, scope.SessionContextApplier);
        long requestId = (await new CreatePurchaseOrderHandler(repository, scope.ContextAccessor)
            .HandleAsync(new CreatePurchaseOrderRequest(null, DateTime.UtcNow.Date, "Approve event"))).PurchaseOrderId!.Value;
        await new UpsertPurchaseOrderDetailHandler(repository, scope.ContextAccessor)
            .HandleAsync(new UpsertPurchaseOrderDetailRequest(requestId, null, null, "Line", 1, 100, null));
        await new SubmitPurchaseOrderHandler(repository, scope.ContextAccessor, scope.CreateOperationalSecurityService(), scope.AuthRepository)
            .HandleAsync(new SubmitPurchaseOrderRequest(requestId));

        Guid approverSession = await CreateApproverSessionAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, approverUserId, true);
        scope.ContextAccessor.SetCurrent(new RequestContext(scope.TenantId, scope.CompanyId, approverUserId, approverSession, Guid.NewGuid().ToString()));

        ApprovePurchaseOrderResponse approved = await new ApprovePurchaseOrderHandler(repository, scope.ContextAccessor, scope.CreateOperationalSecurityService(), scope.AuthRepository)
            .HandleAsync(new ApprovePurchaseOrderRequest(requestId, "approved"));
        Assert.True(approved.IsSuccess);

        await using SqlConnection connection = new(scope.ConnectionString);
        await connection.OpenAsync();
        await scope.ApplyTenantCompanyContextAsync(connection);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM seguridad.security_event_audit WHERE event_type='PURCHASE_ORDER_APPROVED' AND id_tenant=@t AND id_empresa=@e;";
        command.Parameters.Add(new SqlParameter("@t", SqlDbType.BigInt) { Value = scope.TenantId });
        command.Parameters.Add(new SqlParameter("@e", SqlDbType.BigInt) { Value = scope.CompanyId });
        int count = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.True(count > 0);
    }

    [Fact]
    public async Task PurchaseOrder_DenyByPermission_ShouldBeConsistent()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        AuthorizationCheckResult decision = await scope.CreateAuthorizationEvaluator().EvaluateAsync(
            new AuthorizationCheckRequest(
                "PURCHASE.ORDER.APPROVE",
                true,
                "/api/v1/purchase/orders/10/approve",
                "POST",
                "127.0.0.1",
                "SecureERP.Tests",
                Guid.NewGuid().ToString()));

        Assert.False(decision.IsAllowed);
        Assert.True(decision.ReasonCode is "DENY_DEFAULT" or "DENY_EXPLICIT");
    }

    [Fact]
    public async Task PurchaseOrder_DenyByMfa_ShouldBeConsistent()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.APPROVE");
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: false);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        AuthorizationCheckResult decision = await scope.CreateAuthorizationEvaluator().EvaluateAsync(
            new AuthorizationCheckRequest(
                "PURCHASE.ORDER.APPROVE",
                true,
                "/api/v1/purchase/orders/10/approve",
                "POST",
                "127.0.0.1",
                "SecureERP.Tests",
                Guid.NewGuid().ToString()));

        Assert.False(decision.IsAllowed);
        Assert.Equal("MFA_REQUIRED", decision.ReasonCode);
    }

    [Fact]
    public async Task PurchaseOrder_DenyBySoD_ShouldBeConsistent()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.CREATE");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.UPDATE");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.SUBMIT");
        await EnsurePermissionAllowAsync(scope.ConnectionString, scope.TenantId, scope.CompanyId, scope.UserId, "PURCHASE.ORDER.APPROVE");
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        PurchaseOrderRepository repository = new(scope.ConnectionFactory, scope.SessionContextApplier);
        long requestId = (await new CreatePurchaseOrderHandler(repository, scope.ContextAccessor)
            .HandleAsync(new CreatePurchaseOrderRequest(null, DateTime.UtcNow.Date, "SoD consistency"))).PurchaseOrderId!.Value;
        await new UpsertPurchaseOrderDetailHandler(repository, scope.ContextAccessor)
            .HandleAsync(new UpsertPurchaseOrderDetailRequest(requestId, null, null, "Line", 1, 100, null));
        await new SubmitPurchaseOrderHandler(repository, scope.ContextAccessor, scope.CreateOperationalSecurityService(), scope.AuthRepository)
            .HandleAsync(new SubmitPurchaseOrderRequest(requestId));

        ApprovePurchaseOrderResponse denied = await new ApprovePurchaseOrderHandler(repository, scope.ContextAccessor, scope.CreateOperationalSecurityService(), scope.AuthRepository)
            .HandleAsync(new ApprovePurchaseOrderRequest(requestId, "self approve"));

        Assert.False(denied.IsSuccess);
        Assert.Equal("PURCHASE_ORDER_SOD_DENY", denied.ErrorCode);
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
                VALUES (@codigo,N'PURCHASE',N'GENERIC',@codigo,@codigo,1,1,SYSUTCDATETIME(),NULL);
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
                    NULL,@e,NULL,DATEADD(MINUTE,-5,SYSUTCDATETIME()),NULL,NULL,N'Purchase allow',1,SYSUTCDATETIME(),NULL
                );
            END;
            """,
            ("@u", SqlDbType.BigInt, userId, null),
            ("@t", SqlDbType.BigInt, tenantId, null),
            ("@e", SqlDbType.BigInt, companyId, null),
            ("@codigo", SqlDbType.NVarChar, permissionCode, 300));
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

    private static async Task<long> InsertPurchaseOrderInCompanyAsync(string connectionString, long tenantId, long companyId, long userId)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await SetSessionContextAsync(connection, "id_tenant", tenantId);
        await SetSessionContextAsync(connection, "id_empresa", companyId);
        await SetSessionContextAsync(connection, "id_usuario", userId);
        await SetSessionContextAsync(connection, "es_admin_tenant", 1);

        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC compras.usp_solicitud_crear_borrador @id_unidad_organizativa=NULL,@fecha_solicitud=@f,@observaciones=@o,@creado_por=@u,@creado_utc=@c;";
        cmd.Parameters.Add(new SqlParameter("@f", SqlDbType.DateTime2) { Value = DateTime.UtcNow });
        cmd.Parameters.Add(new SqlParameter("@o", SqlDbType.NVarChar, 1000) { Value = "other company seed" });
        cmd.Parameters.Add(new SqlParameter("@u", SqlDbType.BigInt) { Value = userId });
        cmd.Parameters.Add(new SqlParameter("@c", SqlDbType.DateTime2) { Value = DateTime.UtcNow });
        object? id = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(id);
    }

    private static async Task<long> CreateApproverUserAsync(string connectionString, long tenantId, long companyId)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await SetSessionContextAsync(connection, "id_tenant", tenantId);
        await SetSessionContextAsync(connection, "id_empresa", companyId);
        await SetSessionContextAsync(connection, "es_admin_tenant", 1);

        string login = $"approver_{Guid.NewGuid():N}@secureerp.local";
        string code = $"APR_{Guid.NewGuid():N}"[..20];
        await using SqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO seguridad.usuario
            (
                codigo,login_principal,login_normalizado,nombre,apellido,nombre_mostrar,correo_electronico,correo_normalizado,
                telefono_movil,idioma,zona_horaria,id_estado_usuario,bloqueado_hasta_utc,mfa_habilitado,requiere_cambio_clave,ultimo_acceso_utc,
                activo,creado_por,creado_utc,actualizado_por,actualizado_utc
            )
            OUTPUT INSERTED.id_usuario
            VALUES
            (
                @c,@l,UPPER(@l),N'Approver',N'User',N'Approver User',@l,UPPER(@l),
                NULL,N'es-GT',N'America/Guatemala',1,NULL,1,0,NULL,1,NULL,SYSUTCDATETIME(),NULL,NULL
            );
            """;
        cmd.Parameters.Add(new SqlParameter("@c", SqlDbType.NVarChar, 100) { Value = code });
        cmd.Parameters.Add(new SqlParameter("@l", SqlDbType.NVarChar, 240) { Value = login });
        long userId = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        await ExecAsync(connection, """
            INSERT INTO seguridad.usuario_tenant(id_usuario,id_tenant,es_administrador_tenant,es_cuenta_servicio,activo,creado_utc,actualizado_utc)
            VALUES(@u,@t,0,0,1,SYSUTCDATETIME(),NULL);
            INSERT INTO seguridad.usuario_empresa(id_usuario,id_tenant,id_empresa,es_empresa_predeterminada,puede_operar,fecha_inicio_utc,fecha_fin_utc,activo,creado_utc,actualizado_utc)
            VALUES(@u,@t,@e,1,1,DATEADD(MINUTE,-5,SYSUTCDATETIME()),NULL,1,SYSUTCDATETIME(),NULL);
            """,
            ("@u", SqlDbType.BigInt, userId, null),
            ("@t", SqlDbType.BigInt, tenantId, null),
            ("@e", SqlDbType.BigInt, companyId, null));

        return userId;
    }

    private static async Task<Guid> CreateApproverSessionAsync(string connectionString, long tenantId, long companyId, long userId, bool mfaValidated)
    {
        SqlServerConnectionFactory factory = new(connectionString);
        RequestContextAccessor context = new();
        context.SetCurrent(new RequestContext(tenantId, companyId, userId, null, Guid.NewGuid().ToString()));
        AuthRepository auth = new(factory, new SecureERP.Infrastructure.Persistence.SessionContext.SqlSessionContextApplier(context));
        TokenGenerator tokenGenerator = new();
        Guid sessionId = Guid.NewGuid();
        byte[] tokenHash = tokenGenerator.ComputeSha256($"approver-token-{sessionId}");

        await auth.CreateSessionAsync(
            new UserSessionToCreate(
                sessionId,
                userId,
                tenantId,
                companyId,
                tokenHash,
                "LOCAL",
                mfaValidated,
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(8),
                DateTime.UtcNow,
                "127.0.0.1",
                "SecureERP.Tests"),
            CancellationToken.None);

        return sessionId;
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


