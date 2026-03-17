using SecureERP.Application.DependencyInjection;
using SecureERP.Infrastructure.DependencyInjection;

namespace SecureERP.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecureErpApi(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("SecureERP");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'SecureERP' is required.");
        }

        services.AddHttpContextAccessor();
        services.AddControllers();
        services.AddHealthChecks();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddSecureErpApplication();
        services.AddSecureErpInfrastructure(connectionString);

        return services;
    }
}
