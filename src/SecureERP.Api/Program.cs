using SecureERP.Api.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SecureERP.Api.Health;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSecureErpApi(builder.Configuration);

WebApplication app = builder.Build();
app.UseSecureErpApiPipeline();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = HealthResponseWriter.WriteAsync
}).WithTags("System");

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteAsync
}).WithTags("System");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = HealthResponseWriter.WriteAsync
}).WithTags("System");
app.MapControllers();
app.MapGet("/", () => Results.Ok(new
{
    service = "SecureERP.Api",
    status = "running"
})).WithTags("System");

app.Run();

public partial class Program;
