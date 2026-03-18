using Microsoft.Data.SqlClient;
using SecureERP.Domain.Modules.Purchase;
using SecureERP.Infrastructure.Persistence.Db;
using SecureERP.Infrastructure.Persistence.Mapping;
using SecureERP.Infrastructure.Persistence.SessionContext;
using System.Data;

namespace SecureERP.Infrastructure.Business;

public sealed class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private const string CreateDraftProcedure = "[compras].[usp_orden_compra_crear_borrador]";
    private const string GetByIdProcedure = "[compras].[usp_orden_compra_obtener_por_id]";
    private const string ListProcedure = "[compras].[usp_orden_compra_listar]";
    private const string UpdateDraftProcedure = "[compras].[usp_orden_compra_actualizar_borrador]";
    private const string UpsertDetailProcedure = "[compras].[usp_orden_compra_detalle_guardar_borrador]";
    private const string SubmitProcedure = "[compras].[usp_orden_compra_enviar]";
    private const string ApproveProcedure = "[compras].[usp_orden_compra_aprobar]";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ISqlSessionContextApplier _sessionContextApplier;

    public PurchaseOrderRepository(
        ISqlConnectionFactory connectionFactory,
        ISqlSessionContextApplier sessionContextApplier)
    {
        _connectionFactory = connectionFactory;
        _sessionContextApplier = sessionContextApplier;
    }

    public async Task<long> CreateDraftAsync(PurchaseOrderToCreate request, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, CreateDraftProcedure);
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_unidad_organizativa", request.OrganizationUnitId));
        command.Parameters.Add(SqlParameterFactory.DateTime2("@fecha_orden", request.RequestDate));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@observaciones", request.Notes, 1000));
        command.Parameters.Add(SqlParameterFactory.BigInt("@creado_por", request.CreatedByUserId));
        command.Parameters.Add(SqlParameterFactory.DateTime2("@creado_utc", request.UtcCreatedAt));

        object? scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is null || scalar == DBNull.Value ? 0 : Convert.ToInt64(scalar);
    }

    public async Task<PurchaseOrderSnapshot?> GetByIdAsync(long purchaseOrderId, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, GetByIdProcedure);
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_orden_compra", purchaseOrderId));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        PurchaseOrderSnapshot? header = null;
        List<PurchaseOrderDetailSnapshot> details = new();
        while (await reader.ReadAsync(cancellationToken))
        {
            if (header is null)
            {
                header = new PurchaseOrderSnapshot(
                    reader.GetInt64(reader.GetOrdinal("id_orden_compra")),
                    reader.GetInt64(reader.GetOrdinal("id_tenant")),
                    reader.GetInt64(reader.GetOrdinal("id_empresa")),
                    reader.IsDBNull(reader.GetOrdinal("id_unidad_organizativa")) ? null : reader.GetInt64(reader.GetOrdinal("id_unidad_organizativa")),
                    reader.GetString(reader.GetOrdinal("numero_orden_compra")),
                    reader.GetDateTime(reader.GetOrdinal("fecha_orden")),
                    (PurchaseOrderState)reader.GetInt16(reader.GetOrdinal("id_estado_orden_compra")),
                    reader.GetInt64(reader.GetOrdinal("creado_por")),
                    reader.GetDateTime(reader.GetOrdinal("creado_utc")),
                    reader.IsDBNull(reader.GetOrdinal("actualizado_por")) ? null : reader.GetInt64(reader.GetOrdinal("actualizado_por")),
                    reader.IsDBNull(reader.GetOrdinal("actualizado_utc")) ? null : reader.GetDateTime(reader.GetOrdinal("actualizado_utc")),
                    reader.IsDBNull(reader.GetOrdinal("observaciones")) ? null : reader.GetString(reader.GetOrdinal("observaciones")),
                    reader.GetDecimal(reader.GetOrdinal("total_orden")),
                    reader.GetBoolean(reader.GetOrdinal("activo")),
                    Array.Empty<PurchaseOrderDetailSnapshot>());
            }

            if (!reader.IsDBNull(reader.GetOrdinal("id_orden_compra_detalle")))
            {
                details.Add(new PurchaseOrderDetailSnapshot(
                    reader.GetInt64(reader.GetOrdinal("id_orden_compra_detalle")),
                    reader.GetInt64(reader.GetOrdinal("id_orden_compra")),
                    reader.GetInt32(reader.GetOrdinal("linea")),
                    reader.GetString(reader.GetOrdinal("descripcion")),
                    reader.GetDecimal(reader.GetOrdinal("cantidad")),
                    reader.GetDecimal(reader.GetOrdinal("costo_unitario")),
                    reader.GetDecimal(reader.GetOrdinal("total_linea")),
                    reader.IsDBNull(reader.GetOrdinal("centro_costo_codigo")) ? null : reader.GetString(reader.GetOrdinal("centro_costo_codigo")),
                    reader.GetBoolean(reader.GetOrdinal("detalle_activo")),
                    reader.GetDateTime(reader.GetOrdinal("detalle_creado_utc")),
                    reader.IsDBNull(reader.GetOrdinal("detalle_actualizado_utc")) ? null : reader.GetDateTime(reader.GetOrdinal("detalle_actualizado_utc"))));
            }
        }

        if (header is null)
        {
            return null;
        }

        return header with { Details = details };
    }

    public async Task<IReadOnlyList<PurchaseOrderListItemSnapshot>> ListAsync(CancellationToken cancellationToken = default)
    {
        List<PurchaseOrderListItemSnapshot> rows = new();

        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, ListProcedure);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new PurchaseOrderListItemSnapshot(
                reader.GetInt64(reader.GetOrdinal("id_orden_compra")),
                reader.GetString(reader.GetOrdinal("numero_orden_compra")),
                reader.GetDateTime(reader.GetOrdinal("fecha_orden")),
                (PurchaseOrderState)reader.GetInt16(reader.GetOrdinal("id_estado_orden_compra")),
                reader.GetInt64(reader.GetOrdinal("creado_por")),
                reader.GetDecimal(reader.GetOrdinal("total_orden")),
                reader.GetBoolean(reader.GetOrdinal("activo")),
                reader.GetDateTime(reader.GetOrdinal("creado_utc")),
                reader.IsDBNull(reader.GetOrdinal("actualizado_utc")) ? null : reader.GetDateTime(reader.GetOrdinal("actualizado_utc"))));
        }

        return rows;
    }

    public async Task<bool> UpdateDraftAsync(PurchaseOrderToUpdate request, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, UpdateDraftProcedure);
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_orden_compra", request.PurchaseOrderId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_unidad_organizativa", request.OrganizationUnitId));
        command.Parameters.Add(SqlParameterFactory.DateTime2("@fecha_orden", request.RequestDate));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@observaciones", request.Notes, 1000));
        command.Parameters.Add(SqlParameterFactory.BigInt("@actualizado_por", request.UpdatedByUserId));
        command.Parameters.Add(SqlParameterFactory.DateTime2("@actualizado_utc", request.UtcUpdatedAt));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return false;
        }

        return reader.GetBoolean(reader.GetOrdinal("ok"));
    }

    public async Task<bool> UpsertDraftDetailAsync(PurchaseOrderDetailToUpsert detail, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, UpsertDetailProcedure);
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_orden_compra", detail.PurchaseOrderId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_orden_compra_detalle", detail.PurchaseOrderDetailId));
        command.Parameters.Add(SqlParameterFactory.Int("@linea", detail.LineNumber));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@descripcion", detail.Description, 400));
        command.Parameters.Add(SqlParameterFactory.Decimal("@cantidad", detail.Quantity, 18, 4));
        command.Parameters.Add(SqlParameterFactory.Decimal("@costo_unitario", detail.EstimatedUnitCost, 18, 4));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@centro_costo_codigo", detail.CostCenterCode, 50));
        command.Parameters.Add(SqlParameterFactory.BigInt("@actualizado_por", detail.UpdatedByUserId));
        command.Parameters.Add(SqlParameterFactory.DateTime2("@actualizado_utc", detail.UtcUpdatedAt));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return false;
        }

        return reader.GetBoolean(reader.GetOrdinal("ok"));
    }

    public async Task<PurchaseOrderActionResultSnapshot> SubmitAsync(
        long purchaseOrderId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, SubmitProcedure);
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_orden_compra", purchaseOrderId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@usuario_operacion", userId));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new PurchaseOrderActionResultSnapshot(false, "PURCHASE_ORDER_SUBMIT_FAILED", "Submit produced no response.", null);
        }

        return new PurchaseOrderActionResultSnapshot(
            reader.GetBoolean(reader.GetOrdinal("ok")),
            reader.IsDBNull(reader.GetOrdinal("error_code")) ? null : reader.GetString(reader.GetOrdinal("error_code")),
            reader.IsDBNull(reader.GetOrdinal("error_message")) ? null : reader.GetString(reader.GetOrdinal("error_message")),
            reader.IsDBNull(reader.GetOrdinal("new_state_id"))
                ? null
                : (PurchaseOrderState)reader.GetInt16(reader.GetOrdinal("new_state_id")));
    }

    public async Task<PurchaseOrderActionResultSnapshot> ApproveAsync(
        long purchaseOrderId,
        long userId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _sessionContextApplier.ApplyAsync(connection, cancellationToken);

        await using SqlCommand command = CreateStoredProcedureCommand(connection, ApproveProcedure);
        command.Parameters.Add(SqlParameterFactory.BigInt("@id_orden_compra", purchaseOrderId));
        command.Parameters.Add(SqlParameterFactory.BigInt("@usuario_operacion", userId));
        command.Parameters.Add(SqlParameterFactory.NVarChar("@comentario", comment, 1000));

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new PurchaseOrderActionResultSnapshot(false, "PURCHASE_ORDER_APPROVE_FAILED", "Approve produced no response.", null);
        }

        return new PurchaseOrderActionResultSnapshot(
            reader.GetBoolean(reader.GetOrdinal("ok")),
            reader.IsDBNull(reader.GetOrdinal("error_code")) ? null : reader.GetString(reader.GetOrdinal("error_code")),
            reader.IsDBNull(reader.GetOrdinal("error_message")) ? null : reader.GetString(reader.GetOrdinal("error_message")),
            reader.IsDBNull(reader.GetOrdinal("new_state_id"))
                ? null
                : (PurchaseOrderState)reader.GetInt16(reader.GetOrdinal("new_state_id")));
    }

    private static SqlCommand CreateStoredProcedureCommand(SqlConnection connection, string procedure)
    {
        SqlCommand command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = procedure;
        return command;
    }
}




