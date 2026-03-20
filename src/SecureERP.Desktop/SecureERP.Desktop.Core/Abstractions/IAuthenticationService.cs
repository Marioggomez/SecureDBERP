using SecureERP.Desktop.Core.Models;

namespace SecureERP.Desktop.Core.Abstractions;

public interface IAuthenticationService
{
    Task<SessionInfo> SignInAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);
}