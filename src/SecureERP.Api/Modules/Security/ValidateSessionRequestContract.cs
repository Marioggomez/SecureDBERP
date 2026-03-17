namespace SecureERP.Api.Modules.Security;

public sealed record ValidateSessionRequestContract(
    string AccessToken,
    int IdleTimeoutMinutes = 30,
    bool UpdateLastActivity = true);
