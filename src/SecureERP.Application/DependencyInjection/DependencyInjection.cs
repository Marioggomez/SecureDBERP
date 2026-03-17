using Microsoft.Extensions.DependencyInjection;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.Commands;

namespace SecureERP.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddSecureErpApplication(this IServiceCollection services)
    {
        services.AddScoped<ILoginHandler, LoginHandler>();
        return services;
    }
}
