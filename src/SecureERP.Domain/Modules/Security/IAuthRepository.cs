namespace SecureERP.Domain.Modules.Security;

public interface IAuthRepository
{
    Task<LoginUserCredential?> GetUserForLoginAsync(
        string tenantCode,
        string identifier,
        CancellationToken cancellationToken = default);

    Task CreateSessionAsync(
        UserSessionToCreate session,
        CancellationToken cancellationToken = default);
}
