SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'seguridad.usp_security_usuario_listar', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE seguridad.usp_security_usuario_listar AS BEGIN SET NOCOUNT ON; END');
END;
GO

CREATE OR ALTER PROCEDURE seguridad.usp_security_usuario_listar
    @buscar NVARCHAR(200) = NULL,
    @solo_activos BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ctx_id_tenant BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
    DECLARE @ctx_id_empresa BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
    DECLARE @buscar_normalizado NVARCHAR(200) = NULLIF(LTRIM(RTRIM(@buscar)), N'');

    IF @ctx_id_tenant IS NULL
    BEGIN
        THROW 51110, N'Security tenant context is required.', 1;
    END;

    SELECT DISTINCT
        u.id_usuario,
        u.codigo,
        u.login_principal,
        u.nombre_mostrar,
        u.correo_electronico,
        u.mfa_habilitado,
        u.requiere_cambio_clave,
        u.activo,
        ut.es_administrador_tenant,
        ue.id_empresa,
        CAST(ISNULL(ue.es_empresa_predeterminada, 0) AS BIT) AS es_empresa_predeterminada,
        CAST(ISNULL(ue.puede_operar, 0) AS BIT) AS puede_operar,
        ue.fecha_inicio_utc,
        ue.fecha_fin_utc,
        u.bloqueado_hasta_utc,
        u.ultimo_acceso_utc
    FROM seguridad.usuario u
    INNER JOIN seguridad.usuario_tenant ut
        ON ut.id_usuario = u.id_usuario
       AND ut.id_tenant = @ctx_id_tenant
       AND ut.activo = 1
    LEFT JOIN seguridad.usuario_empresa ue
        ON ue.id_usuario = u.id_usuario
       AND ue.id_tenant = @ctx_id_tenant
       AND ue.activo = 1
       AND (@ctx_id_empresa IS NULL OR ue.id_empresa = @ctx_id_empresa)
    WHERE (@solo_activos = 0 OR u.activo = 1)
      AND (
            @ctx_id_empresa IS NULL
            OR ue.id_empresa = @ctx_id_empresa
            OR ut.es_administrador_tenant = 1
          )
      AND (
            @buscar_normalizado IS NULL
            OR u.codigo LIKE N'%' + @buscar_normalizado + N'%'
            OR u.login_principal LIKE N'%' + @buscar_normalizado + N'%'
            OR u.nombre_mostrar LIKE N'%' + @buscar_normalizado + N'%'
            OR u.correo_electronico LIKE N'%' + @buscar_normalizado + N'%'
          )
    ORDER BY u.nombre_mostrar, u.login_principal;
END;
GO

IF OBJECT_ID(N'seguridad.usp_security_usuario_obtener', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE seguridad.usp_security_usuario_obtener AS BEGIN SET NOCOUNT ON; END');
END;
GO

CREATE OR ALTER PROCEDURE seguridad.usp_security_usuario_obtener
    @id_usuario BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ctx_id_tenant BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
    DECLARE @ctx_id_empresa BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);

    IF @ctx_id_tenant IS NULL
    BEGIN
        THROW 51111, N'Security tenant context is required.', 1;
    END;

    SELECT TOP (1)
        u.id_usuario,
        u.codigo,
        u.login_principal,
        u.nombre_mostrar,
        u.correo_electronico,
        u.mfa_habilitado,
        u.requiere_cambio_clave,
        u.activo,
        ut.es_administrador_tenant,
        ue.id_empresa,
        CAST(ISNULL(ue.es_empresa_predeterminada, 0) AS BIT) AS es_empresa_predeterminada,
        CAST(ISNULL(ue.puede_operar, 0) AS BIT) AS puede_operar,
        ue.fecha_inicio_utc,
        ue.fecha_fin_utc,
        u.bloqueado_hasta_utc,
        u.ultimo_acceso_utc
    FROM seguridad.usuario u
    INNER JOIN seguridad.usuario_tenant ut
        ON ut.id_usuario = u.id_usuario
       AND ut.id_tenant = @ctx_id_tenant
       AND ut.activo = 1
    LEFT JOIN seguridad.usuario_empresa ue
        ON ue.id_usuario = u.id_usuario
       AND ue.id_tenant = @ctx_id_tenant
       AND ue.activo = 1
       AND (@ctx_id_empresa IS NULL OR ue.id_empresa = @ctx_id_empresa)
    WHERE u.id_usuario = @id_usuario
      AND (
            @ctx_id_empresa IS NULL
            OR ue.id_empresa = @ctx_id_empresa
            OR ut.es_administrador_tenant = 1
          );
