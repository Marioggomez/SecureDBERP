using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using SecureERP.WinForms.Search;
using SecureERP.WinForms.Services.Search;

namespace SecureERP.WinForms.Modules.Search;

/// <summary>
/// Página de búsqueda ERP gobernada por el shell y la Ribbon principal.
/// </summary>
public sealed class CatalogSearchPage : SearchWorkspacePageBase<CatalogSearchItem>
{
    public override string PageKey => "SEARCH.CATALOG";
    public override string Title => "Búsqueda Global";

    private readonly ISearchDataProvider<CatalogSearchItem> _provider;

    public CatalogSearchPage(ISearchDataProvider<CatalogSearchItem> provider)
    {
        _provider = provider;
    }

    protected override IReadOnlyList<string> GetQuickFilterOptions() => new[] { "Todos", "Activo", "Suspendido", "Borrador" };

    protected override IReadOnlyList<SortOption> GetSortOptions()
        => new[]
        {
            new SortOption(nameof(CatalogSearchItem.Code), "Código"),
            new SortOption(nameof(CatalogSearchItem.Name), "Nombre"),
            new SortOption(nameof(CatalogSearchItem.Category), "Categoría"),
            new SortOption(nameof(CatalogSearchItem.State), "Estado"),
            new SortOption(nameof(CatalogSearchItem.UpdatedAt), "Actualizado"),
        };

    protected override void ConfigureGrid(GridView view)
    {
        view.Columns.Clear();
        view.Columns.AddVisible(nameof(CatalogSearchItem.Code), "Código").Width = 120;
        view.Columns.AddVisible(nameof(CatalogSearchItem.Name), "Nombre").Width = 260;
        view.Columns.AddVisible(nameof(CatalogSearchItem.Category), "Categoría").Width = 150;
        view.Columns.AddVisible(nameof(CatalogSearchItem.State), "Estado").Width = 110;

        GridColumn updatedAt = view.Columns.AddVisible(nameof(CatalogSearchItem.UpdatedAt), "Actualizado UTC");
        updatedAt.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
        updatedAt.DisplayFormat.FormatString = "yyyy-MM-dd HH:mm";
        updatedAt.Width = 170;
    }

    protected override async Task<SearchPageResult<CatalogSearchItem>> ExecuteSearchCoreAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        return await _provider.SearchAsync(query, cancellationToken);
    }

    protected override string? ResolveContextEntityType(CatalogSearchItem? item)
        => item is null ? null : "CATALOG_ITEM";

    protected override long? ResolveContextEntityId(CatalogSearchItem? item)
        => item?.Id;

    protected override void OnSelectionChanged(CatalogSearchItem? item)
    {
        if (item is null)
        {
            RelatedSidebar.BindContext(null, null);
            return;
        }

        RelatedSidebar.BindContext("CATALOG_ITEM", item.Id);
        RelatedSidebar.BindDocuments(new[]
        {
            $"Ficha {item.Code}.pdf",
            $"Contrato {item.Code}.docx"
        });
        RelatedSidebar.BindTags(new[] { "Operativo", item.State });
        RelatedSidebar.NotesText = $"Notas de {item.Name}";
    }

    protected override void OnOpenDetail(CatalogSearchItem item)
    {
        XtraMessageBox.Show(this, $"Abrir detalle: {item.Code} - {item.Name}", "Detalle", MessageBoxButtons.OK);
    }
}
