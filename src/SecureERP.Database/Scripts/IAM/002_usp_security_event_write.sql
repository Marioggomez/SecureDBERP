SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE seguridad.usp_security_event_write
    @event_type VARCHAR(80),
    @severity VARCHAR(20),
    @resultado VARCHAR(30),
    @detalle NVARCHAR(1000) = NULL,
    @id_tenant BIGINT = NULL,
    @id_empresa BIGINT = NULL,
    @id_usuario BIGINT = NULL,
    @id_sesion_usuario UNIQUEIDENTIFIER = NULL,
    @auth_flow_id UNIQUEIDENTIFIER = NULL,
    @correlation_id UNIQUEIDENTIFIER = NULL,
    @ip_origen NVARCHAR(45) = NULL,
    @agente_usuario NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO seguridad.security_event_audit
    (
        event_type,
        severity,
        id_tenant,
        id_empresa,
        id_usuario,
        id_sesion_usuario,
        auth_flow_id,
        correlation_id,
        ip_origen,
        agente_usuario,
        resultado,
        detalle
    )
    VALUES
    (
        @event_type,
        @severity,
        @id_tenant,
        @id_empresa,
        @id_usuario,
        @id_sesion_usuario,
        @auth_flow_id,
        @correlation_id,
        @ip_origen,
        @agente_usuario,
        @resultado,
        @detalle
    );
END;
GO
