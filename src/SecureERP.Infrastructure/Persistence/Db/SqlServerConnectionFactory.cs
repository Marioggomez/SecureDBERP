using Microsoft.Data.SqlClient;

namespace SecureERP.Infrastructure.Persistence.Db;

public sealed class SqlServerConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlServerConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public SqlConnection CreateConnection() => new(_connectionString);
}
