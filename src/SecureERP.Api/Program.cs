using SecureERP.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSecureErpApi(builder.Configuration);

WebApplication app = builder.Build();
app.UseSecureErpApiPipeline();

app.MapHealthChecks("/health").WithTags("System");
app.MapControllers();
app.MapGet("/", () => Results.Ok(new
{
    service = "SecureERP.Api",
    status = "running"
})).WithTags("System");

app.Run();

public partial class Program;
