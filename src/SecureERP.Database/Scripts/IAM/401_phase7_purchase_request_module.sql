SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'compras')
BEGIN
    EXEC(N'CREATE SCHEMA compras AUTHORIZATION dbo;');
END;
GO

IF OBJECT_ID(N'catalogo.estado_solicitud_compra', N'U') IS NULL
BEGIN
    CREATE TABLE catalogo.estado_solicitud_compra
    (
        id_estado_solicitud_compra SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_estado_solicitud_compra PRIMARY KEY,
        codigo VARCHAR(30) NOT NULL,
        nombre NVARCHAR(120) NOT NULL,
        descripcion NVARCHAR(400) NULL,
        orden_visual SMALLINT NOT NULL,
        activo BIT NOT NULL CONSTRAINT DF_estado_solicitud_compra_activo DEFAULT(1),
        creado_utc DATETIME2(7) NOT NULL CONSTRAINT DF_estado_solicitud_compra_creado_utc DEFAULT(SYSUTCDATETIME()),
        actualizado_utc DATETIME2(7) NULL
    );
    CREATE UNIQUE INDEX UX_estado_solicitud_compra_codigo ON catalogo.estado_solicitud_compra(codigo);
END;
GO

IF NOT EXISTS (SELECT 1 FROM catalogo.estado_solicitud_compra WHERE codigo='DRAFT')
    INSERT INTO catalogo.estado_solicitud_compra(codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES('DRAFT',N'Borrador',N'Solicitud editable en preparacion.',1,1,SYSUTCDATETIME(),NULL);
GO
IF NOT EXISTS (SELECT 1 FROM catalogo.estado_solicitud_compra WHERE codigo='SUBMITTED')
    INSERT INTO catalogo.estado_solicitud_compra(codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES('SUBMITTED',N'Enviada',N'Solicitud enviada para aprobacion.',2,1,SYSUTCDATETIME(),NULL);
GO
IF NOT EXISTS (SELECT 1 FROM catalogo.estado_solicitud_compra WHERE codigo='APPROVED')
    INSERT INTO catalogo.estado_solicitud_compra(codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES('APPROVED',N'Aprobada',N'Solicitud aprobada.',3,1,SYSUTCDATETIME(),NULL);
GO
IF NOT EXISTS (SELECT 1 FROM catalogo.estado_solicitud_compra WHERE codigo='REJECTED')
    INSERT INTO catalogo.estado_solicitud_compra(codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES('REJECTED',N'Rechazada',N'Solicitud rechazada.',4,1,SYSUTCDATETIME(),NULL);
GO
IF NOT EXISTS (SELECT 1 FROM catalogo.estado_solicitud_compra WHERE codigo='CANCELLED')
    INSERT INTO catalogo.estado_solicitud_compra(codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES('CANCELLED',N'Cancelada',N'Solicitud cancelada.',5,1,SYSUTCDATETIME(),NULL);
GO

IF OBJECT_ID(N'compras.solicitud', N'U') IS NULL
BEGIN
    CREATE TABLE compras.solicitud
    (
        id_solicitud BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_compras_solicitud PRIMARY KEY,
        id_tenant BIGINT NOT NULL,
        id_empresa BIGINT NOT NULL,
        id_unidad_organizativa BIGINT NULL,
        numero_solicitud NVARCHAR(40) NOT NULL,
        fecha_solicitud DATE NOT NULL,
        id_estado_solicitud_compra SMALLINT NOT NULL,
        creado_por BIGINT NOT NULL,
        creado_utc DATETIME2(7) NOT NULL,
        actualizado_por BIGINT NULL,
        actualizado_utc DATETIME2(7) NULL,
        observaciones NVARCHAR(1000) NULL,
        total_estimado DECIMAL(18,4) NOT NULL CONSTRAINT DF_compras_solicitud_total_estimado DEFAULT(0),
        activo BIT NOT NULL CONSTRAINT DF_compras_solicitud_activo DEFAULT(1),
        version_fila ROWVERSION NOT NULL,
        CONSTRAINT FK_compras_solicitud_tenant FOREIGN KEY (id_tenant) REFERENCES plataforma.tenant(id_tenant),
        CONSTRAINT FK_compras_solicitud_empresa FOREIGN KEY (id_empresa) REFERENCES organizacion.empresa(id_empresa),
        CONSTRAINT FK_compras_solicitud_estado FOREIGN KEY (id_estado_solicitud_compra) REFERENCES catalogo.estado_solicitud_compra(id_estado_solicitud_compra)
    );
    CREATE UNIQUE INDEX UX_compras_solicitud_numero ON compras.solicitud(id_tenant,id_empresa,numero_solicitud);
    CREATE INDEX IX_compras_solicitud_scope ON compras.solicitud(id_tenant,id_empresa,id_estado_solicitud_compra,fecha_solicitud DESC);
END;
GO

IF OBJECT_ID(N'compras.solicitud_detalle', N'U') IS NULL
BEGIN
    CREATE TABLE compras.solicitud_detalle
    (
        id_solicitud_detalle BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_compras_solicitud_detalle PRIMARY KEY,
        id_solicitud BIGINT NOT NULL,
        id_tenant BIGINT NOT NULL,
        id_empresa BIGINT NOT NULL,
        linea INT NOT NULL,
        descripcion NVARCHAR(400) NOT NULL,
        cantidad DECIMAL(18,4) NOT NULL,
        costo_estimado_unitario DECIMAL(18,4) NOT NULL,
        total_estimado_linea AS (ROUND(cantidad * costo_estimado_unitario, 4)) PERSISTED,
        centro_costo_codigo NVARCHAR(50) NULL,
        activo BIT NOT NULL CONSTRAINT DF_compras_solicitud_detalle_activo DEFAULT(1),
        creado_utc DATETIME2(7) NOT NULL,
        actualizado_utc DATETIME2(7) NULL,
        version_fila ROWVERSION NOT NULL,
        CONSTRAINT FK_compras_solicitud_detalle_solicitud FOREIGN KEY (id_solicitud) REFERENCES compras.solicitud(id_solicitud),
        CONSTRAINT FK_compras_solicitud_detalle_tenant FOREIGN KEY (id_tenant) REFERENCES plataforma.tenant(id_tenant),
        CONSTRAINT FK_compras_solicitud_detalle_empresa FOREIGN KEY (id_empresa) REFERENCES organizacion.empresa(id_empresa)
    );
    CREATE UNIQUE INDEX UX_compras_solicitud_detalle_linea ON compras.solicitud_detalle(id_solicitud,linea);
END;
GO

IF OBJECT_ID(N'seguridad.fn_rls_tenant_empresa', N'IF') IS NOT NULL
AND EXISTS (SELECT 1 FROM sys.security_policies WHERE name=N'RLS_scope_tenant_empresa' AND SCHEMA_NAME(schema_id)=N'seguridad')
BEGIN
    IF OBJECT_ID(N'compras.solicitud', N'U') IS NOT NULL
    BEGIN
        BEGIN TRY
            ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa DROP FILTER PREDICATE ON compras.solicitud;
        END TRY
        BEGIN CATCH
            IF ERROR_MESSAGE() NOT LIKE N'%cannot be found%' THROW;
        END CATCH;
        BEGIN TRY
            ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa DROP BLOCK PREDICATE ON compras.solicitud AFTER INSERT;
        END TRY
        BEGIN CATCH
            IF ERROR_MESSAGE() NOT LIKE N'%cannot be found%' THROW;
        END CATCH;
        BEGIN TRY
            ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa DROP BLOCK PREDICATE ON compras.solicitud AFTER UPDATE;
        END TRY
        BEGIN CATCH
            IF ERROR_MESSAGE() NOT LIKE N'%cannot be found%' THROW;
        END CATCH;

        EXEC(N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa ADD FILTER PREDICATE seguridad.fn_rls_tenant_empresa(id_tenant,id_empresa) ON compras.solicitud;');
        EXEC(N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa ADD BLOCK PREDICATE seguridad.fn_rls_tenant_empresa(id_tenant,id_empresa) ON compras.solicitud AFTER INSERT;');
        EXEC(N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa ADD BLOCK PREDICATE seguridad.fn_rls_tenant_empresa(id_tenant,id_empresa) ON compras.solicitud AFTER UPDATE;');
    END;

    IF OBJECT_ID(N'compras.solicitud_detalle', N'U') IS NOT NULL
    BEGIN
        BEGIN TRY
            ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa DROP FILTER PREDICATE ON compras.solicitud_detalle;
        END TRY
        BEGIN CATCH
            IF ERROR_MESSAGE() NOT LIKE N'%cannot be found%' THROW;
        END CATCH;
        BEGIN TRY
            ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa DROP BLOCK PREDICATE ON compras.solicitud_detalle AFTER INSERT;
        END TRY
        BEGIN CATCH
            IF ERROR_MESSAGE() NOT LIKE N'%cannot be found%' THROW;
        END CATCH;
        BEGIN TRY
            ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa DROP BLOCK PREDICATE ON compras.solicitud_detalle AFTER UPDATE;
        END TRY
        BEGIN CATCH
            IF ERROR_MESSAGE() NOT LIKE N'%cannot be found%' THROW;
        END CATCH;

        EXEC(N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa ADD FILTER PREDICATE seguridad.fn_rls_tenant_empresa(id_tenant,id_empresa) ON compras.solicitud_detalle;');
        EXEC(N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa ADD BLOCK PREDICATE seguridad.fn_rls_tenant_empresa(id_tenant,id_empresa) ON compras.solicitud_detalle AFTER INSERT;');
        EXEC(N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa ADD BLOCK PREDICATE seguridad.fn_rls_tenant_empresa(id_tenant,id_empresa) ON compras.solicitud_detalle AFTER UPDATE;');
    END;
END;
GO

IF OBJECT_ID(N'compras.usp_solicitud_crear_borrador', N'P') IS NULL EXEC(N'CREATE PROCEDURE compras.usp_solicitud_crear_borrador AS BEGIN SET NOCOUNT ON; END');
GO
CREATE OR ALTER PROCEDURE compras.usp_solicitud_crear_borrador
    @id_unidad_organizativa BIGINT = NULL,
    @fecha_solicitud DATETIME2,
    @observaciones NVARCHAR(1000) = NULL,
    @creado_por BIGINT,
    @creado_utc DATETIME2
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @t BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
    DECLARE @e BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
    DECLARE @u BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT);
    DECLARE @s UNIQUEIDENTIFIER = TRY_CAST(SESSION_CONTEXT(N'id_sesion') AS UNIQUEIDENTIFIER);
    DECLARE @audit_usuario VARCHAR(100) = CONVERT(VARCHAR(100), @u);
    IF @t IS NULL OR @e IS NULL OR @u IS NULL THROW 51200, N'Scope de sesion incompleto.', 1;
    DECLARE @draft SMALLINT = (SELECT TOP 1 id_estado_solicitud_compra FROM catalogo.estado_solicitud_compra WHERE codigo='DRAFT');
    BEGIN TRAN;
    INSERT INTO compras.solicitud(id_tenant,id_empresa,id_unidad_organizativa,numero_solicitud,fecha_solicitud,id_estado_solicitud_compra,creado_por,creado_utc,actualizado_por,actualizado_utc,observaciones,total_estimado,activo)
    VALUES(@t,@e,@id_unidad_organizativa,N'TMP',CAST(@fecha_solicitud AS DATE),@draft,@u,COALESCE(@creado_utc,SYSUTCDATETIME()),@u,SYSUTCDATETIME(),@observaciones,0,1);
    DECLARE @id BIGINT = SCOPE_IDENTITY();
    UPDATE compras.solicitud SET numero_solicitud = CONCAT(N'PR-', @e, N'-', FORMAT(@id,'000000')) WHERE id_solicitud=@id;
    EXEC cumplimiento.usp_auditoria_operacion_registrar @tabla='compras.solicitud',@operacion='INSERT',@id_registro=@id,@valores_anteriores=NULL,@valores_nuevos=NULL,@usuario=@audit_usuario,@correlation_id=@s,@id_usuario=@u,@id_sesion_usuario=@s,@ip_origen=NULL,@agente_usuario=NULL,@resultado=N'OK',@origen_aplicacion=N'compras.usp_solicitud_crear_borrador';
    COMMIT TRAN;
    SELECT @id AS id;
END;
GO

IF OBJECT_ID(N'compras.usp_solicitud_obtener_por_id', N'P') IS NULL EXEC(N'CREATE PROCEDURE compras.usp_solicitud_obtener_por_id AS BEGIN SET NOCOUNT ON; END');
GO
CREATE OR ALTER PROCEDURE compras.usp_solicitud_obtener_por_id
    @id_solicitud BIGINT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @t BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
    DECLARE @e BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
    IF @t IS NULL OR @e IS NULL THROW 51210, N'Scope de sesion incompleto.', 1;
    SELECT s.id_solicitud,s.id_tenant,s.id_empresa,s.id_unidad_organizativa,s.numero_solicitud,s.fecha_solicitud,s.id_estado_solicitud_compra,s.creado_por,s.creado_utc,s.actualizado_por,s.actualizado_utc,s.observaciones,s.total_estimado,s.activo,
           d.id_solicitud_detalle,d.linea,d.descripcion,d.cantidad,d.costo_estimado_unitario,d.total_estimado_linea,d.centro_costo_codigo,d.activo AS detalle_activo,d.creado_utc AS detalle_creado_utc,d.actualizado_utc AS detalle_actualizado_utc
    FROM compras.solicitud s
    LEFT JOIN compras.solicitud_detalle d ON d.id_solicitud=s.id_solicitud AND d.activo=1
    WHERE s.id_solicitud=@id_solicitud AND s.id_tenant=@t AND s.id_empresa=@e AND s.activo=1
    ORDER BY d.linea;
END;
GO

IF OBJECT_ID(N'compras.usp_solicitud_listar', N'P') IS NULL EXEC(N'CREATE PROCEDURE compras.usp_solicitud_listar AS BEGIN SET NOCOUNT ON; END');
GO
CREATE OR ALTER PROCEDURE compras.usp_solicitud_listar
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @t BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
    DECLARE @e BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
    IF @t IS NULL OR @e IS NULL THROW 51220, N'Scope de sesion incompleto.', 1;
    SELECT id_solicitud,numero_solicitud,fecha_solicitud,id_estado_solicitud_compra,creado_por,total_estimado,activo,creado_utc,actualizado_utc
    FROM compras.solicitud
    WHERE id_tenant=@t AND id_empresa=@e AND activo=1
    ORDER BY id_solicitud DESC;
END;
GO

IF OBJECT_ID(N'compras.usp_solicitud_actualizar_borrador', N'P') IS NULL EXEC(N'CREATE PROCEDURE compras.usp_solicitud_actualizar_borrador AS BEGIN SET NOCOUNT ON; END');
GO
CREATE OR ALTER PROCEDURE compras.usp_solicitud_actualizar_borrador
    @id_solicitud BIGINT,
    @id_unidad_organizativa BIGINT = NULL,
    @fecha_solicitud DATETIME2,
    @observaciones NVARCHAR(1000) = NULL,
    @actualizado_por BIGINT,
    @actualizado_utc DATETIME2
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @t BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
    DECLARE @e BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
    DECLARE @u BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT);
    DECLARE @s UNIQUEIDENTIFIER = TRY_CAST(SESSION_CONTEXT(N'id_sesion') AS UNIQUEIDENTIFIER);
    DECLARE @audit_usuario VARCHAR(100) = CONVERT(VARCHAR(100), @u);
    DECLARE @draft SMALLINT = (SELECT TOP 1 id_estado_solicitud_compra FROM catalogo.estado_solicitud_compra WHERE codigo='DRAFT');
    IF @t IS NULL OR @e IS NULL OR @u IS NULL BEGIN SELECT CAST(0 AS BIT) AS ok,N'SESSION_CONTEXT_REQUIRED' AS error_code,N'Valid session context is required.' AS error_message; RETURN; END;
    UPDATE compras.solicitud
    SET id_unidad_organizativa=@id_unidad_organizativa,fecha_solicitud=CAST(@fecha_solicitud AS DATE),observaciones=@observaciones,actualizado_por=@u,actualizado_utc=COALESCE(@actualizado_utc,SYSUTCDATETIME())
    WHERE id_solicitud=@id_solicitud AND id_tenant=@t AND id_empresa=@e AND id_estado_solicitud_compra=@draft AND activo=1;
    IF @@ROWCOUNT = 0 BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_REQUEST_UPDATE_NOT_ALLOWED' AS error_code,N'Only draft purchase requests can be updated.' AS error_message; RETURN; END;
    EXEC cumplimiento.usp_auditoria_operacion_registrar @tabla='compras.solicitud',@operacion='UPDATE',@id_registro=@id_solicitud,@valores_anteriores=NULL,@valores_nuevos=NULL,@usuario=@audit_usuario,@correlation_id=@s,@id_usuario=@u,@id_sesion_usuario=@s,@ip_origen=NULL,@agente_usuario=NULL,@resultado=N'OK',@origen_aplicacion=N'compras.usp_solicitud_actualizar_borrador';
    SELECT CAST(1 AS BIT) AS ok,CAST(NULL AS NVARCHAR(80)) AS error_code,CAST(NULL AS NVARCHAR(300)) AS error_message;
END;
GO

IF OBJECT_ID(N'compras.usp_solicitud_detalle_guardar_borrador', N'P') IS NULL EXEC(N'CREATE PROCEDURE compras.usp_solicitud_detalle_guardar_borrador AS BEGIN SET NOCOUNT ON; END');
GO
CREATE OR ALTER PROCEDURE compras.usp_solicitud_detalle_guardar_borrador
    @id_solicitud BIGINT,
    @id_solicitud_detalle BIGINT = NULL,
    @linea INT = NULL,
    @descripcion NVARCHAR(400),
    @cantidad DECIMAL(18,4),
    @costo_estimado_unitario DECIMAL(18,4),
    @centro_costo_codigo NVARCHAR(50) = NULL,
    @actualizado_por BIGINT,
    @actualizado_utc DATETIME2
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @t BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
    DECLARE @e BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
    DECLARE @u BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT);
    DECLARE @s UNIQUEIDENTIFIER = TRY_CAST(SESSION_CONTEXT(N'id_sesion') AS UNIQUEIDENTIFIER);
    DECLARE @audit_usuario VARCHAR(100) = CONVERT(VARCHAR(100), @u);
    DECLARE @draft SMALLINT = (SELECT TOP 1 id_estado_solicitud_compra FROM catalogo.estado_solicitud_compra WHERE codigo='DRAFT');
    IF @t IS NULL OR @e IS NULL OR @u IS NULL BEGIN SELECT CAST(0 AS BIT) AS ok,N'SESSION_CONTEXT_REQUIRED' AS error_code,N'Valid session context is required.' AS error_message; RETURN; END;
    IF @cantidad <= 0 OR @costo_estimado_unitario < 0 OR LEN(LTRIM(RTRIM(ISNULL(@descripcion,N'')))) = 0 BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_REQUEST_DETAIL_VALUES_INVALID' AS error_code,N'Detail quantity and amount are invalid.' AS error_message; RETURN; END;
    IF NOT EXISTS (SELECT 1 FROM compras.solicitud WHERE id_solicitud=@id_solicitud AND id_tenant=@t AND id_empresa=@e AND id_estado_solicitud_compra=@draft AND activo=1)
    BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_REQUEST_DETAIL_NOT_ALLOWED' AS error_code,N'Only draft purchase requests can update details.' AS error_message; RETURN; END;
    IF @id_solicitud_detalle IS NULL
    BEGIN
        DECLARE @nextLine INT = ISNULL((SELECT MAX(linea) FROM compras.solicitud_detalle WHERE id_solicitud=@id_solicitud),0)+1;
        INSERT INTO compras.solicitud_detalle(id_solicitud,id_tenant,id_empresa,linea,descripcion,cantidad,costo_estimado_unitario,centro_costo_codigo,activo,creado_utc,actualizado_utc)
        VALUES(@id_solicitud,@t,@e,COALESCE(@linea,@nextLine),@descripcion,@cantidad,@costo_estimado_unitario,@centro_costo_codigo,1,SYSUTCDATETIME(),COALESCE(@actualizado_utc,SYSUTCDATETIME()));
    END
    ELSE
    BEGIN
        UPDATE d
        SET d.linea=COALESCE(@linea,d.linea),d.descripcion=@descripcion,d.cantidad=@cantidad,d.costo_estimado_unitario=@costo_estimado_unitario,d.centro_costo_codigo=@centro_costo_codigo,d.actualizado_utc=COALESCE(@actualizado_utc,SYSUTCDATETIME())
        FROM compras.solicitud_detalle d INNER JOIN compras.solicitud s ON s.id_solicitud=d.id_solicitud
        WHERE d.id_solicitud_detalle=@id_solicitud_detalle AND d.id_solicitud=@id_solicitud AND d.id_tenant=@t AND d.id_empresa=@e AND d.activo=1 AND s.id_estado_solicitud_compra=@draft;
        IF @@ROWCOUNT = 0 BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_REQUEST_DETAIL_NOT_ALLOWED' AS error_code,N'Only draft purchase requests can update details.' AS error_message; RETURN; END;
    END;
    UPDATE compras.solicitud
    SET total_estimado = ISNULL((SELECT SUM(cantidad*costo_estimado_unitario) FROM compras.solicitud_detalle WHERE id_solicitud=@id_solicitud AND activo=1),0),actualizado_por=@u,actualizado_utc=SYSUTCDATETIME()
    WHERE id_solicitud=@id_solicitud;
    EXEC cumplimiento.usp_auditoria_operacion_registrar @tabla='compras.solicitud_detalle',@operacion='UPSERT',@id_registro=@id_solicitud,@valores_anteriores=NULL,@valores_nuevos=NULL,@usuario=@audit_usuario,@correlation_id=@s,@id_usuario=@u,@id_sesion_usuario=@s,@ip_origen=NULL,@agente_usuario=NULL,@resultado=N'OK',@origen_aplicacion=N'compras.usp_solicitud_detalle_guardar_borrador';
    SELECT CAST(1 AS BIT) AS ok,CAST(NULL AS NVARCHAR(80)) AS error_code,CAST(NULL AS NVARCHAR(300)) AS error_message;
END;
GO

IF OBJECT_ID(N'compras.usp_solicitud_enviar', N'P') IS NULL EXEC(N'CREATE PROCEDURE compras.usp_solicitud_enviar AS BEGIN SET NOCOUNT ON; END');
GO
CREATE OR ALTER PROCEDURE compras.usp_solicitud_enviar
    @id_solicitud BIGINT,
    @usuario_operacion BIGINT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @t BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
    DECLARE @e BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
    DECLARE @u BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT);
    DECLARE @s UNIQUEIDENTIFIER = TRY_CAST(SESSION_CONTEXT(N'id_sesion') AS UNIQUEIDENTIFIER);
    DECLARE @audit_usuario VARCHAR(100) = CONVERT(VARCHAR(100), @u);
    DECLARE @draft SMALLINT = (SELECT TOP 1 id_estado_solicitud_compra FROM catalogo.estado_solicitud_compra WHERE codigo='DRAFT');
    DECLARE @submitted SMALLINT = (SELECT TOP 1 id_estado_solicitud_compra FROM catalogo.estado_solicitud_compra WHERE codigo='SUBMITTED');
    IF @t IS NULL OR @e IS NULL OR @u IS NULL BEGIN SELECT CAST(0 AS BIT) AS ok,N'SESSION_CONTEXT_REQUIRED' AS error_code,N'Valid session context is required.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id; RETURN; END;
    IF NOT EXISTS (SELECT 1 FROM compras.solicitud_detalle WHERE id_solicitud=@id_solicitud AND id_tenant=@t AND id_empresa=@e AND activo=1)
    BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_REQUEST_DETAILS_REQUIRED' AS error_code,N'At least one detail line is required to submit.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id; RETURN; END;
    UPDATE compras.solicitud SET id_estado_solicitud_compra=@submitted,actualizado_por=@u,actualizado_utc=SYSUTCDATETIME()
    WHERE id_solicitud=@id_solicitud AND id_tenant=@t AND id_empresa=@e AND id_estado_solicitud_compra=@draft AND activo=1;
    IF @@ROWCOUNT = 0 BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_REQUEST_SUBMIT_NOT_ALLOWED' AS error_code,N'Only draft purchase requests can be submitted.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id; RETURN; END;
    EXEC cumplimiento.usp_auditoria_operacion_registrar @tabla='compras.solicitud',@operacion='SUBMIT',@id_registro=@id_solicitud,@valores_anteriores=NULL,@valores_nuevos=NULL,@usuario=@audit_usuario,@correlation_id=@s,@id_usuario=@u,@id_sesion_usuario=@s,@ip_origen=NULL,@agente_usuario=NULL,@resultado=N'OK',@origen_aplicacion=N'compras.usp_solicitud_enviar';
    SELECT CAST(1 AS BIT) AS ok,CAST(NULL AS NVARCHAR(80)) AS error_code,CAST(NULL AS NVARCHAR(300)) AS error_message,@submitted AS new_state_id;
END;
GO

IF OBJECT_ID(N'compras.usp_solicitud_aprobar', N'P') IS NULL EXEC(N'CREATE PROCEDURE compras.usp_solicitud_aprobar AS BEGIN SET NOCOUNT ON; END');
GO
CREATE OR ALTER PROCEDURE compras.usp_solicitud_aprobar
    @id_solicitud BIGINT,
    @usuario_operacion BIGINT,
    @comentario NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @t BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
    DECLARE @e BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
    DECLARE @u BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT);
    DECLARE @session UNIQUEIDENTIFIER = TRY_CAST(SESSION_CONTEXT(N'id_sesion') AS UNIQUEIDENTIFIER);
    DECLARE @audit_usuario VARCHAR(100) = CONVERT(VARCHAR(100), @u);
    DECLARE @submitted SMALLINT = (SELECT TOP 1 id_estado_solicitud_compra FROM catalogo.estado_solicitud_compra WHERE codigo='SUBMITTED');
    DECLARE @approved SMALLINT = (SELECT TOP 1 id_estado_solicitud_compra FROM catalogo.estado_solicitud_compra WHERE codigo='APPROVED');
    IF @t IS NULL OR @e IS NULL OR @u IS NULL OR @session IS NULL BEGIN SELECT CAST(0 AS BIT) AS ok,N'SESSION_CONTEXT_REQUIRED' AS error_code,N'Valid session context is required.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id; RETURN; END;
    IF NOT EXISTS (SELECT 1 FROM seguridad.sesion_usuario WHERE id_sesion_usuario=@session AND id_tenant=@t AND id_empresa=@e AND id_usuario=@u AND mfa_validado=1 AND activo=1 AND revocada_utc IS NULL)
    BEGIN
        EXEC seguridad.usp_security_event_write @event_type='MFA_REQUIRED_DENY',@severity='WARNING',@resultado='DENIED',@detalle=N'Purchase request approval denied: MFA required.',@id_tenant=@t,@id_empresa=@e,@id_usuario=@u,@id_sesion_usuario=@session,@auth_flow_id=NULL,@correlation_id=NULL,@ip_origen=NULL,@agente_usuario=NULL;
        SELECT CAST(0 AS BIT) AS ok,N'MFA_REQUIRED' AS error_code,N'MFA validated session is required for approval.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id;
        RETURN;
    END;
    DECLARE @creator BIGINT = (SELECT TOP 1 creado_por FROM compras.solicitud WHERE id_solicitud=@id_solicitud AND id_tenant=@t AND id_empresa=@e AND activo=1);
    IF @creator IS NULL BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_REQUEST_NOT_FOUND' AS error_code,N'Purchase request was not found.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id; RETURN; END;
    IF @creator = @u
    BEGIN
        EXEC seguridad.usp_security_event_write @event_type='SOD_DENY',@severity='WARNING',@resultado='DENIED',@detalle=N'Purchase request approval denied by SoD (creator cannot approve).',@id_tenant=@t,@id_empresa=@e,@id_usuario=@u,@id_sesion_usuario=@session,@auth_flow_id=NULL,@correlation_id=NULL,@ip_origen=NULL,@agente_usuario=NULL;
        EXEC cumplimiento.usp_auditoria_operacion_registrar @tabla='compras.solicitud',@operacion='APPROVE_DENY',@id_registro=@id_solicitud,@valores_anteriores=NULL,@valores_nuevos=NULL,@usuario=@audit_usuario,@correlation_id=@session,@id_usuario=@u,@id_sesion_usuario=@session,@ip_origen=NULL,@agente_usuario=NULL,@resultado=N'DENY',@origen_aplicacion=N'compras.usp_solicitud_aprobar';
        SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_REQUEST_SOD_DENY' AS error_code,N'Creator cannot approve the same purchase request.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id;
        RETURN;
    END;
    UPDATE compras.solicitud
    SET id_estado_solicitud_compra=@approved,actualizado_por=@u,actualizado_utc=SYSUTCDATETIME(),
        observaciones=CASE WHEN @comentario IS NULL OR LTRIM(RTRIM(@comentario))='' THEN observaciones WHEN observaciones IS NULL OR LTRIM(RTRIM(observaciones))='' THEN @comentario ELSE CONCAT(observaciones,N' | APPROVE: ',@comentario) END
    WHERE id_solicitud=@id_solicitud AND id_tenant=@t AND id_empresa=@e AND id_estado_solicitud_compra=@submitted AND activo=1;
    IF @@ROWCOUNT = 0 BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_REQUEST_APPROVE_NOT_ALLOWED' AS error_code,N'Only submitted purchase requests can be approved.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id; RETURN; END;
    EXEC cumplimiento.usp_auditoria_operacion_registrar @tabla='compras.solicitud',@operacion='APPROVE',@id_registro=@id_solicitud,@valores_anteriores=NULL,@valores_nuevos=NULL,@usuario=@audit_usuario,@correlation_id=@session,@id_usuario=@u,@id_sesion_usuario=@session,@ip_origen=NULL,@agente_usuario=NULL,@resultado=N'OK',@origen_aplicacion=N'compras.usp_solicitud_aprobar';
    EXEC seguridad.usp_security_event_write @event_type='PURCHASE_REQUEST_APPROVED',@severity='INFO',@resultado='OK',@detalle=N'Purchase request approved.',@id_tenant=@t,@id_empresa=@e,@id_usuario=@u,@id_sesion_usuario=@session,@auth_flow_id=NULL,@correlation_id=NULL,@ip_origen=NULL,@agente_usuario=NULL;
    SELECT CAST(1 AS BIT) AS ok,CAST(NULL AS NVARCHAR(80)) AS error_code,CAST(NULL AS NVARCHAR(300)) AS error_message,@approved AS new_state_id;
END;
GO
