SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE organizacion.usp_unidad_organizativa_listar
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        DECLARE @ctx_id_tenant BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
        IF @ctx_id_tenant IS NULL THROW 51050, N'Scope id_tenant no disponible en SESSION_CONTEXT.', 1;

        DECLARE @ctx_id_empresa BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
        IF @ctx_id_empresa IS NULL THROW 51051, N'Scope id_empresa no disponible en SESSION_CONTEXT.', 1;

        SELECT
            t.id_unidad_organizativa,
            t.id_tenant,
            t.id_empresa,
            t.id_tipo_unidad_organizativa,
            t.id_unidad_padre,
            t.codigo,
            t.nombre,
            t.nivel_jerarquia,
            t.ruta_jerarquia,
            t.es_hoja,
            t.activo,
            t.creado_utc,
            t.actualizado_utc,
            t.version_fila
        FROM organizacion.unidad_organizativa AS t
        WHERE t.id_tenant = @ctx_id_tenant
          AND t.id_empresa = @ctx_id_empresa;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE organizacion.usp_unidad_organizativa_crear
    @id_tenant BIGINT,
    @id_empresa BIGINT,
    @id_tipo_unidad_organizativa SMALLINT,
    @id_unidad_padre BIGINT = NULL,
    @codigo NVARCHAR(60),
    @nombre NVARCHAR(200),
    @nivel_jerarquia SMALLINT,
    @ruta_jerarquia NVARCHAR(500),
    @es_hoja BIT,
    @activo BIT,
    @creado_utc DATETIME2,
    @actualizado_utc DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @__aud_id_usuario BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT);
    DECLARE @__aud_id_sesion UNIQUEIDENTIFIER = TRY_CAST(SESSION_CONTEXT(N'id_sesion') AS UNIQUEIDENTIFIER);
    DECLARE @__aud_usuario VARCHAR(100) = CASE WHEN @__aud_id_usuario IS NULL THEN NULL ELSE CAST(@__aud_id_usuario AS VARCHAR(100)) END;

    BEGIN TRY
        BEGIN TRAN;

        DECLARE @ctx_id_tenant BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
        IF @ctx_id_tenant IS NULL THROW 51050, N'Scope id_tenant no disponible en SESSION_CONTEXT.', 1;
        SET @id_tenant = @ctx_id_tenant;

        DECLARE @ctx_id_empresa BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
        IF @ctx_id_empresa IS NULL THROW 51051, N'Scope id_empresa no disponible en SESSION_CONTEXT.', 1;
        SET @id_empresa = @ctx_id_empresa;

        INSERT INTO organizacion.unidad_organizativa
        (
            id_tenant,
            id_empresa,
            id_tipo_unidad_organizativa,
            id_unidad_padre,
            codigo,
            nombre,
            nivel_jerarquia,
            ruta_jerarquia,
            es_hoja,
            activo,
            creado_utc,
            actualizado_utc
        )
        VALUES
        (
            @id_tenant,
            @id_empresa,
            @id_tipo_unidad_organizativa,
            @id_unidad_padre,
            @codigo,
            @nombre,
            @nivel_jerarquia,
            @ruta_jerarquia,
            @es_hoja,
            @activo,
            @creado_utc,
            @actualizado_utc
        );

        SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS id;

        EXEC cumplimiento.usp_auditoria_operacion_registrar
            @tabla = 'organizacion.usp_unidad_organizativa_crear',
            @operacion = 'EXECUTE',
            @id_registro = NULL,
            @valores_anteriores = NULL,
            @valores_nuevos = NULL,
            @usuario = @__aud_usuario,
            @correlation_id = @__aud_id_sesion,
            @id_usuario = @__aud_id_usuario,
            @id_sesion_usuario = @__aud_id_sesion,
            @ip_origen = NULL,
            @agente_usuario = NULL,
            @resultado = N'OK',
            @origen_aplicacion = N'organizacion.usp_unidad_organizativa_crear';

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE cumplimiento.usp_instancia_aprobacion_listar
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        DECLARE @ctx_id_tenant BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
        IF @ctx_id_tenant IS NULL THROW 51050, N'Scope id_tenant no disponible en SESSION_CONTEXT.', 1;

        DECLARE @ctx_id_empresa BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
        IF @ctx_id_empresa IS NULL THROW 51051, N'Scope id_empresa no disponible en SESSION_CONTEXT.', 1;

        SELECT
            t.id_instancia_aprobacion,
            t.id_tenant,
            t.id_empresa,
            t.id_unidad_organizativa,
            t.id_perfil_aprobacion,
            t.codigo_entidad,
            t.id_objeto,
            t.nivel_actual,
            t.id_estado_aprobacion,
            t.solicitado_por,
            t.solicitado_utc,
            t.expira_utc,
            t.motivo,
            t.hash_payload,
            t.activo
        FROM cumplimiento.instancia_aprobacion AS t
        WHERE t.id_tenant = @ctx_id_tenant
          AND t.id_empresa = @ctx_id_empresa;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE cumplimiento.usp_instancia_aprobacion_crear
    @id_tenant BIGINT,
    @id_empresa BIGINT,
    @id_unidad_organizativa BIGINT,
    @id_perfil_aprobacion BIGINT,
    @codigo_entidad NVARCHAR(128),
    @id_objeto BIGINT,
    @nivel_actual TINYINT,
    @id_estado_aprobacion SMALLINT,
    @solicitado_por BIGINT,
    @solicitado_utc DATETIME2,
    @expira_utc DATETIME2 = NULL,
    @motivo NVARCHAR(300) = NULL,
    @hash_payload BINARY(32),
    @activo BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRAN;

        DECLARE @ctx_id_tenant BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
        IF @ctx_id_tenant IS NULL THROW 51050, N'Scope id_tenant no disponible en SESSION_CONTEXT.', 1;
        SET @id_tenant = @ctx_id_tenant;

        DECLARE @ctx_id_empresa BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
        IF @ctx_id_empresa IS NULL THROW 51051, N'Scope id_empresa no disponible en SESSION_CONTEXT.', 1;
        SET @id_empresa = @ctx_id_empresa;

        INSERT INTO cumplimiento.instancia_aprobacion
        (
            id_tenant,
            id_empresa,
            id_unidad_organizativa,
            id_perfil_aprobacion,
            codigo_entidad,
            id_objeto,
            nivel_actual,
            id_estado_aprobacion,
            solicitado_por,
            solicitado_utc,
            expira_utc,
            motivo,
            hash_payload,
            activo
        )
        VALUES
        (
            @id_tenant,
            @id_empresa,
            @id_unidad_organizativa,
            @id_perfil_aprobacion,
            @codigo_entidad,
            @id_objeto,
            @nivel_actual,
            @id_estado_aprobacion,
            @solicitado_por,
            @solicitado_utc,
            @expira_utc,
            @motivo,
            @hash_payload,
            @activo
        );

        SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS id;

        DECLARE @__aud_id_usuario BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT);
        DECLARE @__aud_id_sesion UNIQUEIDENTIFIER = TRY_CAST(SESSION_CONTEXT(N'id_sesion') AS UNIQUEIDENTIFIER);
        DECLARE @__aud_usuario VARCHAR(100) = CASE WHEN @__aud_id_usuario IS NULL THEN NULL ELSE CAST(@__aud_id_usuario AS VARCHAR(100)) END;

        EXEC cumplimiento.usp_auditoria_operacion_registrar
            @tabla = 'cumplimiento.usp_instancia_aprobacion_crear',
            @operacion = 'EXECUTE',
            @id_registro = NULL,
            @valores_anteriores = NULL,
            @valores_nuevos = NULL,
            @usuario = @__aud_usuario,
            @correlation_id = @__aud_id_sesion,
            @id_usuario = @__aud_id_usuario,
            @id_sesion_usuario = @__aud_id_sesion,
            @ip_origen = NULL,
            @agente_usuario = NULL,
            @resultado = N'OK',
            @origen_aplicacion = N'cumplimiento.usp_instancia_aprobacion_crear';

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END;
GO
