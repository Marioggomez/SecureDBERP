SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @tenantCode NVARCHAR(100) = N'BOOTSTRAP';
DECLARE @tenantName NVARCHAR(400) = N'SecureERP Bootstrap Tenant';
DECLARE @tenantDomain NVARCHAR(400) = N'bootstrap.secureerp.local';
DECLARE @companyCode NVARCHAR(100) = N'MAIN';
DECLARE @companyName NVARCHAR(500) = N'SecureERP Bootstrap Company';
DECLARE @adminLogin NVARCHAR(240) = N'admin@secureerp.local';
DECLARE @adminLoginNormalized NVARCHAR(500) = UPPER(@adminLogin);
DECLARE @adminCode NVARCHAR(100) = N'BOOTSTRAP.ADMIN';
DECLARE @adminDisplay NVARCHAR(240) = N'SecureERP Bootstrap Admin';
DECLARE @adminPasswordAlgorithm VARCHAR(60) = 'PBKDF2_HMAC_SHA512';
DECLARE @adminPasswordIterations INT = 210000;
DECLARE @adminPasswordSalt VARBINARY(64) = 0xA1B2C3D4E5F60123456789ABCDEFFEDC00112233445566778899AABBCCDDEEFF;
DECLARE @adminPasswordHash VARBINARY(128) = 0x0E8B5121A939D0064A65E80091767EA78C0204E994D484207F3F4329FA0993F4F18D0A895DAFD070B8539D7723670A7F6539376A3F3C001147F865505756C448;
DECLARE @defaultUtc DATETIME2(7) = SYSUTCDATETIME();

DECLARE @tenantId BIGINT;
DECLARE @companyId BIGINT;
DECLARE @userId BIGINT;
DECLARE @roleId BIGINT;
DECLARE @allowEffectId SMALLINT;
DECLARE @scopeCompanyId SMALLINT;
DECLARE @stateUserId SMALLINT;
DECLARE @stateCompanyId SMALLINT;
DECLARE @typeCompanyId SMALLINT;

SELECT TOP (1) @stateUserId = id_estado_usuario
FROM catalogo.estado_usuario
WHERE codigo = 'ACTIVO';

SELECT TOP (1) @stateCompanyId = id_estado_empresa
FROM catalogo.estado_empresa
WHERE codigo = 'ACTIVA';

SELECT TOP (1) @typeCompanyId = id_tipo_empresa
FROM catalogo.tipo_empresa
WHERE codigo = 'GENERAL';

SELECT TOP (1) @allowEffectId = id_efecto_permiso
FROM catalogo.efecto_permiso
WHERE codigo = 'ALLOW';

SELECT TOP (1) @scopeCompanyId = id_alcance_asignacion
FROM catalogo.alcance_asignacion
WHERE codigo = 'EMPRESA';

SELECT TOP (1) @tenantId = id_tenant
FROM plataforma.tenant
WHERE codigo = @tenantCode;

IF @tenantId IS NULL
BEGIN
    INSERT INTO plataforma.tenant
    (
        codigo,
        nombre,
        descripcion,
        dominio_principal,
        activo,
        creado_utc,
        actualizado_utc,
        es_entrenamiento
    )
    VALUES
    (
        @tenantCode,
        @tenantName,
        N'Tenant bootstrap para entornos nuevos.',
        @tenantDomain,
        1,
        @defaultUtc,
        NULL,
        0
    );

    SET @tenantId = SCOPE_IDENTITY();
END;

EXEC sys.sp_set_session_context @key=N'id_tenant', @value=@tenantId, @read_only=0;
EXEC sys.sp_set_session_context @key=N'es_admin_tenant', @value=1, @read_only=0;

SELECT TOP (1) @companyId = id_empresa
FROM organizacion.empresa
WHERE id_tenant = @tenantId
  AND codigo = @companyCode;

IF @companyId IS NULL
BEGIN
    INSERT INTO organizacion.empresa
    (
        id_tenant,
        codigo,
        nombre,
        nombre_legal,
        id_tipo_empresa,
        id_estado_empresa,
        identificacion_fiscal,
        moneda_base,
        zona_horaria,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        @tenantId,
        @companyCode,
        @companyName,
        @companyName,
        COALESCE(@typeCompanyId, 1),
        COALESCE(@stateCompanyId, 1),
        N'BOOTSTRAP-NIT',
        'USD',
        N'America/Guatemala',
        1,
        @defaultUtc,
        NULL
    );

    SET @companyId = SCOPE_IDENTITY();
