using Microsoft.Extensions.DependencyInjection;
using SecureERP.Desktop.Infrastructure.SecureApi.DependencyInjection;
using SecureERP.Desktop.Modules.Bootstrap;
using SecureERP.Desktop.Shell.Shell;

namespace SecureERP.Desktop.Shell.Bootstrap;

public static class DesktopServiceConfigurator
{
    public static IServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddSecureErpSecureApi(options =>
        {
            var configuredUrl = Environment.GetEnvironmentVariable("SECUREERP_API_BASEURL");
            if (Uri.TryCreate(configuredUrl, UriKind.Absolute, out var baseUri))
            {
                options.BaseUri = baseUri;
            }
        });

        services.AddDesktopModules();
        services.AddTransient<LoginForm>();
        services.AddTransient<MainShellForm>();

        return services;
    }
}