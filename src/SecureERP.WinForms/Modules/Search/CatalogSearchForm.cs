using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using SecureERP.WinForms.Search;
using SecureERP.WinForms.Services.Search;

namespace SecureERP.WinForms.Modules.Search;

public sealed class CatalogSearchForm : SearchTemplateFormBase<CatalogSearchItem>
{
    private readonly ISearchDataProvider<CatalogSearchItem> _provider;

    public CatalogSearchForm(ISearchDataProvider<CatalogSearchItem> provider)
        : base("Búsqueda Global")
    {
        _provider = provider;
    }

    protected override Task<SearchPageResult<CatalogSearchItem>> ExecuteSearchAsync(SearchQuery query, CancellationToken cancellationToken)
        => _provider.SearchAsync(query, cancellationToken);

    protected override void ConfigureGrid(GridView view)
    {
        view.Columns.Clear();
        view.Columns.AddVisible(nameof(CatalogSearchItem.Code), "Código").Width = 120;
        view.Columns.AddVisible(nameof(CatalogSearchItem.Name), "Nombre").Width = 280;
        view.Columns.AddVisible(nameof(CatalogSearchItem.Category), "Categoría").Width = 160;
        view.Columns.AddVisible(nameof(CatalogSearchItem.State), "Estado").Width = 110;

        GridColumn updatedAt = view.Columns.AddVisible(nameof(CatalogSearchItem.UpdatedAt), "Actualizado UTC");
        updatedAt.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
        updatedAt.DisplayFormat.FormatString = "yyyy-MM-dd HH:mm";
        updatedAt.Width = 170;

        view.BestFitColumns();
    }
}
