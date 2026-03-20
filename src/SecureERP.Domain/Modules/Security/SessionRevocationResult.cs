namespace SecureERP.Domain.Modules.Security;

public sealed record SessionRevocationResult(
    bool IsSuccess,
    string? ErrorCode,
    string? ErrorMessage,
    long? TargetUserId);
