namespace SecureERP.Api.Modules.Security;

public sealed record ValidateSessionRequestContract(
    int IdleTimeoutMinutes = 30,
    bool UpdateLastActivity = true);
