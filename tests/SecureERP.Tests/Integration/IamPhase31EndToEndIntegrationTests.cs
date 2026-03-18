using Microsoft.Data.SqlClient;
using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Domain.Modules.Security;
using System.Data;

namespace SecureERP.Tests.Integration;

public sealed class IamPhase31EndToEndIntegrationTests
{
    [Fact]
    public async Task Login_WithMfaEnabled_ShouldReturnAuthFlow()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        LoginResponse response = await scope.CreateLoginHandler().HandleAsync(new LoginRequest(scope.TenantCode, scope.Login, scope.Password, "127.0.0.1", "SecureERP.Tests"));
        Assert.True(response.IsAuthenticated, $"{response.ErrorCode}:{response.ErrorMessage}");
        Assert.True(response.RequiresMfa);
        Assert.NotNull(response.AuthFlowId);
    }

    [Fact]
    public async Task RequestMfaChallenge_WithAuthFlowWithoutSession_ShouldSucceed()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        LoginResponse login = await scope.CreateLoginHandler().HandleAsync(new LoginRequest(scope.TenantCode, scope.Login, scope.Password, "127.0.0.1", "SecureERP.Tests"));
        RequestMfaChallengeResponse challenge = await scope.CreateRequestMfaChallengeHandler().HandleAsync(new RequestMfaChallengeRequest(login.AuthFlowId, MfaPurpose.Login, MfaChannel.Totp, "LOGIN"));
        Assert.True(challenge.IsSuccess);
        Assert.NotNull(challenge.ChallengeId);
    }

    [Fact]
    public async Task VerifyMfaChallenge_OnAuthFlow_ShouldMarkFlow()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        LoginResponse login = await scope.CreateLoginHandler().HandleAsync(new LoginRequest(scope.TenantCode, scope.Login, scope.Password, "127.0.0.1", "SecureERP.Tests"));
        RequestMfaChallengeResponse challenge = await scope.CreateRequestMfaChallengeHandler().HandleAsync(new RequestMfaChallengeRequest(login.AuthFlowId, MfaPurpose.Login, MfaChannel.Totp, "LOGIN"));
        VerifyMfaChallengeResponse verify = await scope.CreateVerifyMfaChallengeHandler().HandleAsync(new VerifyMfaChallengeRequest(challenge.ChallengeId!.Value, IamTestScope.DeterministicOtp));
        Assert.True(verify.IsVerified);
    }

    [Fact]
    public async Task SelectCompany_AfterMfa_ShouldCreateSession()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        (SelectCompanyResponse select, _) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        Assert.True(select.IsSuccess);
        Assert.NotNull(select.SessionId);
        Assert.False(string.IsNullOrWhiteSpace(select.AccessToken));
    }

    [Fact]
    public async Task ValidateSession_ShouldReturnValid()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        (SelectCompanyResponse select, _) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        ValidateSessionResult session = await scope.CreateValidateSessionHandler().HandleAsync(new ValidateSessionRequest(select.AccessToken!, 30, true));
        Assert.True(session.IsValid);
    }

    [Fact]
    public async Task Authorization_OnProtectedOperation_ShouldAllow()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));
        AuthorizationCheckResult result = await scope.CreateAuthorizationEvaluator().EvaluateAsync(new AuthorizationCheckRequest(scope.PermissionCode, false, "/api/v1/test/protected", "GET", "127.0.0.1", "SecureERP.Tests", Guid.NewGuid().ToString()));
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task Authorization_ExplicitDeny_ShouldWinOverAllow()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        await scope.AddExplicitDenyForPermissionAsync();
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        AuthorizationCheckResult result = await scope.CreateAuthorizationEvaluator().EvaluateAsync(
            new AuthorizationCheckRequest(scope.PermissionCode, false, "/api/v1/test/deny-over-allow", "POST", "127.0.0.1", "SecureERP.Tests", Guid.NewGuid().ToString()));

        Assert.False(result.IsAllowed);
        Assert.Equal("DENY_EXPLICIT", result.ReasonCode);
    }

    [Fact]
    public async Task Authorization_DefaultDeny_ShouldApply_WhenNoEffectivePermission()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        await scope.RemovePermissionExceptionsAsync();
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        AuthorizationCheckResult result = await scope.CreateAuthorizationEvaluator().EvaluateAsync(
            new AuthorizationCheckRequest(scope.PermissionCode, false, "/api/v1/test/default-deny", "GET", "127.0.0.1", "SecureERP.Tests", Guid.NewGuid().ToString()));

        Assert.False(result.IsAllowed);
        Assert.Equal("DENY_DEFAULT", result.ReasonCode);
    }

    [Fact]
    public async Task Authorization_ShouldDeny_WhenMfaRequiredAndSessionNotValidated()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: false);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));
        AuthorizationCheckResult result = await scope.CreateAuthorizationEvaluator().EvaluateAsync(new AuthorizationCheckRequest(scope.PermissionCode, true, "/api/v1/test/sensitive", "POST", "127.0.0.1", "SecureERP.Tests", Guid.NewGuid().ToString()));
        Assert.False(result.IsAllowed);
        Assert.Equal("MFA_REQUIRED", result.ReasonCode);
    }

    [Fact]
    public async Task AuthorizationEvaluation_ShouldPersistAudit()
    {
        await using IamTestScope scope = await IamTestScope.CreateAsync();
        (_, ValidateSessionResult session) = await scope.LoginAndSelectCompanyAsync(withMfa: true);
        scope.ContextAccessor.SetCurrent(new RequestContext(session.TenantId, session.CompanyId, session.UserId, session.SessionId, Guid.NewGuid().ToString()));

        await scope.CreateAuthorizationEvaluator().EvaluateAsync(new AuthorizationCheckRequest(scope.PermissionCode, false, "/api/v1/test/audit", "GET", "127.0.0.1", "SecureERP.Tests", Guid.NewGuid().ToString()));

        await using SqlConnection connection = new(scope.ConnectionString);
        await connection.OpenAsync();
        await scope.ApplyTenantCompanyContextAsync(connection);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM observabilidad.auditoria_autorizacion WHERE id_tenant=@t AND id_usuario=@u AND id_empresa=@e AND codigo_permiso=@p;";
        command.Parameters.Add(new SqlParameter("@t", SqlDbType.BigInt) { Value = scope.TenantId });
        command.Parameters.Add(new SqlParameter("@u", SqlDbType.BigInt) { Value = scope.UserId });
        command.Parameters.Add(new SqlParameter("@e", SqlDbType.BigInt) { Value = scope.CompanyId });
        command.Parameters.Add(new SqlParameter("@p", SqlDbType.NVarChar, 150) { Value = scope.PermissionCode });
        int count = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.True(count > 0);
    }

    [Fact]
    public async Task AuthorizationAuditStoredProcedure_ShouldExist()
    {
        string cs = IamTestScope.ResolveConnectionString() ?? throw new InvalidOperationException("Connection string not configured.");
        await using SqlConnection connection = new(cs);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT OBJECT_ID(N'observabilidad.usp_auditoria_autorizacion_crear', N'P');";
        object? value = await command.ExecuteScalarAsync();
        Assert.NotNull(value);
        Assert.NotEqual(DBNull.Value, value);
    }

    [Fact]
    public async Task RlsPilotPolicy_ShouldCoverSecurityEventAudit()
    {
        string cs = IamTestScope.ResolveConnectionString() ?? throw new InvalidOperationException("Connection string not configured.");
        await using SqlConnection connection = new(cs);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sys.security_policies p INNER JOIN sys.security_predicates pr ON p.object_id = pr.object_id WHERE p.name='RLS_scope_tenant_empresa' AND pr.target_object_id=OBJECT_ID(N'seguridad.security_event_audit') AND pr.predicate_type_desc='FILTER';";
        int count = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.True(count > 0);
    }

    [Fact]
    public async Task RlsPolicy_ShouldExcludeIamCoreSessionAndMfaChallengeTables()
    {
        string cs = IamTestScope.ResolveConnectionString() ?? throw new InvalidOperationException("Connection string not configured.");
        await using SqlConnection connection = new(cs);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                COALESCE(SUM(CASE WHEN pr.target_object_id = OBJECT_ID(N'seguridad.sesion_usuario') THEN 1 ELSE 0 END), 0) AS session_predicates,
                COALESCE(SUM(CASE WHEN pr.target_object_id = OBJECT_ID(N'seguridad.desafio_mfa') THEN 1 ELSE 0 END), 0) AS challenge_predicates
            FROM sys.security_policies p
            INNER JOIN sys.security_predicates pr ON p.object_id = pr.object_id
            WHERE p.name = 'RLS_scope_tenant_empresa'
              AND pr.predicate_type_desc = 'FILTER';
            """;
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(0, reader.GetInt32(reader.GetOrdinal("session_predicates")));
        Assert.Equal(0, reader.GetInt32(reader.GetOrdinal("challenge_predicates")));
    }
}
