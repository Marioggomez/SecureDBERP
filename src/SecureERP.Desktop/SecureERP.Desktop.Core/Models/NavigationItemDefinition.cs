namespace SecureERP.Desktop.Core.Models;

public sealed record NavigationItemDefinition(
    string ModuleKey,
    string ItemKey,
    string Caption,
    Type ViewType,
    string? PermissionKey = null,
    bool Singleton = true);