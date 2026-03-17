using SecureERP.Application.Modules.Security.DTOs;

namespace SecureERP.Application.Modules.Security.Abstractions;

public interface IVerifyMfaChallengeHandler
{
    Task<VerifyMfaChallengeResponse> HandleAsync(VerifyMfaChallengeRequest request, CancellationToken cancellationToken = default);
}
