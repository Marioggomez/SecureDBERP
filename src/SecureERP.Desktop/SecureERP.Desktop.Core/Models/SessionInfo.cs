namespace SecureERP.Desktop.Core.Models;

public sealed record SessionInfo(
    string UserName,
    string AccessToken,
    string? RefreshToken,
    IReadOnlyCollection<string> Permissions,
    DateTimeOffset ExpiresAtUtc);