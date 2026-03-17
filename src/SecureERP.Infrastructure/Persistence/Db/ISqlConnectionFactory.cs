using Microsoft.Data.SqlClient;

namespace SecureERP.Infrastructure.Persistence.Db;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}
