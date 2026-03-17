using System.Text.RegularExpressions;

namespace SecureERP.Infrastructure.Persistence.Db;

internal static partial class StoredProcedureName
{
    [GeneratedRegex("^[A-Za-z0-9_]+$", RegexOptions.CultureInvariant)]
    private static partial Regex NameRegex();

    public static string Build(string schema, string procedureName)
    {
        if (string.IsNullOrWhiteSpace(schema) || !NameRegex().IsMatch(schema))
        {
            throw new ArgumentException("Invalid SQL schema name.", nameof(schema));
        }

        if (string.IsNullOrWhiteSpace(procedureName) || !NameRegex().IsMatch(procedureName))
        {
            throw new ArgumentException("Invalid stored procedure name.", nameof(procedureName));
        }

        return $"[{schema}].[{procedureName}]";
    }
}
