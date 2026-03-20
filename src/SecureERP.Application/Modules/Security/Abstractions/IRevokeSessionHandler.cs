using SecureERP.Application.Modules.Security.DTOs;

namespace SecureERP.Application.Modules.Security.Abstractions;

public interface IRevokeSessionHandler
{
    Task<RevokeSessionResponse> HandleAsync(
        RevokeSessionRequest request,
        CancellationToken cancellationToken = default);
}
