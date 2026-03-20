using SecureERP.Application.Abstractions.Context;
using SecureERP.Application.Modules.Security.Commands;
using SecureERP.Application.Modules.Security.DTOs;
using SecureERP.Application.Modules.Security.Queries;
using SecureERP.Domain.Modules.Security;
using SecureERP.Infrastructure.Persistence.SessionContext;

namespace SecureERP.Tests.Security;

public sealed class SecurityAdministrationHandlersTests
{
    [Fact]
    public async Task RevokeSessionHandler_ShouldRequireSecurityContext()
    {
        FakeSecurityAdministrationRepository repository = new();
        FakeAuthRepository authRepository = new();
        RequestContextAccessor requestContextAccessor = new();
        RevokeSessionHandler handler = new(repository, authRepository, requestContextAccessor);

        RevokeSessionResponse result = await handler.HandleAsync(
            new RevokeSessionRequest(Guid.NewGuid(), "manual revoke", "127.0.0.1", "tests"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("SESSION_CONTEXT_REQUIRED", result.ErrorCode);
    }

    [Fact]
    public async Task RevokeSessionHandler_ShouldWriteSecurityEvent_WhenRevocationSucceeds()
    {
        FakeSecurityAdministrationRepository repository = new()
        {
            RevokeSessionResult = new SessionRevocationResult(true, null, null, 99)
        };
        FakeAuthRepository authRepository = new();
        RequestContextAccessor requestContextAccessor = new();
        Guid actorSessionId = Guid.NewGuid();
        requestContextAccessor.SetCurrent(new RequestContext(1, 10, 20, actorSessionId, Guid.NewGuid().ToString()));
        RevokeSessionHandler handler = new(repository, authRepository, requestContextAccessor);

        RevokeSessionResponse result = await handler.HandleAsync(
            new RevokeSessionRequest(Guid.NewGuid(), "security admin action", "127.0.0.1", "tests"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(authRepository.LastSecurityEvent);
        Assert.Equal("AUTH_SESSION_REVOKED", authRepository.LastSecurityEvent!.EventType);
        Assert.Equal(actorSessionId, authRepository.LastSecurityEvent.SessionId);
    }

    [Fact]
    public async Task ListSecurityUsersHandler_ShouldMapRepositoryRows()
    {
        FakeSecurityAdministrationRepository repository = new();
        repository.Users.Add(new SecurityUserSnapshot(
            7,
            "USR.ADMIN",
            "admin@secureerp.local",
            "SecureERP Admin",
            "admin@secureerp.local",
            true,
            true,
            true,
            true,
            5,
            true,
            true,
            DateTime.UtcNow.AddDays(-1),
            null,
            null,
            DateTime.UtcNow));
        ListSecurityUsersHandler handler = new(repository);

        IReadOnlyList<SecurityUserDto> result = await handler.HandleAsync(
            new ListSecurityUsersRequest("admin", true),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("USR.ADMIN", result[0].Code);
        Assert.True(result[0].IsTenantAdministrator);
    }

    [Fact]
    public async Task ListSecurityEventsHandler_ShouldClampTopAndMapRepositoryRows()
    {
        FakeSecurityAdministrationRepository repository = new();
        repository.SecurityEvents.Add(new SecurityEventSnapshot(
            1,
            DateTime.UtcNow,
            "AUTH_LOGIN_SUCCESS",
            "INFO",
            "OK",
            "detail",
            1,
            2,
            3,
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            "127.0.0.1",
            "tests"));
        ListSecurityEventsHandler handler = new(repository);

        IReadOnlyList<SecurityEventDto> result = await handler.HandleAsync(
            new ListSecurityEventsRequest(999, "AUTH_LOGIN_SUCCESS", null, null),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(500, repository.LastRequestedTop);
        Assert.Equal("AUTH_LOGIN_SUCCESS", result[0].EventType);
    }

    private sealed class FakeSecurityAdministrationRepository : ISecurityAdministrationRepository
    {
        public List<SecurityUserSnapshot> Users { get; } = [];
        public List<SecurityEventSnapshot> SecurityEvents { get; } = [];
        public SessionRevocationResult RevokeSessionResult { get; set; }
            = new(false, "AUTH_SESSION_NOT_FOUND", "not found", null);
        public int LastRequestedTop { get; private set; }

        public Task<IReadOnlyList<SecurityUserSnapshot>> ListUsersAsync(string? search, bool activeOnly, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SecurityUserSnapshot>>(Users);

        public Task<SecurityUserSnapshot?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.FirstOrDefault(u => u.UserId == userId));

        public Task<IReadOnlyList<SecurityEventSnapshot>> ListSecurityEventsAsync(int top, string? eventType, string? severity, string? result, CancellationToken cancellationToken = default)
        {
            LastRequestedTop = top;
            return Task.FromResult<IReadOnlyList<SecurityEventSnapshot>>(SecurityEvents);
        }

        public Task<SessionRevocationResult> RevokeSessionAsync(Guid sessionId, long revokedByUserId, string? reason, CancellationToken cancellationToken = default)
            => Task.FromResult(RevokeSessionResult);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public SecurityEventToCreate? LastSecurityEvent { get; private set; }

        public Task<LoginUserCredential?> GetUserForLoginAsync(string tenantCode, string identifier, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<OperableCompany>> GetOperableCompaniesAsync(long userId, long tenantId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task CreateAuthFlowAsync(AuthFlowToCreate authFlow, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<AuthFlowSnapshot?> GetAuthFlowAsync(Guid authFlowId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> MarkAuthFlowAsUsedAsync(Guid authFlowId, bool mfaValidated, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task CreateSessionAsync(UserSessionToCreate session, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<SessionValidationSnapshot?> ValidateSessionByTokenHashAsync(byte[] tokenHash, int idleTimeoutMinutes, bool updateLastActivity, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task WriteSecurityEventAsync(SecurityEventToCreate securityEvent, CancellationToken cancellationToken = default)
        {
            LastSecurityEvent = securityEvent;
            return Task.CompletedTask;
        }

        public Task CreateMfaChallengeAsync(MfaChallengeToCreate challenge, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<MfaChallengeSnapshot?> GetMfaChallengeAsync(Guid challengeId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> IncrementMfaChallengeAttemptAsync(Guid challengeId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> MarkMfaChallengeValidatedAsync(Guid challengeId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> MarkAuthFlowMfaValidatedAsync(Guid authFlowId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> MarkSessionMfaValidatedAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
