namespace SecureERP.Domain.Modules.Security;

public interface IAuthRepository
{
    Task<LoginUserCredential?> GetUserForLoginAsync(
        string tenantCode,
        string identifier,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperableCompany>> GetOperableCompaniesAsync(
        long userId,
        long tenantId,
        CancellationToken cancellationToken = default);

    Task CreateAuthFlowAsync(
        AuthFlowToCreate authFlow,
        CancellationToken cancellationToken = default);

    Task<AuthFlowSnapshot?> GetAuthFlowAsync(
        Guid authFlowId,
        CancellationToken cancellationToken = default);

    Task<bool> MarkAuthFlowAsUsedAsync(
        Guid authFlowId,
        bool mfaValidated,
        CancellationToken cancellationToken = default);

    Task CreateSessionAsync(
        UserSessionToCreate session,
        CancellationToken cancellationToken = default);

    Task<SessionValidationSnapshot?> ValidateSessionByTokenHashAsync(
        byte[] tokenHash,
        int idleTimeoutMinutes,
        bool updateLastActivity,
        CancellationToken cancellationToken = default);

    Task WriteSecurityEventAsync(
        SecurityEventToCreate securityEvent,
        CancellationToken cancellationToken = default);

    Task CreateMfaChallengeAsync(
        MfaChallengeToCreate challenge,
        CancellationToken cancellationToken = default);

    Task<MfaChallengeSnapshot?> GetMfaChallengeAsync(
        Guid challengeId,
        CancellationToken cancellationToken = default);

    Task<bool> IncrementMfaChallengeAttemptAsync(
        Guid challengeId,
        CancellationToken cancellationToken = default);

    Task<bool> MarkMfaChallengeValidatedAsync(
        Guid challengeId,
        CancellationToken cancellationToken = default);

    Task<bool> MarkAuthFlowMfaValidatedAsync(
        Guid authFlowId,
        CancellationToken cancellationToken = default);

    Task<bool> MarkSessionMfaValidatedAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
