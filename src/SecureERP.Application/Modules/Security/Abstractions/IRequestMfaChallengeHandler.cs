using SecureERP.Application.Modules.Security.DTOs;

namespace SecureERP.Application.Modules.Security.Abstractions;

public interface IRequestMfaChallengeHandler
{
    Task<RequestMfaChallengeResponse> HandleAsync(RequestMfaChallengeRequest request, CancellationToken cancellationToken = default);
}
