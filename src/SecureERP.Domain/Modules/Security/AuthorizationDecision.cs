namespace SecureERP.Domain.Modules.Security;

public sealed record AuthorizationDecision(
    bool IsAllowed,
    string ReasonCode,
    string ResolutionSource);