END;

EXEC sys.sp_set_session_context @key=N'id_empresa', @value=@companyId, @read_only=0;

SELECT TOP (1) @userId = id_usuario
FROM seguridad.usuario
WHERE login_normalizado = @adminLoginNormalized;

IF @userId IS NULL
BEGIN
    INSERT INTO seguridad.usuario
    (
        codigo,
        login_principal,
        login_normalizado,
        nombre,
        apellido,
        nombre_mostrar,
        correo_electronico,
        correo_normalizado,
        telefono_movil,
        idioma,
        zona_horaria,
        id_estado_usuario,
        bloqueado_hasta_utc,
        mfa_habilitado,
        requiere_cambio_clave,
        ultimo_acceso_utc,
        activo,
        creado_por,
        creado_utc,
        actualizado_por,
        actualizado_utc
    )
    VALUES
    (
        @adminCode,
        @adminLogin,
        @adminLoginNormalized,
        N'Bootstrap',
        N'Admin',
        @adminDisplay,
        @adminLogin,
        @adminLoginNormalized,
        NULL,
        N'es-GT',
        N'America/Guatemala',
        COALESCE(@stateUserId, 1),
        NULL,
        1,
        1,
        NULL,
        1,
        NULL,
        @defaultUtc,
        NULL,
        NULL
    );

    SET @userId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE seguridad.usuario
    SET
        nombre_mostrar = @adminDisplay,
        correo_electronico = @adminLogin,
        correo_normalizado = @adminLoginNormalized,
        mfa_habilitado = 1,
        activo = 1,
        actualizado_utc = SYSUTCDATETIME()
    WHERE id_usuario = @userId;
END;

EXEC sys.sp_set_session_context @key=N'id_usuario', @value=@userId, @read_only=0;

IF NOT EXISTS
(
    SELECT 1
    FROM seguridad.usuario_tenant
    WHERE id_usuario = @userId
      AND id_tenant = @tenantId
)
BEGIN
    INSERT INTO seguridad.usuario_tenant
    (
        id_usuario,
        id_tenant,
        es_administrador_tenant,
        es_cuenta_servicio,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        @userId,
        @tenantId,
        1,
        0,
        1,
        SYSUTCDATETIME(),
        NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM seguridad.usuario_empresa
    WHERE id_usuario = @userId
      AND id_tenant = @tenantId
      AND id_empresa = @companyId
)
BEGIN
    INSERT INTO seguridad.usuario_empresa
    (
        id_usuario,
        id_tenant,
        id_empresa,
        es_empresa_predeterminada,
        puede_operar,
        fecha_inicio_utc,
        fecha_fin_utc,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        @userId,
        @tenantId,
        @companyId,
        1,
        1,
        DATEADD(MINUTE, -5, SYSUTCDATETIME()),
        NULL,
        1,
        SYSUTCDATETIME(),
        NULL
    );
END;

IF EXISTS (SELECT 1 FROM seguridad.credencial_local_usuario WHERE id_usuario = @userId)
BEGIN
    UPDATE seguridad.credencial_local_usuario
    SET
        hash_clave = @adminPasswordHash,
        salt_clave = @adminPasswordSalt,
        algoritmo_clave = @adminPasswordAlgorithm,
        iteraciones_clave = @adminPasswordIterations,
        cambio_clave_utc = SYSUTCDATETIME(),
        debe_cambiar_clave = 1,
        activo = 1
    WHERE id_usuario = @userId;
END
ELSE
BEGIN
    INSERT INTO seguridad.credencial_local_usuario
    (
        id_usuario,
        hash_clave,
        salt_clave,
        algoritmo_clave,
        iteraciones_clave,
        cambio_clave_utc,
        debe_cambiar_clave,
        activo
    )
    VALUES
    (
        @userId,
        @adminPasswordHash,
        @adminPasswordSalt,
        @adminPasswordAlgorithm,
        @adminPasswordIterations,
        SYSUTCDATETIME(),
        1,
        1
    );
END;

SELECT TOP (1) @roleId = id_rol
FROM seguridad.rol
WHERE id_tenant = @tenantId
  AND codigo = N'SECURITY.ADMIN';

