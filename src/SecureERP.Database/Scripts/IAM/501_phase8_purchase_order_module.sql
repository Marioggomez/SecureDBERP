SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'compras')
BEGIN
    EXEC(N'CREATE SCHEMA compras AUTHORIZATION dbo;');
END;
GO

IF OBJECT_ID(N'catalogo.estado_orden_compra', N'U') IS NULL
BEGIN
    CREATE TABLE catalogo.estado_orden_compra
    (
        id_estado_orden_compra SMALLINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_estado_orden_compra PRIMARY KEY,
        codigo VARCHAR(30) NOT NULL,
        nombre NVARCHAR(120) NOT NULL,
        descripcion NVARCHAR(400) NULL,
        orden_visual SMALLINT NOT NULL,
        activo BIT NOT NULL CONSTRAINT DF_estado_orden_compra_activo DEFAULT(1),
        creado_utc DATETIME2(7) NOT NULL CONSTRAINT DF_estado_orden_compra_creado_utc DEFAULT(SYSUTCDATETIME()),
        actualizado_utc DATETIME2(7) NULL
    );
    CREATE UNIQUE INDEX UX_estado_orden_compra_codigo ON catalogo.estado_orden_compra(codigo);
END;
GO

IF NOT EXISTS (SELECT 1 FROM catalogo.estado_orden_compra WHERE codigo='DRAFT')
    INSERT INTO catalogo.estado_orden_compra(codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES('DRAFT',N'Borrador',N'Solicitud editable en preparacion.',1,1,SYSUTCDATETIME(),NULL);
GO
IF NOT EXISTS (SELECT 1 FROM catalogo.estado_orden_compra WHERE codigo='SUBMITTED')
    INSERT INTO catalogo.estado_orden_compra(codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES('SUBMITTED',N'Enviada',N'Solicitud enviada para aprobacion.',2,1,SYSUTCDATETIME(),NULL);
GO
IF NOT EXISTS (SELECT 1 FROM catalogo.estado_orden_compra WHERE codigo='APPROVED')
    INSERT INTO catalogo.estado_orden_compra(codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES('APPROVED',N'Aprobada',N'Solicitud aprobada.',3,1,SYSUTCDATETIME(),NULL);
GO
IF NOT EXISTS (SELECT 1 FROM catalogo.estado_orden_compra WHERE codigo='REJECTED')
    INSERT INTO catalogo.estado_orden_compra(codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES('REJECTED',N'Rechazada',N'Solicitud rechazada.',4,1,SYSUTCDATETIME(),NULL);
GO
IF NOT EXISTS (SELECT 1 FROM catalogo.estado_orden_compra WHERE codigo='CANCELLED')
    INSERT INTO catalogo.estado_orden_compra(codigo,nombre,descripcion,orden_visual,activo,creado_utc,actualizado_utc) VALUES('CANCELLED',N'Cancelada',N'Solicitud cancelada.',5,1,SYSUTCDATETIME(),NULL);
GO

IF OBJECT_ID(N'compras.orden_compra', N'U') IS NULL
BEGIN
    CREATE TABLE compras.orden_compra
    (
        id_orden_compra BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_compras_orden_compra PRIMARY KEY,
        id_tenant BIGINT NOT NULL,
        id_empresa BIGINT NOT NULL,
        id_unidad_organizativa BIGINT NULL,
        numero_orden_compra NVARCHAR(40) NOT NULL,
        fecha_orden DATE NOT NULL,
        id_estado_orden_compra SMALLINT NOT NULL,
        creado_por BIGINT NOT NULL,
        creado_utc DATETIME2(7) NOT NULL,
        actualizado_por BIGINT NULL,
        actualizado_utc DATETIME2(7) NULL,
        observaciones NVARCHAR(1000) NULL,
        total_orden DECIMAL(18,4) NOT NULL CONSTRAINT DF_compras_orden_compra_total_orden DEFAULT(0),
        activo BIT NOT NULL CONSTRAINT DF_compras_orden_compra_activo DEFAULT(1),
        version_fila ROWVERSION NOT NULL,
        CONSTRAINT FK_compras_orden_compra_tenant FOREIGN KEY (id_tenant) REFERENCES plataforma.tenant(id_tenant),
        CONSTRAINT FK_compras_orden_compra_empresa FOREIGN KEY (id_empresa) REFERENCES organizacion.empresa(id_empresa),
        CONSTRAINT FK_compras_orden_compra_estado FOREIGN KEY (id_estado_orden_compra) REFERENCES catalogo.estado_orden_compra(id_estado_orden_compra)
    );
    CREATE UNIQUE INDEX UX_compras_orden_compra_numero ON compras.orden_compra(id_tenant,id_empresa,numero_orden_compra);
    CREATE INDEX IX_compras_orden_compra_scope ON compras.orden_compra(id_tenant,id_empresa,id_estado_orden_compra,fecha_orden DESC);
END;
GO

IF OBJECT_ID(N'compras.orden_compra_detalle', N'U') IS NULL
BEGIN
    CREATE TABLE compras.orden_compra_detalle
    (
        id_orden_compra_detalle BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_compras_orden_compra_detalle PRIMARY KEY,
        id_orden_compra BIGINT NOT NULL,
        id_tenant BIGINT NOT NULL,
        id_empresa BIGINT NOT NULL,
        linea INT NOT NULL,
        descripcion NVARCHAR(400) NOT NULL,
        cantidad DECIMAL(18,4) NOT NULL,
        costo_unitario DECIMAL(18,4) NOT NULL,
        total_linea AS (ROUND(cantidad * costo_unitario, 4)) PERSISTED,
        centro_costo_codigo NVARCHAR(50) NULL,
        activo BIT NOT NULL CONSTRAINT DF_compras_orden_compra_detalle_activo DEFAULT(1),
        creado_utc DATETIME2(7) NOT NULL,
        actualizado_utc DATETIME2(7) NULL,
        version_fila ROWVERSION NOT NULL,
        CONSTRAINT FK_compras_orden_compra_detalle_solicitud FOREIGN KEY (id_orden_compra) REFERENCES compras.orden_compra(id_orden_compra),
        CONSTRAINT FK_compras_orden_compra_detalle_tenant FOREIGN KEY (id_tenant) REFERENCES plataforma.tenant(id_tenant),
        CONSTRAINT FK_compras_orden_compra_detalle_empresa FOREIGN KEY (id_empresa) REFERENCES organizacion.empresa(id_empresa)
    );
    CREATE UNIQUE INDEX UX_compras_orden_compra_detalle_linea ON compras.orden_compra_detalle(id_orden_compra,linea);
END;
GO

IF OBJECT_ID(N'seguridad.fn_rls_tenant_empresa', N'IF') IS NOT NULL
AND EXISTS (SELECT 1 FROM sys.security_policies WHERE name=N'RLS_scope_tenant_empresa' AND SCHEMA_NAME(schema_id)=N'seguridad')
BEGIN
    DECLARE @policyId INT = OBJECT_ID(N'seguridad.RLS_scope_tenant_empresa');

    IF OBJECT_ID(N'compras.orden_compra', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.security_predicates WHERE object_id=@policyId AND target_object_id=OBJECT_ID(N'compras.orden_compra') AND operation IS NULL)
            EXEC(N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa ADD FILTER PREDICATE seguridad.fn_rls_tenant_empresa(id_tenant,id_empresa) ON compras.orden_compra;');
        IF NOT EXISTS (SELECT 1 FROM sys.security_predicates WHERE object_id=@policyId AND target_object_id=OBJECT_ID(N'compras.orden_compra') AND operation_desc=N'AFTER INSERT')
            EXEC(N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa ADD BLOCK PREDICATE seguridad.fn_rls_tenant_empresa(id_tenant,id_empresa) ON compras.orden_compra AFTER INSERT;');
        IF NOT EXISTS (SELECT 1 FROM sys.security_predicates WHERE object_id=@policyId AND target_object_id=OBJECT_ID(N'compras.orden_compra') AND operation_desc=N'AFTER UPDATE')
            EXEC(N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa ADD BLOCK PREDICATE seguridad.fn_rls_tenant_empresa(id_tenant,id_empresa) ON compras.orden_compra AFTER UPDATE;');
    END;

    IF OBJECT_ID(N'compras.orden_compra_detalle', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.security_predicates WHERE object_id=@policyId AND target_object_id=OBJECT_ID(N'compras.orden_compra_detalle') AND operation IS NULL)
            EXEC(N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa ADD FILTER PREDICATE seguridad.fn_rls_tenant_empresa(id_tenant,id_empresa) ON compras.orden_compra_detalle;');
        IF NOT EXISTS (SELECT 1 FROM sys.security_predicates WHERE object_id=@policyId AND target_object_id=OBJECT_ID(N'compras.orden_compra_detalle') AND operation_desc=N'AFTER INSERT')
            EXEC(N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa ADD BLOCK PREDICATE seguridad.fn_rls_tenant_empresa(id_tenant,id_empresa) ON compras.orden_compra_detalle AFTER INSERT;');
        IF NOT EXISTS (SELECT 1 FROM sys.security_predicates WHERE object_id=@policyId AND target_object_id=OBJECT_ID(N'compras.orden_compra_detalle') AND operation_desc=N'AFTER UPDATE')
            EXEC(N'ALTER SECURITY POLICY seguridad.RLS_scope_tenant_empresa ADD BLOCK PREDICATE seguridad.fn_rls_tenant_empresa(id_tenant,id_empresa) ON compras.orden_compra_detalle AFTER UPDATE;');
    END;
END;
GO

IF OBJECT_ID(N'compras.usp_orden_compra_crear_borrador', N'P') IS NULL EXEC(N'CREATE PROCEDURE compras.usp_orden_compra_crear_borrador AS BEGIN SET NOCOUNT ON; END');
GO
CREATE OR ALTER PROCEDURE compras.usp_orden_compra_crear_borrador
    @id_unidad_organizativa BIGINT = NULL,
    @fecha_orden DATETIME2,
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
    DECLARE @draft SMALLINT = (SELECT TOP 1 id_estado_orden_compra FROM catalogo.estado_orden_compra WHERE codigo='DRAFT');
    BEGIN TRAN;
    INSERT INTO compras.orden_compra(id_tenant,id_empresa,id_unidad_organizativa,numero_orden_compra,fecha_orden,id_estado_orden_compra,creado_por,creado_utc,actualizado_por,actualizado_utc,observaciones,total_orden,activo)
    VALUES(@t,@e,@id_unidad_organizativa,N'TMP',CAST(@fecha_orden AS DATE),@draft,@u,COALESCE(@creado_utc,SYSUTCDATETIME()),@u,SYSUTCDATETIME(),@observaciones,0,1);
    DECLARE @id BIGINT = SCOPE_IDENTITY();
    UPDATE compras.orden_compra SET numero_orden_compra = CONCAT(N'PR-', @e, N'-', FORMAT(@id,'000000')) WHERE id_orden_compra=@id;
    EXEC cumplimiento.usp_auditoria_operacion_registrar @tabla='compras.orden_compra',@operacion='INSERT',@id_registro=@id,@valores_anteriores=NULL,@valores_nuevos=NULL,@usuario=@audit_usuario,@correlation_id=@s,@id_usuario=@u,@id_sesion_usuario=@s,@ip_origen=NULL,@agente_usuario=NULL,@resultado=N'OK',@origen_aplicacion=N'compras.usp_orden_compra_crear_borrador';
    COMMIT TRAN;
    SELECT @id AS id;
END;
GO

IF OBJECT_ID(N'compras.usp_orden_compra_obtener_por_id', N'P') IS NULL EXEC(N'CREATE PROCEDURE compras.usp_orden_compra_obtener_por_id AS BEGIN SET NOCOUNT ON; END');
GO
CREATE OR ALTER PROCEDURE compras.usp_orden_compra_obtener_por_id
    @id_orden_compra BIGINT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @t BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
    DECLARE @e BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
    IF @t IS NULL OR @e IS NULL THROW 51210, N'Scope de sesion incompleto.', 1;
    SELECT s.id_orden_compra,s.id_tenant,s.id_empresa,s.id_unidad_organizativa,s.numero_orden_compra,s.fecha_orden,s.id_estado_orden_compra,s.creado_por,s.creado_utc,s.actualizado_por,s.actualizado_utc,s.observaciones,s.total_orden,s.activo,
           d.id_orden_compra_detalle,d.linea,d.descripcion,d.cantidad,d.costo_unitario,d.total_linea,d.centro_costo_codigo,d.activo AS detalle_activo,d.creado_utc AS detalle_creado_utc,d.actualizado_utc AS detalle_actualizado_utc
    FROM compras.orden_compra s
    LEFT JOIN compras.orden_compra_detalle d ON d.id_orden_compra=s.id_orden_compra AND d.activo=1
    WHERE s.id_orden_compra=@id_orden_compra AND s.id_tenant=@t AND s.id_empresa=@e AND s.activo=1
    ORDER BY d.linea;
END;
GO

IF OBJECT_ID(N'compras.usp_orden_compra_listar', N'P') IS NULL EXEC(N'CREATE PROCEDURE compras.usp_orden_compra_listar AS BEGIN SET NOCOUNT ON; END');
GO
CREATE OR ALTER PROCEDURE compras.usp_orden_compra_listar
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @t BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
    DECLARE @e BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
    IF @t IS NULL OR @e IS NULL THROW 51220, N'Scope de sesion incompleto.', 1;
    SELECT id_orden_compra,numero_orden_compra,fecha_orden,id_estado_orden_compra,creado_por,total_orden,activo,creado_utc,actualizado_utc
    FROM compras.orden_compra
    WHERE id_tenant=@t AND id_empresa=@e AND activo=1
    ORDER BY id_orden_compra DESC;
END;
GO

IF OBJECT_ID(N'compras.usp_orden_compra_actualizar_borrador', N'P') IS NULL EXEC(N'CREATE PROCEDURE compras.usp_orden_compra_actualizar_borrador AS BEGIN SET NOCOUNT ON; END');
GO
CREATE OR ALTER PROCEDURE compras.usp_orden_compra_actualizar_borrador
    @id_orden_compra BIGINT,
    @id_unidad_organizativa BIGINT = NULL,
    @fecha_orden DATETIME2,
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
    DECLARE @draft SMALLINT = (SELECT TOP 1 id_estado_orden_compra FROM catalogo.estado_orden_compra WHERE codigo='DRAFT');
    IF @t IS NULL OR @e IS NULL OR @u IS NULL BEGIN SELECT CAST(0 AS BIT) AS ok,N'SESSION_CONTEXT_REQUIRED' AS error_code,N'Valid session context is required.' AS error_message; RETURN; END;
    UPDATE compras.orden_compra
    SET id_unidad_organizativa=@id_unidad_organizativa,fecha_orden=CAST(@fecha_orden AS DATE),observaciones=@observaciones,actualizado_por=@u,actualizado_utc=COALESCE(@actualizado_utc,SYSUTCDATETIME())
    WHERE id_orden_compra=@id_orden_compra AND id_tenant=@t AND id_empresa=@e AND id_estado_orden_compra=@draft AND activo=1;
    IF @@ROWCOUNT = 0 BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_ORDER_UPDATE_NOT_ALLOWED' AS error_code,N'Only draft purchase orders can be updated.' AS error_message; RETURN; END;
    EXEC cumplimiento.usp_auditoria_operacion_registrar @tabla='compras.orden_compra',@operacion='UPDATE',@id_registro=@id_orden_compra,@valores_anteriores=NULL,@valores_nuevos=NULL,@usuario=@audit_usuario,@correlation_id=@s,@id_usuario=@u,@id_sesion_usuario=@s,@ip_origen=NULL,@agente_usuario=NULL,@resultado=N'OK',@origen_aplicacion=N'compras.usp_orden_compra_actualizar_borrador';
    SELECT CAST(1 AS BIT) AS ok,CAST(NULL AS NVARCHAR(80)) AS error_code,CAST(NULL AS NVARCHAR(300)) AS error_message;
END;
GO

IF OBJECT_ID(N'compras.usp_orden_compra_detalle_guardar_borrador', N'P') IS NULL EXEC(N'CREATE PROCEDURE compras.usp_orden_compra_detalle_guardar_borrador AS BEGIN SET NOCOUNT ON; END');
GO
CREATE OR ALTER PROCEDURE compras.usp_orden_compra_detalle_guardar_borrador
    @id_orden_compra BIGINT,
    @id_orden_compra_detalle BIGINT = NULL,
    @linea INT = NULL,
    @descripcion NVARCHAR(400),
    @cantidad DECIMAL(18,4),
    @costo_unitario DECIMAL(18,4),
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
    DECLARE @draft SMALLINT = (SELECT TOP 1 id_estado_orden_compra FROM catalogo.estado_orden_compra WHERE codigo='DRAFT');
    IF @t IS NULL OR @e IS NULL OR @u IS NULL BEGIN SELECT CAST(0 AS BIT) AS ok,N'SESSION_CONTEXT_REQUIRED' AS error_code,N'Valid session context is required.' AS error_message; RETURN; END;
    IF @cantidad <= 0 OR @costo_unitario < 0 OR LEN(LTRIM(RTRIM(ISNULL(@descripcion,N'')))) = 0 BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_ORDER_DETAIL_VALUES_INVALID' AS error_code,N'Detail quantity and amount are invalid.' AS error_message; RETURN; END;
    IF NOT EXISTS (SELECT 1 FROM compras.orden_compra WHERE id_orden_compra=@id_orden_compra AND id_tenant=@t AND id_empresa=@e AND id_estado_orden_compra=@draft AND activo=1)
    BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_ORDER_DETAIL_NOT_ALLOWED' AS error_code,N'Only draft purchase orders can update details.' AS error_message; RETURN; END;
    IF @id_orden_compra_detalle IS NULL
    BEGIN
        DECLARE @nextLine INT = ISNULL((SELECT MAX(linea) FROM compras.orden_compra_detalle WHERE id_orden_compra=@id_orden_compra),0)+1;
        INSERT INTO compras.orden_compra_detalle(id_orden_compra,id_tenant,id_empresa,linea,descripcion,cantidad,costo_unitario,centro_costo_codigo,activo,creado_utc,actualizado_utc)
        VALUES(@id_orden_compra,@t,@e,COALESCE(@linea,@nextLine),@descripcion,@cantidad,@costo_unitario,@centro_costo_codigo,1,SYSUTCDATETIME(),COALESCE(@actualizado_utc,SYSUTCDATETIME()));
    END
    ELSE
    BEGIN
        UPDATE d
        SET d.linea=COALESCE(@linea,d.linea),d.descripcion=@descripcion,d.cantidad=@cantidad,d.costo_unitario=@costo_unitario,d.centro_costo_codigo=@centro_costo_codigo,d.actualizado_utc=COALESCE(@actualizado_utc,SYSUTCDATETIME())
        FROM compras.orden_compra_detalle d INNER JOIN compras.orden_compra s ON s.id_orden_compra=d.id_orden_compra
        WHERE d.id_orden_compra_detalle=@id_orden_compra_detalle AND d.id_orden_compra=@id_orden_compra AND d.id_tenant=@t AND d.id_empresa=@e AND d.activo=1 AND s.id_estado_orden_compra=@draft;
        IF @@ROWCOUNT = 0 BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_ORDER_DETAIL_NOT_ALLOWED' AS error_code,N'Only draft purchase orders can update details.' AS error_message; RETURN; END;
    END;
    UPDATE compras.orden_compra
    SET total_orden = ISNULL((SELECT SUM(cantidad*costo_unitario) FROM compras.orden_compra_detalle WHERE id_orden_compra=@id_orden_compra AND activo=1),0),actualizado_por=@u,actualizado_utc=SYSUTCDATETIME()
    WHERE id_orden_compra=@id_orden_compra;
    EXEC cumplimiento.usp_auditoria_operacion_registrar @tabla='compras.orden_compra_detalle',@operacion='UPSERT',@id_registro=@id_orden_compra,@valores_anteriores=NULL,@valores_nuevos=NULL,@usuario=@audit_usuario,@correlation_id=@s,@id_usuario=@u,@id_sesion_usuario=@s,@ip_origen=NULL,@agente_usuario=NULL,@resultado=N'OK',@origen_aplicacion=N'compras.usp_orden_compra_detalle_guardar_borrador';
    SELECT CAST(1 AS BIT) AS ok,CAST(NULL AS NVARCHAR(80)) AS error_code,CAST(NULL AS NVARCHAR(300)) AS error_message;
END;
GO

IF OBJECT_ID(N'compras.usp_orden_compra_enviar', N'P') IS NULL EXEC(N'CREATE PROCEDURE compras.usp_orden_compra_enviar AS BEGIN SET NOCOUNT ON; END');
GO
CREATE OR ALTER PROCEDURE compras.usp_orden_compra_enviar
    @id_orden_compra BIGINT,
    @usuario_operacion BIGINT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @t BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
    DECLARE @e BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
    DECLARE @u BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT);
    DECLARE @s UNIQUEIDENTIFIER = TRY_CAST(SESSION_CONTEXT(N'id_sesion') AS UNIQUEIDENTIFIER);
    DECLARE @audit_usuario VARCHAR(100) = CONVERT(VARCHAR(100), @u);
    DECLARE @draft SMALLINT = (SELECT TOP 1 id_estado_orden_compra FROM catalogo.estado_orden_compra WHERE codigo='DRAFT');
    DECLARE @submitted SMALLINT = (SELECT TOP 1 id_estado_orden_compra FROM catalogo.estado_orden_compra WHERE codigo='SUBMITTED');
    IF @t IS NULL OR @e IS NULL OR @u IS NULL BEGIN SELECT CAST(0 AS BIT) AS ok,N'SESSION_CONTEXT_REQUIRED' AS error_code,N'Valid session context is required.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id; RETURN; END;
    IF NOT EXISTS (SELECT 1 FROM compras.orden_compra_detalle WHERE id_orden_compra=@id_orden_compra AND id_tenant=@t AND id_empresa=@e AND activo=1)
    BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_ORDER_DETAILS_REQUIRED' AS error_code,N'At least one detail line is required to submit.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id; RETURN; END;
    UPDATE compras.orden_compra SET id_estado_orden_compra=@submitted,actualizado_por=@u,actualizado_utc=SYSUTCDATETIME()
    WHERE id_orden_compra=@id_orden_compra AND id_tenant=@t AND id_empresa=@e AND id_estado_orden_compra=@draft AND activo=1;
    IF @@ROWCOUNT = 0 BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_ORDER_SUBMIT_NOT_ALLOWED' AS error_code,N'Only draft purchase orders can be submitted.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id; RETURN; END;
    EXEC cumplimiento.usp_auditoria_operacion_registrar @tabla='compras.orden_compra',@operacion='SUBMIT',@id_registro=@id_orden_compra,@valores_anteriores=NULL,@valores_nuevos=NULL,@usuario=@audit_usuario,@correlation_id=@s,@id_usuario=@u,@id_sesion_usuario=@s,@ip_origen=NULL,@agente_usuario=NULL,@resultado=N'OK',@origen_aplicacion=N'compras.usp_orden_compra_enviar';
    SELECT CAST(1 AS BIT) AS ok,CAST(NULL AS NVARCHAR(80)) AS error_code,CAST(NULL AS NVARCHAR(300)) AS error_message,@submitted AS new_state_id;
END;
GO

IF OBJECT_ID(N'compras.usp_orden_compra_aprobar', N'P') IS NULL EXEC(N'CREATE PROCEDURE compras.usp_orden_compra_aprobar AS BEGIN SET NOCOUNT ON; END');
GO
CREATE OR ALTER PROCEDURE compras.usp_orden_compra_aprobar
    @id_orden_compra BIGINT,
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
    DECLARE @submitted SMALLINT = (SELECT TOP 1 id_estado_orden_compra FROM catalogo.estado_orden_compra WHERE codigo='SUBMITTED');
    DECLARE @approved SMALLINT = (SELECT TOP 1 id_estado_orden_compra FROM catalogo.estado_orden_compra WHERE codigo='APPROVED');
    IF @t IS NULL OR @e IS NULL OR @u IS NULL OR @session IS NULL BEGIN SELECT CAST(0 AS BIT) AS ok,N'SESSION_CONTEXT_REQUIRED' AS error_code,N'Valid session context is required.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id; RETURN; END;
    IF NOT EXISTS (SELECT 1 FROM seguridad.sesion_usuario WHERE id_sesion_usuario=@session AND id_tenant=@t AND id_empresa=@e AND id_usuario=@u AND mfa_validado=1 AND activo=1 AND revocada_utc IS NULL)
    BEGIN
        EXEC seguridad.usp_security_event_write @event_type='MFA_REQUIRED_DENY',@severity='WARNING',@resultado='DENIED',@detalle=N'Purchase order approval denied: MFA required.',@id_tenant=@t,@id_empresa=@e,@id_usuario=@u,@id_sesion_usuario=@session,@auth_flow_id=NULL,@correlation_id=NULL,@ip_origen=NULL,@agente_usuario=NULL;
        SELECT CAST(0 AS BIT) AS ok,N'MFA_REQUIRED' AS error_code,N'MFA validated session is required for approval.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id;
        RETURN;
    END;
    DECLARE @creator BIGINT = (SELECT TOP 1 creado_por FROM compras.orden_compra WHERE id_orden_compra=@id_orden_compra AND id_tenant=@t AND id_empresa=@e AND activo=1);
    IF @creator IS NULL BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_ORDER_NOT_FOUND' AS error_code,N'Purchase order was not found.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id; RETURN; END;
    IF @creator = @u
    BEGIN
        EXEC seguridad.usp_security_event_write @event_type='SOD_DENY',@severity='WARNING',@resultado='DENIED',@detalle=N'Purchase order approval denied by SoD (creator cannot approve).',@id_tenant=@t,@id_empresa=@e,@id_usuario=@u,@id_sesion_usuario=@session,@auth_flow_id=NULL,@correlation_id=NULL,@ip_origen=NULL,@agente_usuario=NULL;
        EXEC cumplimiento.usp_auditoria_operacion_registrar @tabla='compras.orden_compra',@operacion='APPROVE_DENY',@id_registro=@id_orden_compra,@valores_anteriores=NULL,@valores_nuevos=NULL,@usuario=@audit_usuario,@correlation_id=@session,@id_usuario=@u,@id_sesion_usuario=@session,@ip_origen=NULL,@agente_usuario=NULL,@resultado=N'DENY',@origen_aplicacion=N'compras.usp_orden_compra_aprobar';
        SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_ORDER_SOD_DENY' AS error_code,N'Creator cannot approve the same purchase order.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id;
        RETURN;
    END;
    UPDATE compras.orden_compra
    SET id_estado_orden_compra=@approved,actualizado_por=@u,actualizado_utc=SYSUTCDATETIME(),
        observaciones=CASE WHEN @comentario IS NULL OR LTRIM(RTRIM(@comentario))='' THEN observaciones WHEN observaciones IS NULL OR LTRIM(RTRIM(observaciones))='' THEN @comentario ELSE CONCAT(observaciones,N' | APPROVE: ',@comentario) END
    WHERE id_orden_compra=@id_orden_compra AND id_tenant=@t AND id_empresa=@e AND id_estado_orden_compra=@submitted AND activo=1;
    IF @@ROWCOUNT = 0 BEGIN SELECT CAST(0 AS BIT) AS ok,N'PURCHASE_ORDER_APPROVE_NOT_ALLOWED' AS error_code,N'Only submitted purchase orders can be approved.' AS error_message,CAST(NULL AS SMALLINT) AS new_state_id; RETURN; END;
    EXEC cumplimiento.usp_auditoria_operacion_registrar @tabla='compras.orden_compra',@operacion='APPROVE',@id_registro=@id_orden_compra,@valores_anteriores=NULL,@valores_nuevos=NULL,@usuario=@audit_usuario,@correlation_id=@session,@id_usuario=@u,@id_sesion_usuario=@session,@ip_origen=NULL,@agente_usuario=NULL,@resultado=N'OK',@origen_aplicacion=N'compras.usp_orden_compra_aprobar';
    EXEC seguridad.usp_security_event_write @event_type='PURCHASE_ORDER_APPROVED',@severity='INFO',@resultado='OK',@detalle=N'Purchase order approved.',@id_tenant=@t,@id_empresa=@e,@id_usuario=@u,@id_sesion_usuario=@session,@auth_flow_id=NULL,@correlation_id=NULL,@ip_origen=NULL,@agente_usuario=NULL;
    SELECT CAST(1 AS BIT) AS ok,CAST(NULL AS NVARCHAR(80)) AS error_code,CAST(NULL AS NVARCHAR(300)) AS error_message,@approved AS new_state_id;
END;
GO



