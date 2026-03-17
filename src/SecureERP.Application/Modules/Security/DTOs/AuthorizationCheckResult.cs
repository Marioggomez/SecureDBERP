namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record AuthorizationCheckResult(
    bool IsAllowed,
    string ReasonCode,
    string ResolutionSource);
