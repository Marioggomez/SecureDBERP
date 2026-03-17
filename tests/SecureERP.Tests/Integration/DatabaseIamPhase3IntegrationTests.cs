using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace SecureERP.Tests.Integration;

public sealed class DatabaseIamPhase3IntegrationTests
{
    [Fact]
    public async Task RlsPolicy_ShouldContainSecurityEventAuditFilterPredicate()
    {
        await using SqlConnection connection = await OpenConnectionOrSkipAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(1)
            FROM sys.security_policies p
            INNER JOIN sys.security_predicates pr ON p.object_id = pr.object_id
            WHERE p.name = 'RLS_scope_tenant_empresa'
              AND pr.target_object_id = OBJECT_ID(N'seguridad.security_event_audit')
              AND pr.predicate_type = 0
              AND pr.predicate_definition LIKE N'%fn_rls_tenant_empresa%';
            """;

        int count = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.True(count > 0, "RLS filter predicate for seguridad.security_event_audit was not found.");
    }

    [Fact]
    public async Task SecurityEventWrite_ShouldPersistScopedAuditRow()
    {
        const long tenantId = 910001;
        const long companyId = 910001;
        string marker = $"PHASE3_TEST_{Guid.NewGuid():N}";

        await using SqlConnection connection = await OpenConnectionOrSkipAsync();
        await SetSessionContextAsync(connection, "id_tenant", tenantId);
        await SetSessionContextAsync(connection, "id_empresa", companyId);

        await using (SqlCommand insert = connection.CreateCommand())
        {
            insert.CommandType = CommandType.StoredProcedure;
            insert.CommandText = "[seguridad].[usp_security_event_write]";
            insert.Parameters.Add(new SqlParameter("@event_type", SqlDbType.VarChar, 80) { Value = marker });
            insert.Parameters.Add(new SqlParameter("@severity", SqlDbType.VarChar, 20) { Value = "INFO" });
            insert.Parameters.Add(new SqlParameter("@resultado", SqlDbType.VarChar, 30) { Value = "OK" });
            insert.Parameters.Add(new SqlParameter("@detalle", SqlDbType.NVarChar, 1000) { Value = "Phase 3 integration test." });
            insert.Parameters.Add(new SqlParameter("@id_tenant", SqlDbType.BigInt) { Value = tenantId });
            insert.Parameters.Add(new SqlParameter("@id_empresa", SqlDbType.BigInt) { Value = companyId });
            insert.Parameters.Add(new SqlParameter("@id_usuario", SqlDbType.BigInt) { Value = DBNull.Value });
            insert.Parameters.Add(new SqlParameter("@id_sesion_usuario", SqlDbType.UniqueIdentifier) { Value = DBNull.Value });
            insert.Parameters.Add(new SqlParameter("@auth_flow_id", SqlDbType.UniqueIdentifier) { Value = DBNull.Value });
            insert.Parameters.Add(new SqlParameter("@correlation_id", SqlDbType.UniqueIdentifier) { Value = DBNull.Value });
            insert.Parameters.Add(new SqlParameter("@ip_origen", SqlDbType.NVarChar, 45) { Value = "127.0.0.1" });
            insert.Parameters.Add(new SqlParameter("@agente_usuario", SqlDbType.NVarChar, 300) { Value = "SecureERP.Tests" });
            await insert.ExecuteNonQueryAsync();
        }

        int count;
        await using (SqlCommand verify = connection.CreateCommand())
        {
            verify.CommandText = """
                SELECT COUNT(1)
                FROM seguridad.security_event_audit
                WHERE event_type = @event_type
                  AND id_tenant = @id_tenant
                  AND id_empresa = @id_empresa;
                """;
            verify.Parameters.Add(new SqlParameter("@event_type", SqlDbType.VarChar, 80) { Value = marker });
            verify.Parameters.Add(new SqlParameter("@id_tenant", SqlDbType.BigInt) { Value = tenantId });
            verify.Parameters.Add(new SqlParameter("@id_empresa", SqlDbType.BigInt) { Value = companyId });
            count = Convert.ToInt32(await verify.ExecuteScalarAsync());
        }

        Assert.True(count > 0, "Audit row was not persisted or is not visible under current RLS context.");

        await using SqlCommand cleanup = connection.CreateCommand();
        cleanup.CommandText = "DELETE FROM seguridad.security_event_audit WHERE event_type = @event_type;";
        cleanup.Parameters.Add(new SqlParameter("@event_type", SqlDbType.VarChar, 80) { Value = marker });
        await cleanup.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task AuthorizationProcedure_ShouldDefaultDenyForUnknownContext()
    {
        await using SqlConnection connection = await OpenConnectionOrSkipAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "[seguridad].[usp_autorizacion_evaluar]";
        command.Parameters.Add(new SqlParameter("@id_usuario", SqlDbType.BigInt) { Value = 999999999L });
        command.Parameters.Add(new SqlParameter("@id_tenant", SqlDbType.BigInt) { Value = 999999999L });
        command.Parameters.Add(new SqlParameter("@id_empresa", SqlDbType.BigInt) { Value = 999999999L });
        command.Parameters.Add(new SqlParameter("@id_sesion_usuario", SqlDbType.UniqueIdentifier) { Value = Guid.NewGuid() });
        command.Parameters.Add(new SqlParameter("@codigo_permiso", SqlDbType.NVarChar, 150) { Value = "SECURITY.TEST.PERMISSION" });
        command.Parameters.Add(new SqlParameter("@requiere_mfa", SqlDbType.Bit) { Value = true });

        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync(), "Authorization procedure returned no rows.");

        bool allowed = reader.GetBoolean(reader.GetOrdinal("autorizado"));
        string reason = reader.GetString(reader.GetOrdinal("reason_code"));
        Assert.False(allowed);
        Assert.False(string.IsNullOrWhiteSpace(reason));
    }

    private static async Task<SqlConnection> OpenConnectionOrSkipAsync()
    {
        string? connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Integration DB connection string was not found.");
        }

        try
        {
            SqlConnection connection = new(connectionString);
            await connection.OpenAsync();
            return connection;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Integration DB is not reachable: {ex.Message}", ex);
        }
    }

    private static string? ResolveConnectionString()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable("SECUREERP_SQL_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        string? repoRoot = FindRepoRoot();
        if (repoRoot is null)
        {
            return null;
        }

        string configPath = Path.Combine(repoRoot, "database.config.example.json");
        if (!File.Exists(configPath))
        {
            return null;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(configPath));
            if (document.RootElement.TryGetProperty("connectionString", out JsonElement value))
            {
                return value.GetString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string? FindRepoRoot()
    {
        DirectoryInfo? current = new(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "SecureERP.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static async Task SetSessionContextAsync(SqlConnection connection, string key, object value)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sys.sp_set_session_context";
        command.Parameters.Add(new SqlParameter("@key", SqlDbType.NVarChar, 128) { Value = key });
        command.Parameters.Add(new SqlParameter("@value", SqlDbType.Variant) { Value = value });
        command.Parameters.Add(new SqlParameter("@read_only", SqlDbType.Bit) { Value = false });
        await command.ExecuteNonQueryAsync();
    }
}
