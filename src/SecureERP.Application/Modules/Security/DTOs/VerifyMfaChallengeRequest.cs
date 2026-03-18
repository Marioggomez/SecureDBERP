namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record VerifyMfaChallengeRequest(
    Guid ChallengeId,
    string OtpCode,
    string? IpAddress = null,
    string? UserAgent = null);
