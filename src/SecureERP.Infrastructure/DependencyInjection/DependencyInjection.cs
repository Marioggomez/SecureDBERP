using Microsoft.Extensions.DependencyInjection;
using SecureERP.Application.Abstractions.Context;
using SecureERP.Domain.Modules.Organization;
using SecureERP.Domain.Modules.Purchase;
using SecureERP.Domain.Modules.Security;
using SecureERP.Domain.Modules.Workflow;
using SecureERP.Infrastructure.Auditing;
using SecureERP.Infrastructure.Business;
using SecureERP.Infrastructure.Logging;
using SecureERP.Infrastructure.Persistence.Commands;
using SecureERP.Infrastructure.Persistence.Db;
using SecureERP.Infrastructure.Persistence.Queries;
using SecureERP.Infrastructure.Persistence.SessionContext;
using SecureERP.Infrastructure.Security;
using SecureERP.Infrastructure.Serialization;
using SecureERP.Infrastructure.Time;

namespace SecureERP.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddSecureErpInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlServerConnectionFactory(connectionString));

        services.AddScoped<IRequestContextAccessor, RequestContextAccessor>();
        services.AddScoped<ISqlSessionContextApplier, SqlSessionContextApplier>();

        services.AddScoped<IStoredProcedureCommandExecutor, StoredProcedureCommandExecutor>();
        services.AddScoped<IStoredProcedureQueryExecutor, StoredProcedureQueryExecutor>();
        services.AddScoped<BusinessPilotRepository>();
        services.AddScoped<IOrganizationPilotRepository>(provider => provider.GetRequiredService<BusinessPilotRepository>());
        services.AddScoped<IWorkflowPilotRepository>(provider => provider.GetRequiredService<BusinessPilotRepository>());
        services.AddScoped<IPurchaseRequestRepository, PurchaseRequestRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthorizationRepository, AuthorizationRepository>();
        services.AddScoped<IOperationalSecurityRepository, OperationalSecurityRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenGenerator, TokenGenerator>();
        services.AddSingleton<IMfaCodeService, MfaCodeService>();

        services.AddScoped<IAuditTrailWriter, NoOpAuditTrailWriter>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IJsonSerializer, SystemTextJsonSerializer>();
        services.AddScoped(typeof(IApplicationLogger<>), typeof(ApplicationLogger<>));

        return services;
    }
}
