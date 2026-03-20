namespace SecureERP.Application.Modules.Security.DTOs;

public sealed record ListSecurityUsersRequest(
    string? Search,
    bool ActiveOnly);
