SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER PROCEDURE seguridad.usp_autorizacion_evaluar
    @id_usuario BIGINT,
    @id_tenant BIGINT,
    @id_empresa BIGINT,
    @id_sesion_usuario UNIQUEIDENTIFIER,
    @codigo_permiso NVARCHAR(150),
    @requiere_mfa BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @utc DATETIME2(7) = SYSUTCDATETIME();

    IF EXISTS
    (
        SELECT 1
        FROM seguridad.excepcion_permiso_usuario e
        INNER JOIN seguridad.permiso p ON p.id_permiso = e.id_permiso
        LEFT JOIN catalogo.efecto_permiso ef ON ef.id_efecto_permiso = e.id_efecto_permiso
        WHERE e.id_usuario = @id_usuario
          AND e.id_tenant = @id_tenant
          AND p.codigo = @codigo_permiso
          AND e.activo = 1
          AND (e.fecha_inicio_utc IS NULL OR e.fecha_inicio_utc <= @utc)
          AND (e.fecha_fin_utc IS NULL OR e.fecha_fin_utc >= @utc)
          AND (e.id_empresa IS NULL OR e.id_empresa = @id_empresa)
          AND UPPER(COALESCE(ef.codigo, N'')) = N'DENY'
    )
    BEGIN
        SELECT CAST(0 AS BIT) AS autorizado,
               N'DENY_EXPLICIT' AS reason_code,
               N'EXCEPTION' AS resolution_source;
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM seguridad.fn_usuario_empresas_efectivas(@id_usuario, @id_tenant)
        WHERE id_empresa = @id_empresa
    )
    BEGIN
        SELECT CAST(0 AS BIT) AS autorizado,
               N'SCOPE_DENIED' AS reason_code,
               N'SCOPE' AS resolution_source;
        RETURN;
    END;

    IF @requiere_mfa = 1
    BEGIN
        IF NOT EXISTS (
            SELECT 1
            FROM seguridad.sesion_usuario s
            WHERE s.id_sesion_usuario = @id_sesion_usuario
              AND s.id_usuario = @id_usuario
              AND s.id_tenant = @id_tenant
              AND s.id_empresa = @id_empresa
              AND s.activo = 1
              AND s.revocada_utc IS NULL
              AND s.mfa_validado = 1
        )
        BEGIN
            SELECT CAST(0 AS BIT) AS autorizado,
                   N'MFA_REQUIRED' AS reason_code,
                   N'SESSION' AS resolution_source;
            RETURN;
        END;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM seguridad.excepcion_permiso_usuario e
        INNER JOIN seguridad.permiso p ON p.id_permiso = e.id_permiso
        LEFT JOIN catalogo.efecto_permiso ef ON ef.id_efecto_permiso = e.id_efecto_permiso
        WHERE e.id_usuario = @id_usuario
          AND e.id_tenant = @id_tenant
          AND p.codigo = @codigo_permiso
          AND e.activo = 1
          AND (e.fecha_inicio_utc IS NULL OR e.fecha_inicio_utc <= @utc)
          AND (e.fecha_fin_utc IS NULL OR e.fecha_fin_utc >= @utc)
          AND (e.id_empresa IS NULL OR e.id_empresa = @id_empresa)
          AND UPPER(COALESCE(ef.codigo, N'')) = N'ALLOW'
    )
    BEGIN
        SELECT CAST(1 AS BIT) AS autorizado,
               N'ALLOW_EXPLICIT' AS reason_code,
               N'EXCEPTION' AS resolution_source;
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM seguridad.fn_usuario_permisos_efectivos(@id_usuario, @id_tenant, @id_empresa) p
        WHERE p.codigo = @codigo_permiso
    )
    BEGIN
        SELECT CAST(1 AS BIT) AS autorizado,
               N'ALLOW_EFFECTIVE' AS reason_code,
               N'PERMISSION' AS resolution_source;
        RETURN;
    END;

    SELECT CAST(0 AS BIT) AS autorizado,
           N'DENY_DEFAULT' AS reason_code,
           N'DEFAULT_DENY' AS resolution_source;
END;
GO
