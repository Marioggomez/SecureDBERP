using Microsoft.Extensions.DependencyInjection;
using SecureERP.Desktop.Core.Abstractions;
using SecureERP.Desktop.Core.Contracts.Catalog;
using SecureERP.Desktop.Infrastructure.SecureApi.Auth;
using SecureERP.Desktop.Infrastructure.SecureApi.Catalog;
using SecureERP.Desktop.Infrastructure.SecureApi.Configuration;

namespace SecureERP.Desktop.Infrastructure.SecureApi.DependencyInjection;

public static class SecureApiServiceCollectionExtensions
{
    public static IServiceCollection AddSecureErpSecureApi(
        this IServiceCollection services,
        Action<SecureErpApiOptions>? configure = null)
    {
        var options = new SecureErpApiOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton(_ => new HttpClient
        {
            BaseAddress = options.BaseUri,
            Timeout = options.Timeout
        });

        services.AddSingleton<ISessionContext, InMemorySessionContext>();
        services.AddSingleton<IPermissionService, SessionPermissionService>();
        services.AddTransient<IAuthenticationService, SecureErpAuthenticationService>();
        services.AddTransient<ICountriesApiClient, SecureErpCountriesApiClient>();

        return services;
    }
}
