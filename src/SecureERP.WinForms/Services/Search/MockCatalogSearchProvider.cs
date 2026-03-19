using SecureERP.WinForms.Modules.Search;

namespace SecureERP.WinForms.Services.Search;

public sealed class MockCatalogSearchProvider : ISearchDataProvider<CatalogSearchItem>
{
    private static readonly string[] States = ["Activo", "Suspendido", "Borrador"];

    private readonly List<CatalogSearchItem> _dataset;

    public MockCatalogSearchProvider()
    {
        _dataset = Enumerable.Range(1, 500)
            .Select(index => new CatalogSearchItem(
                Id: index,
                Code: $"CAT-{index:00000}",
                Name: $"Elemento de catálogo {index}",
                Category: index % 2 == 0 ? "Maestro" : "Operativo",
                State: States[index % States.Length],
                UpdatedAt: DateTime.UtcNow.AddMinutes(-index)))
            .ToList();
    }

    public async Task<SearchPageResult<CatalogSearchItem>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        await Task.Delay(250, cancellationToken);

        IEnumerable<CatalogSearchItem> q = _dataset;

        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            string normalized = query.Text.Trim();
            q = q.Where(item =>
                item.Code.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                || item.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                || item.Category.Contains(normalized, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Filter) && !string.Equals(query.Filter, "Todos", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(item => string.Equals(item.State, query.Filter, StringComparison.OrdinalIgnoreCase));
        }

        int total = q.Count();
        int page = Math.Max(1, query.Page);
        int pageSize = Math.Max(5, query.PageSize);

        IReadOnlyList<CatalogSearchItem> pageItems = q
            .OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new SearchPageResult<CatalogSearchItem>(pageItems, total, page, pageSize);
    }
}

