SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'seguridad.catalogo_permiso_oficial', N'U') IS NOT NULL
BEGIN
    MERGE seguridad.catalogo_permiso_oficial AS target
    USING
    (
        VALUES
        (N'PURCHASE.REQUEST.READ', N'PURCHASE', N'REQUEST', N'READ', N'Permite consultar solicitudes de compra por contexto tenant/empresa.', N'Consultar solicitudes de compra.', 0, 0, 1),
        (N'PURCHASE.REQUEST.CREATE', N'PURCHASE', N'REQUEST', N'CREATE', N'Permite crear solicitudes de compra en estado borrador.', N'Crear solicitud de compra.', 0, 1, 1),
        (N'PURCHASE.REQUEST.UPDATE', N'PURCHASE', N'REQUEST', N'UPDATE', N'Permite actualizar cabecera/detalle mientras la solicitud esta en borrador.', N'Actualizar solicitud de compra en borrador.', 0, 1, 1),
        (N'PURCHASE.REQUEST.SUBMIT', N'PURCHASE', N'REQUEST', N'SUBMIT', N'Permite enviar solicitud de compra a flujo de aprobacion.', N'Enviar solicitud de compra.', 0, 1, 1),
        (N'PURCHASE.REQUEST.APPROVE', N'PURCHASE', N'REQUEST', N'APPROVE', N'Permite aprobar solicitud enviada. Requiere MFA y SoD.', N'Aprobar solicitud de compra.', 1, 1, 1)
    ) AS source(codigo, modulo, entidad, accion, descripcion_tecnica, descripcion_funcional, requiere_mfa, es_sensible, activo)
    ON target.codigo = source.codigo
    WHEN MATCHED THEN
        UPDATE
        SET
            target.modulo = source.modulo,
            target.entidad = source.entidad,
            target.accion = source.accion,
            target.descripcion_tecnica = source.descripcion_tecnica,
            target.descripcion_funcional = source.descripcion_funcional,
            target.requiere_mfa = source.requiere_mfa,
            target.es_sensible = source.es_sensible,
            target.activo = source.activo,
            target.actualizado_utc = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT
        (
            codigo, modulo, entidad, accion, descripcion_tecnica, descripcion_funcional,
            requiere_mfa, es_sensible, activo, creado_utc, actualizado_utc
        )
        VALUES
        (
            source.codigo, source.modulo, source.entidad, source.accion, source.descripcion_tecnica, source.descripcion_funcional,
            source.requiere_mfa, source.es_sensible, source.activo, SYSUTCDATETIME(), NULL
        );
END;
GO

MERGE seguridad.permiso AS target
USING
(
    VALUES
    (N'PURCHASE.REQUEST.READ', N'PURCHASE', N'READ', N'PURCHASE.REQUEST.READ', N'Lectura de solicitudes de compra.', 0, 1),
    (N'PURCHASE.REQUEST.CREATE', N'PURCHASE', N'CREATE', N'PURCHASE.REQUEST.CREATE', N'Creacion de solicitudes de compra.', 1, 1),
    (N'PURCHASE.REQUEST.UPDATE', N'PURCHASE', N'UPDATE', N'PURCHASE.REQUEST.UPDATE', N'Actualizacion de solicitudes de compra en borrador.', 1, 1),
    (N'PURCHASE.REQUEST.SUBMIT', N'PURCHASE', N'SUBMIT', N'PURCHASE.REQUEST.SUBMIT', N'Envio de solicitud de compra.', 1, 1),
    (N'PURCHASE.REQUEST.APPROVE', N'PURCHASE', N'APPROVE', N'PURCHASE.REQUEST.APPROVE', N'Aprobacion de solicitud de compra con MFA y SoD.', 1, 1)
) AS source(codigo, modulo, accion, nombre, descripcion, es_sensible, activo)
ON target.codigo = source.codigo
WHEN MATCHED THEN
    UPDATE
    SET
        target.modulo = source.modulo,
        target.accion = source.accion,
        target.nombre = source.nombre,
        target.descripcion = source.descripcion,
        target.es_sensible = source.es_sensible,
        target.activo = source.activo,
        target.actualizado_utc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT
    (
        codigo, modulo, accion, nombre, descripcion, es_sensible, activo, creado_utc, actualizado_utc
    )
    VALUES
    (
        source.codigo, source.modulo, source.accion, source.nombre, source.descripcion, source.es_sensible, source.activo, SYSUTCDATETIME(), NULL
    );
GO

