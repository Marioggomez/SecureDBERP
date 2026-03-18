using SecureERP.Application.DependencyInjection;
using SecureERP.Api.Health;
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
        services.AddHealthChecks()
            .AddCheck("live", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<SqlServerReadyHealthCheck>("sqlserver", tags: ["ready"]);
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddSecureErpApplication();
        services.AddSecureErpInfrastructure(connectionString);

        return services;
    }
}
