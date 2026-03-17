namespace SecureERP.Api.Modules.Security;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : Attribute
{
    public RequirePermissionAttribute(string permissionCode, bool requiresMfa = false)
    {
        PermissionCode = permissionCode;
        RequiresMfa = requiresMfa;
    }

    public string PermissionCode { get; }

    public bool RequiresMfa { get; }
}
