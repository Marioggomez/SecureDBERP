using SecureERP.Domain.Modules.Security;

namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record RequestMfaChallengeRequest(
    Guid AuthFlowId,
    MfaPurpose Purpose,
    MfaChannel Channel,
    string? ActionCode = null);
