using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Data;

namespace SecureERP.Api.Health;

public sealed class SqlServerReadyHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public SqlServerReadyHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        string? connectionString = _configuration.GetConnectionString("SecureERP");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("Connection string 'SecureERP' is not configured.");
        }

        try
        {
            await using SqlConnection connection = new(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using SqlCommand command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = "SELECT 1;";
            object? result = await command.ExecuteScalarAsync(cancellationToken);

            return Convert.ToInt32(result) == 1
                ? HealthCheckResult.Healthy("SQL Server reachable.")
                : HealthCheckResult.Unhealthy("SQL Server readiness probe failed.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server readiness probe failed.", ex);
        }
    }
}
