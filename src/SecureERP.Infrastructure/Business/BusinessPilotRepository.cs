using Microsoft.Data.SqlClient;
using SecureERP.Domain.Modules.Organization;
using SecureERP.Domain.Modules.Workflow;
using SecureERP.Infrastructure.Persistence.Db;
using SecureERP.Infrastructure.Persistence.Mapping;
using SecureERP.Infrastructure.Persistence.SessionContext;
using System.Data;

namespace SecureERP.Infrastructure.Business;

public sealed class BusinessPilotRepository : IOrganizationPilotRepository, IWorkflowPilotRepository
{
    private const string ListUnitsProcedure = "[organizacion].[usp_unidad_organizativa_listar]";
    private const string CreateUnitProcedure = "[organizacion].[usp_unidad_organizativa_crear]";
    private const string ListApprovalInstancesProcedure = "[cumplimiento].[usp_instancia_aprobacion_listar]";
    private const string CreateApprovalInstanceProcedure = "[cumplimiento].[usp_instancia_aprobacion_crear]";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ISqlSessionContextApplier _sessionContextApplier;

    public BusinessPilotRepository(
        ISqlConnectionFactory connectionFactory,
        ISqlSessionContextApplier sessionContextApplier)
    {
        _connectionFactory = connectionFactory;
        _sessionContextApplier = sessionContextApplier;
    }

    public async Task<IReadOnlyList<OrganizationUnitSnapshot>> ListUnitsAsync(CancellationToken cancellationToken = default)
    {
        List<OrganizationUnitSnapshot> result = new();

        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, ListUnitsProcedure);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new OrganizationUnitSnapshot(
                reader.GetInt64(reader.GetOrdinal("id_unidad_organizativa")),
                reader.GetInt64(reader.GetOrdinal("id_tenant")),
                reader.GetInt64(reader.GetOrdinal("id_empresa")),
                reader.GetInt16(reader.GetOrdinal("id_tipo_unidad_organizativa")),
                reader.IsDBNull(reader.GetOrdinal("id_unidad_padre")) ? null : reader.GetInt64(reader.GetOrdinal("id_unidad_padre")),
                reader.GetString(reader.GetOrdinal("codigo")),
                reader.GetString(reader.GetOrdinal("nombre")),
                reader.GetInt16(reader.GetOrdinal("nivel_jerarquia")),
                reader.GetString(reader.GetOrdinal("ruta_jerarquia")),
                reader.GetBoolean(reader.GetOrdinal("es_hoja")),
                reader.GetBoolean(reader.GetOrdinal("activo")),
                reader.GetDateTime(reader.GetOrdinal("creado_utc")),
                reader.IsDBNull(reader.GetOrdinal("actualizado_utc")) ? null : reader.GetDateTime(reader.GetOrdinal("actualizado_utc"))));
        }

        return result;
    }

    public async Task<long> CreateUnitAsync(OrganizationUnitToCreate unit, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, CreateUnitProcedure);
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_tenant", 0));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_empresa", 0));
        command.Parameters.Add(SqlParameterFactory.SmallInt("@id_tipo_unidad_organizativa", unit.UnitTypeId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_unidad_padre", unit.ParentUnitId));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@codigo", unit.Code, 60));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@nombre", unit.Name, 200));
        command.Parameters.Add(SqlParameterFactory.SmallInt("@nivel_jerarquia", unit.HierarchyLevel));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@ruta_jerarquia", unit.HierarchyPath, 500));
        command.Parameters.Add(SqlParameterFactory.Bit("@es_hoja", unit.IsLeaf));
        command.Parameters.Add(SqlParameterFactory.Bit("@activo", unit.IsActive));
        command.Parameters.Add(SqlParameterFactory.DateTime2("@creado_utc", unit.UtcCreatedAt));
        command.Parameters.Add(SqlParameterFactory.DateTime2("@actualizado_utc", unit.UtcUpdatedAt));

        object? scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is null || scalar == DBNull.Value ? 0 : Convert.ToInt64(scalar);
    }

    public async Task<IReadOnlyList<ApprovalInstanceSnapshot>> ListApprovalInstancesAsync(CancellationToken cancellationToken = default)
    {
        List<ApprovalInstanceSnapshot> result = new();

        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, ListApprovalInstancesProcedure);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ApprovalInstanceSnapshot(
                reader.GetInt64(reader.GetOrdinal("id_instancia_aprobacion")),
                reader.GetInt64(reader.GetOrdinal("id_tenant")),
                reader.GetInt64(reader.GetOrdinal("id_empresa")),
                reader.GetInt64(reader.GetOrdinal("id_unidad_organizativa")),
                reader.GetInt64(reader.GetOrdinal("id_perfil_aprobacion")),
                reader.GetString(reader.GetOrdinal("codigo_entidad")),
                reader.GetInt64(reader.GetOrdinal("id_objeto")),
                reader.GetByte(reader.GetOrdinal("nivel_actual")),
                reader.GetInt16(reader.GetOrdinal("id_estado_aprobacion")),
                reader.GetInt64(reader.GetOrdinal("solicitado_por")),
                reader.GetDateTime(reader.GetOrdinal("solicitado_utc")),
                reader.IsDBNull(reader.GetOrdinal("expira_utc")) ? null : reader.GetDateTime(reader.GetOrdinal("expira_utc")),
                reader.IsDBNull(reader.GetOrdinal("motivo")) ? null : reader.GetString(reader.GetOrdinal("motivo")),
                (byte[])reader["hash_payload"],
                reader.GetBoolean(reader.GetOrdinal("activo"))));
        }

        return result;
    }

    public async Task<long> CreateApprovalInstanceAsync(
        ApprovalInstanceToCreate instance,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, CreateApprovalInstanceProcedure);
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_tenant", 0));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_empresa", 0));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_unidad_organizativa", instance.OrganizationUnitId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_perfil_aprobacion", instance.ApprovalProfileId));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@codigo_entidad", instance.EntityCode, 128));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_objeto", instance.ObjectId));
        command.Parameters.Add(SqlParameterFactory.TinyInt("@nivel_actual", instance.CurrentLevel));
        command.Parameters.Add(SqlParameterFactory.SmallInt("@id_estado_aprobacion", instance.ApprovalStateId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@solicitado_por", instance.RequestedByUserId));
        command.Parameters.Add(SqlParameterFactory.DateTime2("@solicitado_utc", instance.UtcRequestedAt));
        command.Parameters.Add(SqlParameterFactory.DateTime2("@expira_utc", instance.UtcExpiresAt));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@motivo", instance.Reason, 300));
        command.Parameters.Add(SqlParameterFactory.Binary("@hash_payload", instance.PayloadHash, 32));
        command.Parameters.Add(SqlParameterFactory.Bit("@activo", instance.IsActive));

        object? scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is null || scalar == DBNull.Value ? 0 : Convert.ToInt64(scalar);
    }

    private static SqlCommand CreateStoredProcedureCommand(SqlConnection connection, string procedure)
    {
        SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = procedure;
        return command;
    }
}