IF @roleId IS NULL
BEGIN
    INSERT INTO seguridad.rol
    (
        id_tenant,
        codigo,
        nombre,
        descripcion,
        es_sistema,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        @tenantId,
        N'SECURITY.ADMIN',
        N'Security Administrator',
        N'Rol bootstrap para administrar seguridad en entorno inicial.',
        1,
        1,
        SYSUTCDATETIME(),
        NULL
    );

    SET @roleId = SCOPE_IDENTITY();
END;

IF NOT EXISTS
(
    SELECT 1
    FROM seguridad.asignacion_rol_usuario
    WHERE id_usuario = @userId
      AND id_tenant = @tenantId
      AND id_rol = @roleId
      AND id_empresa = @companyId
      AND activo = 1
)
BEGIN
    INSERT INTO seguridad.asignacion_rol_usuario
    (
        id_usuario,
        id_tenant,
        id_rol,
        id_alcance_asignacion,
        id_grupo_empresarial,
        id_empresa,
        id_unidad_organizativa,
        fecha_inicio_utc,
        fecha_fin_utc,
        concedido_por,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        @userId,
        @tenantId,
        @roleId,
        COALESCE(@scopeCompanyId, 1),
        NULL,
        @companyId,
        NULL,
        DATEADD(MINUTE, -5, SYSUTCDATETIME()),
        NULL,
        @userId,
        1,
        SYSUTCDATETIME(),
        NULL
    );
END;

MERGE seguridad.politica_tenant AS target
USING
(
    SELECT
        id_tenant = @tenantId,
        timeout_inactividad_min = 30,
        timeout_absoluto_min = 480,
        longitud_minima_clave = CAST(12 AS TINYINT),
        requiere_mayuscula = CAST(1 AS BIT),
        requiere_minuscula = CAST(1 AS BIT),
        requiere_numero = CAST(1 AS BIT),
        requiere_especial = CAST(1 AS BIT),
        historial_claves = CAST(5 AS TINYINT),
        max_intentos_login = CAST(5 AS TINYINT),
        minutos_bloqueo = 15,
        mfa_obligatorio = CAST(1 AS BIT),
        permite_login_local = CAST(1 AS BIT),
        permite_sso = CAST(0 AS BIT),
        requiere_mfa_aprobaciones = CAST(1 AS BIT),
        requiere_politica_ip = CAST(0 AS BIT),
        limite_rate_por_minuto = 120,
        activo = CAST(1 AS BIT)
) AS source
ON target.id_tenant = source.id_tenant
WHEN MATCHED THEN
    UPDATE
    SET
        target.timeout_inactividad_min = source.timeout_inactividad_min,
        target.timeout_absoluto_min = source.timeout_absoluto_min,
        target.longitud_minima_clave = source.longitud_minima_clave,
        target.requiere_mayuscula = source.requiere_mayuscula,
        target.requiere_minuscula = source.requiere_minuscula,
        target.requiere_numero = source.requiere_numero,
        target.requiere_especial = source.requiere_especial,
        target.historial_claves = source.historial_claves,
        target.max_intentos_login = source.max_intentos_login,
        target.minutos_bloqueo = source.minutos_bloqueo,
        target.mfa_obligatorio = source.mfa_obligatorio,
        target.permite_login_local = source.permite_login_local,
        target.permite_sso = source.permite_sso,
        target.requiere_mfa_aprobaciones = source.requiere_mfa_aprobaciones,
        target.requiere_politica_ip = source.requiere_politica_ip,
        target.limite_rate_por_minuto = source.limite_rate_por_minuto,
        target.activo = source.activo,
        target.actualizado_utc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT
    (
        id_tenant,
        timeout_inactividad_min,
        timeout_absoluto_min,
        longitud_minima_clave,
        requiere_mayuscula,
        requiere_minuscula,
        requiere_numero,
        requiere_especial,
        historial_claves,
        max_intentos_login,
        minutos_bloqueo,
        mfa_obligatorio,
        permite_login_local,
        permite_sso,
        requiere_mfa_aprobaciones,
        requiere_politica_ip,
        limite_rate_por_minuto,
        creado_utc,
        actualizado_utc,
        activo
    )
    VALUES
    (
        source.id_tenant,
        source.timeout_inactividad_min,
        source.timeout_absoluto_min,
        source.longitud_minima_clave,
        source.requiere_mayuscula,
        source.requiere_minuscula,
        source.requiere_numero,
        source.requiere_especial,
        source.historial_claves,
        source.max_intentos_login,
        source.minutos_bloqueo,
        source.mfa_obligatorio,
        source.permite_login_local,
        source.permite_sso,
        source.requiere_mfa_aprobaciones,
        source.requiere_politica_ip,
        source.limite_rate_por_minuto,
        SYSUTCDATETIME(),
        NULL,
        source.activo
    );