IF OBJECT_ID(N'seguridad.entidad_alcance_dato', N'U') IS NOT NULL
BEGIN
    MERGE seguridad.entidad_alcance_dato AS target
    USING
    (
        VALUES
        (
            N'PURCHASE.REQUEST',
            N'compras.solicitud',
            N'id_solicitud',
            N'id_tenant',
            N'id_empresa',
            N'id_unidad_organizativa',
            NULL,
            NULL,
            N'Entidad de solicitud de compra para autorizacion central y alcance de datos.',
            N'DIRECTO',
            NULL,
            CAST(1 AS BIT)
        )
    ) AS source
    (
        codigo_entidad, nombre_tabla, columna_llave_primaria, columna_tenant, columna_empresa,
        columna_unidad_organizativa, columna_propietario, columna_contexto, descripcion,
        modo_scope, codigo_entidad_raiz, activo
    )
    ON target.codigo_entidad = source.codigo_entidad
    WHEN MATCHED THEN
        UPDATE
        SET
            target.nombre_tabla = source.nombre_tabla,
            target.columna_llave_primaria = source.columna_llave_primaria,
            target.columna_tenant = source.columna_tenant,
            target.columna_empresa = source.columna_empresa,
            target.columna_unidad_organizativa = source.columna_unidad_organizativa,
            target.columna_propietario = source.columna_propietario,
            target.columna_contexto = source.columna_contexto,
            target.descripcion = source.descripcion,
            target.modo_scope = source.modo_scope,
            target.codigo_entidad_raiz = source.codigo_entidad_raiz,
            target.activo = source.activo,
            target.actualizado_utc = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT
        (
            codigo_entidad, nombre_tabla, columna_llave_primaria, columna_tenant, columna_empresa,
            columna_unidad_organizativa, columna_propietario, columna_contexto, descripcion,
            activo, creado_utc, actualizado_utc, modo_scope, codigo_entidad_raiz
        )
        VALUES
        (
            source.codigo_entidad, source.nombre_tabla, source.columna_llave_primaria, source.columna_tenant, source.columna_empresa,
            source.columna_unidad_organizativa, source.columna_propietario, source.columna_contexto, source.descripcion,
            source.activo, SYSUTCDATETIME(), NULL, source.modo_scope, source.codigo_entidad_raiz
        );
END;
GO

