using Microsoft.Data.SqlClient;
using SecureERP.Infrastructure.Persistence.Mapping;
using System.Data;

namespace SecureERP.Tests.Integration;

public sealed class SqlParameterFactoryTests
{
    [Fact]
    public void UniqueIdentifier_ShouldCreateTypedParameter()
    {
        Guid value = Guid.NewGuid();

        SqlParameter parameter = SqlParameterFactory.UniqueIdentifier("@id_usuario", value);

        Assert.Equal("@id_usuario", parameter.ParameterName);
        Assert.Equal(SqlDbType.UniqueIdentifier, parameter.SqlDbType);
        Assert.Equal(value, parameter.Value);
    }
}
