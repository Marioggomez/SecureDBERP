using Microsoft.Extensions.DependencyInjection;
using SecureERP.Application.Modules.Organization.Abstractions;
using SecureERP.Application.Modules.Organization.Commands;
using SecureERP.Application.Modules.Organization.Queries;
using SecureERP.Application.Modules.Security.Abstractions;
using SecureERP.Application.Modules.Security.Commands;
using SecureERP.Application.Modules.Security.Queries;
using SecureERP.Application.Modules.Security.Services;
using SecureERP.Application.Modules.Workflow.Abstractions;
using SecureERP.Application.Modules.Workflow.Commands;
using SecureERP.Application.Modules.Workflow.Queries;

namespace SecureERP.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddSecureErpApplication(this IServiceCollection services)
    {
        services.AddScoped<ILoginHandler, LoginHandler>();
        services.AddScoped<ISelectCompanyHandler, SelectCompanyHandler>();
        services.AddScoped<IValidateSessionHandler, ValidateSessionHandler>();
        services.AddScoped<IRequestMfaChallengeHandler, RequestMfaChallengeHandler>();
        services.AddScoped<IVerifyMfaChallengeHandler, VerifyMfaChallengeHandler>();
        services.AddScoped<IAuthorizationEvaluator, AuthorizationEvaluator>();

        services.AddScoped<IListOrganizationUnitsHandler, ListOrganizationUnitsHandler>();
        services.AddScoped<ICreateOrganizationUnitHandler, CreateOrganizationUnitHandler>();
        services.AddScoped<IListApprovalInstancesHandler, ListApprovalInstancesHandler>();
        services.AddScoped<ICreateApprovalInstanceHandler, CreateApprovalInstanceHandler>();
        return services;
    }
}
