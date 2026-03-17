using SecureERP.Domain.Modules.Security;

namespace SecureERP.Application.Modules.Security.Abstractions;

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
