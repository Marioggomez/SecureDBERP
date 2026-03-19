namespace SecureERP.WinForms.Services.Search;

public interface ISearchDataProvider<T>
{
    Task<SearchPageResult<T>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default);
}

