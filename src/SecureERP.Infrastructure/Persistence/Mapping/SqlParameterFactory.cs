using Microsoft.Data.SqlClient;
using System.Data;

namespace SecureERP.Infrastructure.Persistence.Mapping;

public static class SqlParameterFactory
{
    public static SqlParameter UniqueIdentifier(string name, Guid? value)
        => Create(name, SqlDbType.UniqueIdentifier, value ?? (object)DBNull.Value);

    public static SqlParameter Int(string name, int? value)
        => Create(name, SqlDbType.Int, value ?? (object)DBNull.Value);

    public static SqlParameter SmallInt(string name, short? value)
        => Create(name, SqlDbType.SmallInt, value ?? (object)DBNull.Value);

    public static SqlParameter BigInt(string name, long? value)
        => Create(name, SqlDbType.BigInt, value ?? (object)DBNull.Value);

    public static SqlParameter Bit(string name, bool? value)
        => Create(name, SqlDbType.Bit, value ?? (object)DBNull.Value);

    public static SqlParameter DateTime2(string name, DateTime? value)
        => Create(name, SqlDbType.DateTime2, value ?? (object)DBNull.Value);

    public static SqlParameter Decimal(string name, decimal? value, byte precision = 18, byte scale = 2)
    {
        SqlParameter parameter = Create(name, SqlDbType.Decimal, value ?? (object)DBNull.Value);
        parameter.Precision = precision;
        parameter.Scale = scale;
        return parameter;
    }

    public static SqlParameter NVarChar(string name, string? value, int size = 4000)
    {
        SqlParameter parameter = new(name, SqlDbType.NVarChar, size)
        {
            Value = string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim()
        };
        return parameter;
    }

    public static SqlParameter VarChar(string name, string? value, int size = 8000)
    {
        SqlParameter parameter = new(name, SqlDbType.VarChar, size)
        {
            Value = string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim()
        };
        return parameter;
    }

    public static SqlParameter VarBinary(string name, byte[]? value, int size)
    {
        SqlParameter parameter = new(name, SqlDbType.VarBinary, size)
        {
            Value = value is null ? DBNull.Value : value
        };
        return parameter;
    }

    public static SqlParameter Variant(string name, object? value)
        => Create(name, SqlDbType.Variant, value ?? DBNull.Value);

    private static SqlParameter Create(string name, SqlDbType type, object value)
    {
        SqlParameter parameter = new(name, type)
        {
            Value = value
        };
        return parameter;
    }
}