MERGE seguridad.politica_seguridad_operacional AS target
USING
(
    VALUES
    (N'AUTH.LOGIN', 60, 5, 15, 1, 1),
    (N'AUTH.MFA.CHALLENGE', 60, 6, NULL, 0, 1),
    (N'AUTH.MFA.VERIFY', 60, 8, NULL, 0, 1),
    (N'AUTH.VALIDATE_SESSION', 60, 20, NULL, 0, 1),
    (N'WORKFLOW.APPROVAL_INSTANCE.CREATE', 60, 10, NULL, 0, 1)
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
        codigo_accion,
        ventana_segundos,
        max_intentos,
        lockout_minutos,
        aplica_lockout,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        source.codigo_accion,
        source.ventana_segundos,
        source.max_intentos,
        source.lockout_minutos,
        source.aplica_lockout,
        source.activo,
        SYSUTCDATETIME(),
        NULL
    );

IF NOT EXISTS
(
    SELECT 1
    FROM seguridad.politica_ip
    WHERE id_tenant = @tenantId
      AND id_empresa IS NULL
      AND ip_o_cidr = N'127.0.0.1'
      AND accion = 'ALLOW'
)
BEGIN
    INSERT INTO seguridad.politica_ip
    (
        id_tenant,
        id_empresa,
        ip_o_cidr,
        accion,
        prioridad,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        @tenantId,
        NULL,
        N'127.0.0.1',
        'ALLOW',
        100,
        0,
        SYSUTCDATETIME(),
        NULL
    );
END;

INSERT INTO seguridad.excepcion_permiso_usuario
(
    id_usuario,
    id_tenant,
    id_permiso,
    id_efecto_permiso,
    id_alcance_asignacion,
    id_grupo_empresarial,
    id_empresa,
    id_unidad_organizativa,
    fecha_inicio_utc,
    fecha_fin_utc,
    concedido_por,
    motivo,
    activo,
    creado_utc,
    actualizado_utc
)
SELECT
    @userId,
    @tenantId,
    p.id_permiso,
    COALESCE(@allowEffectId, 1),
    COALESCE(@scopeCompanyId, 1),
    NULL,
    @companyId,
    NULL,
    DATEADD(MINUTE, -5, SYSUTCDATETIME()),
    NULL,
    @userId,
    N'Bootstrap admin explicit allow',
    1,
    SYSUTCDATETIME(),
    NULL
