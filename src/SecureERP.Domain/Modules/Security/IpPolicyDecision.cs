namespace SecureERP.Domain.Modules.Security;

public sealed record IpPolicyDecision(
    bool IsAllowed,
    string ReasonCode);