IF OBJECT_ID(N'seguridad.operacion_api', N'U') IS NOT NULL
AND OBJECT_ID(N'seguridad.politica_operacion_api', N'U') IS NOT NULL
BEGIN
    DECLARE @operations TABLE
    (
        codigo NVARCHAR(200) NOT NULL,
        modulo NVARCHAR(100) NOT NULL,
        controlador NVARCHAR(200) NULL,
        accion NVARCHAR(120) NOT NULL,
        metodo_http NVARCHAR(20) NOT NULL,
        ruta NVARCHAR(500) NOT NULL,
        descripcion NVARCHAR(800) NULL,
        permiso_codigo NVARCHAR(300) NULL,
        requiere_autenticacion BIT NOT NULL,
        requiere_sesion BIT NOT NULL,
        requiere_empresa BIT NOT NULL,
        requiere_unidad_organizativa BIT NOT NULL,
        requiere_mfa BIT NOT NULL,
        requiere_auditoria BIT NOT NULL,
        requiere_aprobacion BIT NOT NULL,
        codigo_entidad NVARCHAR(120) NULL,
        activo BIT NOT NULL
    );

    INSERT INTO @operations
    (
        codigo, modulo, controlador, accion, metodo_http, ruta, descripcion, permiso_codigo,
        requiere_autenticacion, requiere_sesion, requiere_empresa, requiere_unidad_organizativa,
        requiere_mfa, requiere_auditoria, requiere_aprobacion, codigo_entidad, activo
    )
    VALUES
    (N'PURCHASE.REQUEST.CREATE', N'PURCHASE', N'PurchaseRequestsController', N'Create', N'POST', N'/api/v1/purchase/requests', N'Crear solicitud de compra en borrador.', N'PURCHASE.REQUEST.CREATE', 1, 1, 1, 0, 0, 1, 0, N'PURCHASE.REQUEST', 1),
    (N'PURCHASE.REQUEST.READ.ONE', N'PURCHASE', N'PurchaseRequestsController', N'GetById', N'GET', N'/api/v1/purchase/requests/{id}', N'Obtener solicitud de compra por id.', N'PURCHASE.REQUEST.READ', 1, 1, 1, 0, 0, 1, 0, N'PURCHASE.REQUEST', 1),
    (N'PURCHASE.REQUEST.READ.LIST', N'PURCHASE', N'PurchaseRequestsController', N'List', N'GET', N'/api/v1/purchase/requests', N'Listar solicitudes visibles por contexto.', N'PURCHASE.REQUEST.READ', 1, 1, 1, 0, 0, 1, 0, N'PURCHASE.REQUEST', 1),
    (N'PURCHASE.REQUEST.UPDATE', N'PURCHASE', N'PurchaseRequestsController', N'UpdateDraft', N'PUT', N'/api/v1/purchase/requests/{id}', N'Actualizar solicitud en borrador.', N'PURCHASE.REQUEST.UPDATE', 1, 1, 1, 0, 0, 1, 0, N'PURCHASE.REQUEST', 1),
    (N'PURCHASE.REQUEST.DETAIL.UPSERT', N'PURCHASE', N'PurchaseRequestsController', N'UpsertDetail', N'PUT', N'/api/v1/purchase/requests/{id}/details', N'Agregar o editar detalle en borrador.', N'PURCHASE.REQUEST.UPDATE', 1, 1, 1, 0, 0, 1, 0, N'PURCHASE.REQUEST', 1),
    (N'PURCHASE.REQUEST.SUBMIT', N'PURCHASE', N'PurchaseRequestsController', N'Submit', N'POST', N'/api/v1/purchase/requests/{id}/submit', N'Enviar solicitud para aprobacion.', N'PURCHASE.REQUEST.SUBMIT', 1, 1, 1, 0, 0, 1, 0, N'PURCHASE.REQUEST', 1),
    (N'PURCHASE.REQUEST.APPROVE', N'PURCHASE', N'PurchaseRequestsController', N'Approve', N'POST', N'/api/v1/purchase/requests/{id}/approve', N'Aprobar solicitud enviada (MFA obligatorio).', N'PURCHASE.REQUEST.APPROVE', 1, 1, 1, 0, 1, 1, 0, N'PURCHASE.REQUEST', 1);

    MERGE seguridad.operacion_api AS target
    USING
    (
        SELECT
            codigo, modulo, controlador, accion, metodo_http, ruta, descripcion, activo
        FROM @operations
    ) AS source
    ON target.codigo = source.codigo
    WHEN MATCHED THEN
        UPDATE
        SET
            target.modulo = source.modulo,
            target.controlador = source.controlador,
            target.accion = source.accion,
            target.metodo_http = source.metodo_http,
            target.ruta = source.ruta,
            target.descripcion = source.descripcion,
            target.activo = source.activo,
            target.actualizado_utc = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT
        (
            codigo, modulo, controlador, accion, metodo_http, ruta, descripcion, activo, creado_utc, actualizado_utc
        )
        VALUES
        (
            source.codigo, source.modulo, source.controlador, source.accion, source.metodo_http, source.ruta, source.descripcion, source.activo, SYSUTCDATETIME(), NULL
        );

    MERGE seguridad.politica_operacion_api AS target
    USING
    (
        SELECT
            oa.id_operacion_api,
            p.id_permiso,
            o.requiere_autenticacion,
            o.requiere_sesion,
            o.requiere_empresa,
            o.requiere_unidad_organizativa,
            o.requiere_mfa,
            o.requiere_auditoria,
            o.requiere_aprobacion,
            o.codigo_entidad,
            o.activo
        FROM @operations o
        INNER JOIN seguridad.operacion_api oa ON oa.codigo = o.codigo
        LEFT JOIN seguridad.permiso p ON p.codigo = o.permiso_codigo
    ) AS source
    ON target.id_operacion_api = source.id_operacion_api
    WHEN MATCHED THEN
        UPDATE
        SET
            target.id_permiso = source.id_permiso,
            target.requiere_autenticacion = source.requiere_autenticacion,
            target.requiere_sesion = source.requiere_sesion,
            target.requiere_empresa = source.requiere_empresa,
            target.requiere_unidad_organizativa = source.requiere_unidad_organizativa,
            target.requiere_mfa = source.requiere_mfa,
            target.requiere_auditoria = source.requiere_auditoria,
            target.requiere_aprobacion = source.requiere_aprobacion,
            target.codigo_entidad = source.codigo_entidad,
            target.activo = source.activo,
            target.actualizado_utc = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT
        (
            id_operacion_api, id_permiso, requiere_autenticacion, requiere_sesion, requiere_empresa,
            requiere_unidad_organizativa, requiere_mfa, requiere_auditoria, requiere_aprobacion,
            codigo_entidad, activo, creado_utc, actualizado_utc
        )
        VALUES
        (
            source.id_operacion_api, source.id_permiso, source.requiere_autenticacion, source.requiere_sesion, source.requiere_empresa,
            source.requiere_unidad_organizativa, source.requiere_mfa, source.requiere_auditoria, source.requiere_aprobacion,
            source.codigo_entidad, source.activo, SYSUTCDATETIME(), NULL
        );
END;
GO

MERGE seguridad.politica_seguridad_operacional AS target
USING
(
    VALUES
    (N'PURCHASE.REQUEST.SUBMIT', 60, 12, NULL, 0, 1),
    (N'PURCHASE.REQUEST.APPROVE', 60, 8, NULL, 0, 1)
) AS source(codigo_accion, ventana_segundos, max_intentos, lockout_minutos, aplica_lockout, activo)
ON target.codigo_accion = source.codigo_accion
WHEN MATCHED THEN
    UPDATE
    SET
        target.ventana_segundos = source.ventana_segundos,
        target.max_intentos = source.max_intentos,
        target.lockout_minutos = source.lockout_minutos,
        target.aplica_lockout = source.aplica_lockout,
        target.activo = source.activo,
        target.actualizado_utc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT
    (
        codigo_accion, ventana_segundos, max_intentos, lockout_minutos, aplica_lockout, activo, creado_utc, actualizado_utc
    )
    VALUES
    (
        source.codigo_accion, source.ventana_segundos, source.max_intentos, source.lockout_minutos, source.aplica_lockout, source.activo, SYSUTCDATETIME(), NULL
    );
GO
