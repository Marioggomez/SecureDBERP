namespace SecureERP.WinForms.Services.Search;

public sealed record SearchPageResult<T>(
    IReadOnlyList<T> Items,
    int TotalRecords,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling((double)TotalRecords / PageSize));
}