FROM seguridad.permiso p
WHERE p.activo = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM seguridad.excepcion_permiso_usuario e
      WHERE e.id_usuario = @userId
        AND e.id_tenant = @tenantId
        AND e.id_permiso = p.id_permiso
        AND e.id_efecto_permiso = COALESCE(@allowEffectId, 1)
        AND e.id_empresa = @companyId
        AND e.activo = 1
  );

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
    codigo,
    modulo,
    controlador,
    accion,
    metodo_http,
    ruta,
    descripcion,
    permiso_codigo,
    requiere_autenticacion,
    requiere_sesion,
    requiere_empresa,
    requiere_unidad_organizativa,
    requiere_mfa,
    requiere_auditoria,
    requiere_aprobacion,
    codigo_entidad,
    activo
)
VALUES
(N'AUTH.LOGIN', N'AUTH', N'AuthController', N'Login', N'POST', N'/api/v1/auth/login', N'Autenticacion inicial.', NULL, 0, 0, 0, 0, 0, 1, 0, NULL, 1),
(N'AUTH.MFA.CHALLENGE', N'AUTH', N'AuthController', N'RequestMfaChallenge', N'POST', N'/api/v1/auth/mfa/challenge', N'Emision de desafio MFA.', NULL, 0, 0, 0, 0, 0, 1, 0, NULL, 1),
(N'AUTH.MFA.VERIFY', N'AUTH', N'AuthController', N'VerifyMfaChallenge', N'POST', N'/api/v1/auth/mfa/verify', N'Validacion de desafio MFA.', NULL, 0, 0, 0, 0, 0, 1, 0, NULL, 1),
(N'AUTH.SELECT_COMPANY', N'AUTH', N'AuthController', N'SelectCompany', N'POST', N'/api/v1/auth/select-company', N'Seleccion de empresa para emitir sesion.', NULL, 0, 0, 0, 0, 0, 1, 0, NULL, 1),
(N'AUTH.SESSION.VALIDATE', N'AUTH', N'AuthController', N'ValidateSession', N'POST', N'/api/v1/auth/validate-session', N'Validacion de sesion opaca y contexto.', N'AUTH.SESSION.VALIDATE', 1, 1, 0, 0, 0, 1, 0, NULL, 1),
(N'ORGANIZATION.UNIT.READ', N'ORGANIZATION', N'OrganizationUnitsController', N'List', N'GET', N'/api/v1/organization/units', N'Listado de unidades organizativas.', N'ORGANIZATION.UNIT.READ', 1, 1, 1, 0, 0, 1, 0, NULL, 1),
(N'ORGANIZATION.UNIT.CREATE', N'ORGANIZATION', N'OrganizationUnitsController', N'Create', N'POST', N'/api/v1/organization/units', N'Creacion de unidad organizativa.', N'ORGANIZATION.UNIT.CREATE', 1, 1, 1, 0, 0, 1, 0, NULL, 1),
(N'WORKFLOW.APPROVAL_INSTANCE.READ', N'WORKFLOW', N'ApprovalInstancesController', N'List', N'GET', N'/api/v1/workflow/approval-instances', N'Listado de instancias de aprobacion.', N'WORKFLOW.APPROVAL_INSTANCE.READ', 1, 1, 1, 0, 0, 1, 0, NULL, 1),
(N'WORKFLOW.APPROVAL_INSTANCE.CREATE', N'WORKFLOW', N'ApprovalInstancesController', N'Create', N'POST', N'/api/v1/workflow/approval-instances', N'Creacion de instancia de aprobacion sensible.', N'WORKFLOW.APPROVAL_INSTANCE.CREATE', 1, 1, 1, 0, 1, 1, 0, NULL, 1),
(N'SYSTEM.HEALTH.READ', N'SYSTEM', N'Health', N'Read', N'GET', N'/health', N'Consulta de estado general.', N'SYSTEM.HEALTH.READ', 0, 0, 0, 0, 0, 0, 0, NULL, 1),
(N'SYSTEM.HEALTH.READY', N'SYSTEM', N'Health', N'Readiness', N'GET', N'/health/ready', N'Readiness de dependencias.', N'SYSTEM.HEALTH.READ', 0, 0, 0, 0, 0, 0, 0, NULL, 1),
(N'SYSTEM.HEALTH.LIVE', N'SYSTEM', N'Health', N'Liveness', N'GET', N'/health/live', N'Liveness endpoint.', N'SYSTEM.HEALTH.READ', 0, 0, 0, 0, 0, 0, 0, NULL, 1);

MERGE seguridad.operacion_api AS target
USING
(
    SELECT
        o.codigo,
        o.modulo,
        o.controlador,
        o.accion,
        o.metodo_http,
        o.ruta,
        o.descripcion,
        o.activo
    FROM @operations o
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
        codigo,
        modulo,
        controlador,
        accion,
        metodo_http,
        ruta,
        descripcion,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        source.codigo,
        source.modulo,
        source.controlador,
        source.accion,
        source.metodo_http,
        source.ruta,
        source.descripcion,
        source.activo,
        SYSUTCDATETIME(),
        NULL
    );

MERGE seguridad.politica_operacion_api AS target
USING
(
    SELECT
        oa.id_operacion_api,
        id_permiso = p.id_permiso,
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
        id_operacion_api,
        id_permiso,
        requiere_autenticacion,
        requiere_sesion,
        requiere_empresa,
        requiere_unidad_organizativa,
        requiere_mfa,
        requiere_auditoria,
        requiere_aprobacion,
        codigo_entidad,
        activo,
        creado_utc,
        actualizado_utc
    )
    VALUES
    (
        source.id_operacion_api,
        source.id_permiso,
        source.requiere_autenticacion,
        source.requiere_sesion,
        source.requiere_empresa,
        source.requiere_unidad_organizativa,
        source.requiere_mfa,
        source.requiere_auditoria,
        source.requiere_aprobacion,
        source.codigo_entidad,
        source.activo,
        SYSUTCDATETIME(),
        NULL
    );
GO
