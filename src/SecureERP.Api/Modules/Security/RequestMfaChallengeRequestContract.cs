namespace SecureERP.Api.Modules.Security;

public sealed record RequestMfaChallengeRequestContract(
    Guid AuthFlowId,
    short Purpose,
    short Channel,
    string? ActionCode = null);
