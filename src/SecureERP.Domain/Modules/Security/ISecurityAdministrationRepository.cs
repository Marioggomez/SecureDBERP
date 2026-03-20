namespace SecureERP.Domain.Modules.Security;

public interface ISecurityAdministrationRepository
{
    Task<IReadOnlyList<SecurityUserSnapshot>> ListUsersAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<SecurityUserSnapshot?> GetUserByIdAsync(
        long userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SecurityEventSnapshot>> ListSecurityEventsAsync(
        int top,
        string? eventType,
        string? severity,
        string? result,
        CancellationToken cancellationToken = default);

    Task<SessionRevocationResult> RevokeSessionAsync(
        Guid sessionId,
        long revokedByUserId,
        string? reason,
        CancellationToken cancellationToken = default);
}
