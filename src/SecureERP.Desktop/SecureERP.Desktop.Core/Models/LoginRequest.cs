namespace SecureERP.Desktop.Core.Models;

public sealed record LoginRequest(string Username, string Password, string? Tenant = null);