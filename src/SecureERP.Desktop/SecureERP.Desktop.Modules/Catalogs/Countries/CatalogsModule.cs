using Microsoft.Extensions.DependencyInjection;
using SecureERP.Desktop.Core.Abstractions;
using SecureERP.Desktop.Core.Models;
using SecureERP.Desktop.Modules.Shared.Pages;

namespace SecureERP.Desktop.Modules.Catalogs.Countries;

public sealed class CatalogsModule : IDesktopModule
{
    public string ModuleKey => "catalogs";

    public string ModuleCaption => "Catalogos";

    public IEnumerable<NavigationItemDefinition> GetNavigationItems()
    {
        yield return new NavigationItemDefinition(
            ModuleKey,
            "home.dashboard",
            "Inicio",
            typeof(HomeDashboardView),
            PermissionKey: null,
            Singleton: true);

        yield return new NavigationItemDefinition(
            ModuleKey,
            "catalogs.countries",
            "Paises",
            typeof(CountriesListView),
            PermissionKey: "catalogs.countries.read",
            Singleton: true);
    }

    public void Register(IServiceCollection services)
    {
        services.AddTransient<HomeDashboardView>();
        services.AddTransient<CountriesListView>();
        services.AddTransient<CountryEditDialog>();
    }
}