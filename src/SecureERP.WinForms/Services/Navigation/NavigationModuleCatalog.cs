using SecureERP.WinForms.Modules.Home;
using SecureERP.WinForms.Modules.Search;
using SecureERP.WinForms.Modules.System;
using SecureERP.WinForms.Services.Search;
using SecureERP.WinForms.Themes;

namespace SecureERP.WinForms.Services.Navigation;

public sealed class NavigationModuleCatalog : INavigationModule
{
    private readonly IThemePreferenceService _themePreferenceService;

    public NavigationModuleCatalog(IThemePreferenceService themePreferenceService)
    {
        _themePreferenceService = themePreferenceService;
    }

    public IReadOnlyList<NavigationItemDefinition> BuildItems()
    {
        return
        [
            new NavigationItemDefinition(
                Key: "HOME.DASHBOARD",
                Caption: "Inicio",
                Group: "General",
                CreateView: () => new HomeDashboardForm(),
                OpenOnStartup: true),

            new NavigationItemDefinition(
                Key: "SYSTEM.APPEARANCE",
                Caption: "Apariencia",
                Group: "General",
                CreateView: () => new AppearanceSettingsForm(_themePreferenceService)),

            new NavigationItemDefinition(
                Key: "SEARCH.CATALOG",
                Caption: "Búsqueda Global",
                Group: "Búsquedas",
                CreateView: () => new CatalogSearchForm(new MockCatalogSearchProvider()))
        ];
    }
}
