SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'seguridad.catalogo_permiso_oficial', N'U') IS NULL
BEGIN
    CREATE TABLE seguridad.catalogo_permiso_oficial
    (
        id_catalogo_permiso_oficial BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_catalogo_permiso_oficial PRIMARY KEY,
        codigo NVARCHAR(300) NOT NULL,
        modulo NVARCHAR(100) NOT NULL,
        entidad NVARCHAR(120) NOT NULL,
        accion NVARCHAR(120) NOT NULL,
        descripcion_tecnica NVARCHAR(1000) NOT NULL,
        descripcion_funcional NVARCHAR(500) NOT NULL,
        requiere_mfa BIT NOT NULL CONSTRAINT DF_catalogo_permiso_oficial_requiere_mfa DEFAULT(0),
        es_sensible BIT NOT NULL CONSTRAINT DF_catalogo_permiso_oficial_es_sensible DEFAULT(0),
        activo BIT NOT NULL CONSTRAINT DF_catalogo_permiso_oficial_activo DEFAULT(1),
        creado_utc DATETIME2(7) NOT NULL CONSTRAINT DF_catalogo_permiso_oficial_creado_utc DEFAULT(SYSUTCDATETIME()),
        actualizado_utc DATETIME2(7) NULL
    );

    CREATE UNIQUE INDEX UX_catalogo_permiso_oficial_codigo
        ON seguridad.catalogo_permiso_oficial(codigo);
END;
GO

DECLARE @permisos TABLE
(
    codigo NVARCHAR(300) NOT NULL,
    modulo NVARCHAR(100) NOT NULL,
    entidad NVARCHAR(120) NOT NULL,
    accion NVARCHAR(120) NOT NULL,
    descripcion_tecnica NVARCHAR(1000) NOT NULL,
    descripcion_funcional NVARCHAR(500) NOT NULL,
    requiere_mfa BIT NOT NULL,
    es_sensible BIT NOT NULL,
    activo BIT NOT NULL
);

