using DevExpress.XtraGrid.Columns;
using SecureERP.Desktop.Core.Contracts.Catalog;
using SecureERP.Desktop.Modules.Shared.Pages;

namespace SecureERP.Desktop.Modules.Catalogs.Countries;

public sealed class CountriesListView(ICountriesApiClient countriesApiClient) : ErpListPageBase("Catalogo de Paises")
{
    protected override void ConfigureGrid()
    {
        GridView.Columns.Clear();
        GridView.Columns.Add(new GridColumn { FieldName = "Code", Caption = "Codigo", Visible = true, VisibleIndex = 0, Width = 120 });
        GridView.Columns.Add(new GridColumn { FieldName = "Name", Caption = "Nombre", Visible = true, VisibleIndex = 1, Width = 260 });
        GridView.Columns.Add(new GridColumn { FieldName = "Iso3", Caption = "ISO3", Visible = true, VisibleIndex = 2, Width = 120 });
        GridView.Columns.Add(new GridColumn { FieldName = "IsActive", Caption = "Activo", Visible = true, VisibleIndex = 3, Width = 100 });
        GridView.BestFitColumns();
    }

    protected override async Task LoadDataAsync(CancellationToken cancellationToken = default)
    {
        var countries = await countriesApiClient.GetCountriesAsync(cancellationToken);
        BindData(countries);
    }
}
