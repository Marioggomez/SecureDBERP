namespace SecureERP.WinForms.Services.Search;

public sealed record SearchQuery(
    string Text,
    string? Filter,
    int Page,
    int PageSize,
    string? SortBy = null,
    bool SortDescending = false,
    string? AdvancedFilter = null);

