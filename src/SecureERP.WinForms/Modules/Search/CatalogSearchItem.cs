namespace SecureERP.WinForms.Modules.Search;

public sealed record CatalogSearchItem(
    int Id,
    string Code,
    string Name,
    string Category,
    string State,
    DateTime UpdatedAt);

