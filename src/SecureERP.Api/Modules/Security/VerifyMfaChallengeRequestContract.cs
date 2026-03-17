namespace SecureERP.Api.Modules.Security;

public sealed record VerifyMfaChallengeRequestContract(
    Guid ChallengeId,
    string OtpCode);
