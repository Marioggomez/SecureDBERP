SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'seguridad.security_event_audit', N'U') IS NULL
BEGIN
    CREATE TABLE seguridad.security_event_audit
    (
        id_evento BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_security_event_audit PRIMARY KEY,
        event_type VARCHAR(80) NOT NULL,
        severity VARCHAR(20) NOT NULL,
        id_tenant BIGINT NULL,
        id_empresa BIGINT NULL,
        id_usuario BIGINT NULL,
        id_sesion_usuario UNIQUEIDENTIFIER NULL,
        auth_flow_id UNIQUEIDENTIFIER NULL,
        correlation_id UNIQUEIDENTIFIER NULL,
        ip_origen NVARCHAR(45) NULL,
        agente_usuario NVARCHAR(300) NULL,
        resultado VARCHAR(30) NOT NULL,
        detalle NVARCHAR(1000) NULL,
        creado_utc DATETIME2(3) NOT NULL CONSTRAINT DF_security_event_audit_creado_utc DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'seguridad.security_event_audit')
      AND name = N'IX_security_event_audit_creado_utc')
BEGIN
    CREATE INDEX IX_security_event_audit_creado_utc
        ON seguridad.security_event_audit (creado_utc DESC);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'seguridad.security_event_audit')
      AND name = N'IX_security_event_audit_actor')
BEGIN
    CREATE INDEX IX_security_event_audit_actor
        ON seguridad.security_event_audit (id_tenant, id_usuario, creado_utc DESC);
END;
GO