INSERT INTO @permisos
(
    codigo,
    modulo,
    entidad,
    accion,
    descripcion_tecnica,
    descripcion_funcional,
    requiere_mfa,
    es_sensible,
    activo
)
VALUES
(N'AUTH.SESSION.VALIDATE', N'AUTH', N'SESSION', N'VALIDATE', N'Permite validar sesion opaca activa desde API y middleware.', N'Validar sesion vigente.', 0, 0, 1),
(N'AUTH.SESSION.REVOKE', N'AUTH', N'SESSION', N'REVOKE', N'Permite revocar sesiones activas de usuario en el contexto permitido.', N'Revocar sesion activa.', 1, 1, 1),
(N'AUTH.MFA.CHALLENGE', N'AUTH', N'MFA', N'CHALLENGE', N'Permite emitir desafios MFA para login o step-up.', N'Generar desafio MFA.', 0, 1, 1),
(N'AUTH.MFA.VERIFY', N'AUTH', N'MFA', N'VERIFY', N'Permite verificar codigos MFA y marcar estado validado.', N'Validar codigo MFA.', 0, 1, 1),
(N'SECURITY.USER.READ', N'SECURITY', N'USER', N'READ', N'Permite consultar usuarios de seguridad en contexto autorizado.', N'Consultar usuarios.', 0, 1, 1),
(N'SECURITY.USER.RESET_PASSWORD', N'SECURITY', N'USER', N'RESET_PASSWORD', N'Permite iniciar y confirmar reseteo de clave de usuario.', N'Resetear clave de usuario.', 1, 1, 1),
(N'SECURITY.ROLE.ASSIGN', N'SECURITY', N'ROLE', N'ASSIGN', N'Permite asignar roles y alcances a usuario objetivo.', N'Asignar roles.', 1, 1, 1),
(N'ORGANIZATION.UNIT.READ', N'ORGANIZATION', N'UNIT', N'READ', N'Permite listar unidades organizativas filtradas por contexto/RLS.', N'Consultar unidades organizativas.', 0, 0, 1),
(N'ORGANIZATION.UNIT.CREATE', N'ORGANIZATION', N'UNIT', N'CREATE', N'Permite crear unidades organizativas en tenant/empresa activos.', N'Crear unidad organizativa.', 0, 1, 1),
(N'WORKFLOW.APPROVAL_INSTANCE.READ', N'WORKFLOW', N'APPROVAL_INSTANCE', N'READ', N'Permite consultar instancias de aprobacion respetando permisos y RLS.', N'Consultar instancias de aprobacion.', 0, 0, 1),
(N'WORKFLOW.APPROVAL_INSTANCE.CREATE', N'WORKFLOW', N'APPROVAL_INSTANCE', N'CREATE', N'Permite crear instancias de aprobacion; operacion sensible con MFA.', N'Crear instancia de aprobacion.', 1, 1, 1),
(N'PURCHASE.REQUEST.READ', N'PURCHASE', N'REQUEST', N'READ', N'Permite consultar solicitudes de compra por contexto tenant/empresa.', N'Consultar solicitudes de compra.', 0, 0, 1),
(N'PURCHASE.REQUEST.CREATE', N'PURCHASE', N'REQUEST', N'CREATE', N'Permite crear solicitudes de compra en estado borrador.', N'Crear solicitud de compra.', 0, 1, 1),
(N'PURCHASE.REQUEST.UPDATE', N'PURCHASE', N'REQUEST', N'UPDATE', N'Permite actualizar solicitud de compra y su detalle mientras esta en borrador.', N'Actualizar solicitud de compra en borrador.', 0, 1, 1),
(N'PURCHASE.REQUEST.SUBMIT', N'PURCHASE', N'REQUEST', N'SUBMIT', N'Permite enviar solicitud de compra al flujo de aprobacion.', N'Enviar solicitud de compra.', 0, 1, 1),
(N'PURCHASE.REQUEST.APPROVE', N'PURCHASE', N'REQUEST', N'APPROVE', N'Permite aprobar solicitud de compra enviada. Requiere MFA y SoD.', N'Aprobar solicitud de compra.', 1, 1, 1),
(N'PURCHASE.ORDER.READ', N'PURCHASE', N'ORDER', N'READ', N'Permite consultar ordenes de compra por contexto tenant/empresa.', N'Consultar ordenes de compra.', 0, 0, 1),
(N'PURCHASE.ORDER.CREATE', N'PURCHASE', N'ORDER', N'CREATE', N'Permite crear ordenes de compra en estado borrador.', N'Crear orden de compra.', 0, 1, 1),
(N'PURCHASE.ORDER.UPDATE', N'PURCHASE', N'ORDER', N'UPDATE', N'Permite actualizar orden de compra y su detalle mientras esta en borrador.', N'Actualizar orden de compra en borrador.', 0, 1, 1),
(N'PURCHASE.ORDER.SUBMIT', N'PURCHASE', N'ORDER', N'SUBMIT', N'Permite enviar orden de compra al flujo de aprobacion.', N'Enviar orden de compra.', 0, 1, 1),
(N'PURCHASE.ORDER.APPROVE', N'PURCHASE', N'ORDER', N'APPROVE', N'Permite aprobar orden de compra enviada. Requiere MFA y SoD.', N'Aprobar orden de compra.', 1, 1, 1),
(N'SYSTEM.HEALTH.READ', N'SYSTEM', N'HEALTH', N'READ', N'Permite consultar estado operativo de endpoints de salud.', N'Consultar estado de salud del sistema.', 0, 0, 1),
(N'AUDIT.SECURITY_EVENT.READ', N'AUDIT', N'SECURITY_EVENT', N'READ', N'Permite consultar eventos de seguridad y observabilidad.', N'Consultar eventos de seguridad.', 1, 1, 1);

MERGE seguridad.catalogo_permiso_oficial AS target
USING @permisos AS source
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
        codigo,
        modulo,
        entidad,
        accion,
        descripcion_tecnica,
        descripcion_funcional,
        requiere_mfa,
        es_sensible,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        source.codigo,
        source.modulo,
        source.entidad,
        source.accion,
        source.descripcion_tecnica,
        source.descripcion_funcional,
        source.requiere_mfa,
        source.es_sensible,
        source.activo,
        SYSUTCDATETIME(),
        NULL
    );
GO

MERGE seguridad.permiso AS target
USING
(
    SELECT
        p.codigo,
        p.modulo,
        p.accion,
        nombre = CONCAT(p.modulo, N'.', p.entidad, N'.', p.accion),
        descripcion = p.descripcion_tecnica,
        p.es_sensible,
        p.activo
    FROM seguridad.catalogo_permiso_oficial p
) AS source
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
        codigo,
        modulo,
        accion,
        nombre,
        descripcion,
        es_sensible,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        source.codigo,
        source.modulo,
        source.accion,
        source.nombre,
        source.descripcion,
        source.es_sensible,
        source.activo,
        SYSUTCDATETIME(),
        NULL
    );
GO
