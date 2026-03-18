SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF SCHEMA_ID(N'observabilidad') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA observabilidad AUTHORIZATION dbo;');
END;
GO

IF OBJECT_ID(N'observabilidad.auditoria_autorizacion', N'U') IS NULL
BEGIN
    CREATE TABLE observabilidad.auditoria_autorizacion
    (
        id_auditoria_autorizacion BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_observabilidad_auditoria_autorizacion PRIMARY KEY,
        fecha_utc DATETIME2(7) NOT NULL,
        id_tenant BIGINT NULL,
        id_usuario BIGINT NULL,
        id_empresa BIGINT NULL,
        id_sesion_usuario UNIQUEIDENTIFIER NULL,
        codigo_permiso NVARCHAR(150) NULL,
        codigo_operacion NVARCHAR(150) NULL,
        metodo_http NVARCHAR(10) NULL,
        permitido BIT NOT NULL,
        motivo NVARCHAR(200) NULL,
        codigo_entidad NVARCHAR(128) NULL,
        id_objeto BIGINT NULL,
        ip_origen NVARCHAR(45) NULL,
        agente_usuario NVARCHAR(300) NULL,
        solicitud_id NVARCHAR(64) NULL
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'observabilidad.auditoria_autorizacion')
      AND name = N'IX_auditoria_autorizacion_fecha'
)
BEGIN
    CREATE INDEX IX_auditoria_autorizacion_fecha
        ON observabilidad.auditoria_autorizacion (fecha_utc DESC);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'observabilidad.auditoria_autorizacion')
      AND name = N'IX_auditoria_autorizacion_actor'
)
BEGIN
    CREATE INDEX IX_auditoria_autorizacion_actor
        ON observabilidad.auditoria_autorizacion (id_tenant, id_usuario, fecha_utc DESC);
END;
GO

IF OBJECT_ID(N'observabilidad.usp_auditoria_autorizacion_crear', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE observabilidad.usp_auditoria_autorizacion_crear AS BEGIN SET NOCOUNT ON; END');
END;
GO

CREATE OR ALTER PROCEDURE observabilidad.usp_auditoria_autorizacion_crear
    @fecha_utc DATETIME2,
    @id_tenant BIGINT,
    @id_usuario BIGINT,
    @id_empresa BIGINT,
    @id_sesion_usuario UNIQUEIDENTIFIER,
    @codigo_permiso NVARCHAR(150),
    @codigo_operacion NVARCHAR(150),
    @metodo_http NVARCHAR(10),
    @permitido BIT,
    @motivo NVARCHAR(200),
    @codigo_entidad NVARCHAR(128),
    @id_objeto BIGINT,
    @ip_origen NVARCHAR(45),
    @agente_usuario NVARCHAR(300),
    @solicitud_id NVARCHAR(64)
AS
BEGIN
    BEGIN TRY
        BEGIN TRAN;
        SET NOCOUNT ON;
        SET XACT_ABORT ON;

        DECLARE @ctx_id_tenant BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
        DECLARE @ctx_id_empresa BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
        DECLARE @ctx_id_usuario BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT);
        DECLARE @ctx_id_sesion UNIQUEIDENTIFIER = TRY_CAST(SESSION_CONTEXT(N'id_sesion') AS UNIQUEIDENTIFIER);

        IF @ctx_id_tenant IS NOT NULL SET @id_tenant = @ctx_id_tenant;
        IF @ctx_id_empresa IS NOT NULL SET @id_empresa = @ctx_id_empresa;
        IF @ctx_id_usuario IS NOT NULL SET @id_usuario = @ctx_id_usuario;
        IF @ctx_id_sesion IS NOT NULL SET @id_sesion_usuario = @ctx_id_sesion;

        INSERT INTO observabilidad.auditoria_autorizacion
        (
            fecha_utc,
            id_tenant,
            id_usuario,
            id_empresa,
            id_sesion_usuario,
            codigo_permiso,
            codigo_operacion,
            metodo_http,
            permitido,
            motivo,
            codigo_entidad,
            id_objeto,
            ip_origen,
            agente_usuario,
            solicitud_id
        )
        VALUES
        (
            @fecha_utc,
            @id_tenant,
            @id_usuario,
            @id_empresa,
            @id_sesion_usuario,
            @codigo_permiso,
            @codigo_operacion,
            @metodo_http,
            @permitido,
            @motivo,
            @codigo_entidad,
            @id_objeto,
            @ip_origen,
            @agente_usuario,
            @solicitud_id
        );

        SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS id;

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END;
GO
