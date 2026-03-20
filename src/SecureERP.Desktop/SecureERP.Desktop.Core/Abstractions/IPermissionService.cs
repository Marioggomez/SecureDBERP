namespace SecureERP.Desktop.Core.Abstractions;

public interface IPermissionService
{
    bool CanAccess(string? permissionKey);
}