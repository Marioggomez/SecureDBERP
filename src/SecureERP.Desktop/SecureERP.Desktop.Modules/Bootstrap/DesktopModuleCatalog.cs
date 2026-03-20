using Microsoft.Extensions.DependencyInjection;
using SecureERP.Desktop.Core.Abstractions;
using SecureERP.Desktop.Modules.Catalogs.Countries;

namespace SecureERP.Desktop.Modules.Bootstrap;

public static class DesktopModuleCatalog
{
    public static IReadOnlyCollection<IDesktopModule> BuildModules() =>
    [
        new CatalogsModule()
    ];

    public static IServiceCollection AddDesktopModules(this IServiceCollection services)
    {
        var modules = BuildModules();

        foreach (var module in modules)
        {
            module.Register(services);
        }

        services.AddSingleton<IReadOnlyCollection<IDesktopModule>>(modules);
        return services;
    }
}
