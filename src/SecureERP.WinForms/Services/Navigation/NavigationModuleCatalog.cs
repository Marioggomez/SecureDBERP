using SecureERP.WinForms.Modules.Home;
using SecureERP.WinForms.Modules.Search;
using SecureERP.WinForms.Modules.System;
using SecureERP.WinForms.Services.Search;
using SecureERP.WinForms.Services.Workspace;
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
                CreateView: () => new WorkspaceHostForm(new HomeDashboardPage()),
                OpenOnStartup: true),

            new NavigationItemDefinition(
                Key: "SYSTEM.APPEARANCE",
                Caption: "Apariencia",
                Group: "General",
                CreateView: () => new WorkspaceHostForm(new AppearanceWorkspacePage(_themePreferenceService))),

            new NavigationItemDefinition(
                Key: "SEARCH.CATALOG",
                Caption: "Búsqueda Global",
                Group: "Búsquedas",
                CreateView: () => new WorkspaceHostForm(new CatalogSearchPage(new MockCatalogSearchProvider())))
        ];
    }
}
