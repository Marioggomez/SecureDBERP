using SecureERP.Desktop.Core.Abstractions;

namespace SecureERP.Desktop.Infrastructure.SecureApi.Auth;

public sealed class SessionPermissionService(ISessionContext sessionContext) : IPermissionService
{
    public bool CanAccess(string? permissionKey)
    {
        if (string.IsNullOrWhiteSpace(permissionKey))
        {
            return true;
        }

        return sessionContext.Current?.Permissions.Contains(permissionKey, StringComparer.OrdinalIgnoreCase) == true;
    }
}