END;
GO

IF OBJECT_ID(N'seguridad.usp_security_event_listar', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE seguridad.usp_security_event_listar AS BEGIN SET NOCOUNT ON; END');
END;
GO

CREATE OR ALTER PROCEDURE seguridad.usp_security_event_listar
    @top INT = 100,
    @event_type VARCHAR(80) = NULL,
    @severity VARCHAR(20) = NULL,
    @resultado VARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ctx_id_tenant BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
    DECLARE @ctx_id_empresa BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
    DECLARE @take INT = CASE
        WHEN @top IS NULL OR @top < 1 THEN 100
        WHEN @top > 500 THEN 500
        ELSE @top
    END;

    IF @ctx_id_tenant IS NULL
    BEGIN
        THROW 51112, N'Security tenant context is required.', 1;
    END;

    SELECT TOP (@take)
        sea.id_security_event_audit,
        sea.creado_utc,
        sea.event_type,
        sea.severity,
        sea.resultado,
        sea.detalle,
        sea.id_tenant,
        sea.id_empresa,
        sea.id_usuario,
        sea.id_sesion_usuario,
        sea.auth_flow_id,
        sea.correlation_id,
        sea.ip_origen,
        sea.agente_usuario
    FROM seguridad.security_event_audit sea
    WHERE sea.id_tenant = @ctx_id_tenant
      AND (@ctx_id_empresa IS NULL OR sea.id_empresa IS NULL OR sea.id_empresa = @ctx_id_empresa)
      AND (@event_type IS NULL OR sea.event_type = @event_type)
      AND (@severity IS NULL OR sea.severity = @severity)
      AND (@resultado IS NULL OR sea.resultado = @resultado)
    ORDER BY sea.creado_utc DESC, sea.id_security_event_audit DESC;
END;
GO

IF OBJECT_ID(N'seguridad.usp_auth_revocar_sesion_usuario', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE seguridad.usp_auth_revocar_sesion_usuario AS BEGIN SET NOCOUNT ON; END');
END;
GO

CREATE OR ALTER PROCEDURE seguridad.usp_auth_revocar_sesion_usuario
    @id_sesion_usuario UNIQUEIDENTIFIER,
    @revocado_por BIGINT,
    @motivo NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ctx_id_tenant BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_tenant') AS BIGINT);
    DECLARE @ctx_id_empresa BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_empresa') AS BIGINT);
    DECLARE @ctx_id_usuario BIGINT = TRY_CAST(SESSION_CONTEXT(N'id_usuario') AS BIGINT);

    DECLARE @target_user_id BIGINT;
    DECLARE @target_tenant_id BIGINT;
    DECLARE @target_company_id BIGINT;

    IF @ctx_id_tenant IS NULL OR @ctx_id_empresa IS NULL OR @ctx_id_usuario IS NULL
    BEGIN
        THROW 51113, N'Full session context is required.', 1;
    END;

    SELECT TOP (1)
        @target_user_id = s.id_usuario,
        @target_tenant_id = s.id_tenant,
        @target_company_id = s.id_empresa
    FROM seguridad.sesion_usuario s
    WHERE s.id_sesion_usuario = @id_sesion_usuario;

    IF @target_user_id IS NULL
       OR @target_tenant_id <> @ctx_id_tenant
       OR @target_company_id <> @ctx_id_empresa
    BEGIN
        SELECT
            CAST(0 AS BIT) AS ok,
            N'AUTH_SESSION_NOT_FOUND' AS error_code,
            N'Session was not found in the current scope.' AS error_message,
            CAST(NULL AS BIGINT) AS target_user_id;
        RETURN;
    END;

    UPDATE seguridad.sesion_usuario
    SET activo = 0,
        revocada_utc = COALESCE(revocada_utc, SYSUTCDATETIME())
    WHERE id_sesion_usuario = @id_sesion_usuario;

    SELECT
        CAST(1 AS BIT) AS ok,
        CAST(NULL AS NVARCHAR(50)) AS error_code,
        CAST(NULL AS NVARCHAR(300)) AS error_message,
        @target_user_id AS target_user_id;
END;
GO
