using SecureERP.Api.Middleware;

namespace SecureERP.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseSecureErpApiPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<SecurityContextMiddleware>();
        app.UseHttpsRedirection();

        return app;
    }
}
